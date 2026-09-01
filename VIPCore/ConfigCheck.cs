using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace VIPCore;

internal static class ConfigCheck
{
    private static readonly HashSet<string> ReservedFeatures = new(StringComparer.OrdinalIgnoreCase)
    {
        "PistolRoundDisable",
        "Force"
    };

    private static readonly Dictionary<string, string> ModuleRenames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ChatTag"] = "Tag",
        ["Zeus"] = "GiveZeus",
        ["PoisonBullet"] = "BulletEffect icindeki \"poison\"",
        ["MagneticDecoy"] = "DecoyEffect icindeki \"magnetic\"",
        ["HealthshotBoost"] = "HealthshotEffect"
    };

    private static readonly Dictionary<string, string> KeyRenames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FallDamage.count"] = "limit",
        ["RapidFire.norecoil"] = "recoilpercent",
        ["Respawn.timer"] = "time",
        ["HealthRegen.max_hp"] = "",
        ["TeamHeal.maxhp"] = "",
        ["SmokeEffect.heal.maxhp"] = ""
    };

    private sealed class Node
    {
        public readonly Dictionary<string, Node?> Props = new(StringComparer.OrdinalIgnoreCase);
        public Node? Item;
        public Node? DictValue;
        public bool FreeKeys;
    }

    private static readonly Dictionary<Type, Node?> _schemas = new();

    public static List<string> Groups(Dictionary<string, Dictionary<string, JsonElement>> groups,
                                      IReadOnlyDictionary<string, VipModule> modules)
    {
        var issues = new List<string>();

        foreach (var (groupName, feats) in groups)
        {
            foreach (var (feature, element) in feats)
            {
                if (ReservedFeatures.Contains(feature))
                {
                    CheckModuleList(groupName, feature, element, modules, issues);
                    continue;
                }

                if (ModuleRenames.TryGetValue(feature, out var moved))
                {
                    issues.Add($"\"{groupName}\" -> \"{feature}\" kaldirildi, yerine {moved} kullanin.");
                    continue;
                }

                if (!modules.TryGetValue(feature, out var module))
                {
                    string hint = Nearest(feature, modules.Keys);
                    issues.Add(hint.Length > 0
                        ? $"\"{groupName}\" -> \"{feature}\" diye bir modul yok, \"{hint}\" mi olacakti?"
                        : $"\"{groupName}\" -> \"{feature}\" diye bir modul yok, satiri silin.");
                    continue;
                }

                Check(element, Schema(module), groupName, feature, feature, issues);
            }
        }

        return issues;
    }

    public static List<string> Settings(VipConfig config, IReadOnlyDictionary<string, VipModule> modules)
    {
        var issues = new List<string>();

        foreach (var name in config.Hide.Keys)
        {
            if (Array.IndexOf(EffectHide.Names, name) >= 0)
                continue;

            issues.Add(modules.ContainsKey(name)
                ? $"hide icindeki \"{name}\" gizlenebilir bir modul degil."
                : $"hide icindeki \"{name}\" diye bir modul yok.");
        }

        return issues;
    }

    private static void CheckModuleList(string groupName, string feature, JsonElement element,
                                        IReadOnlyDictionary<string, VipModule> modules, List<string> issues)
    {
        if (element.ValueKind != JsonValueKind.Array)
            return;

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
                continue;

            string name = item.GetString() ?? "";
            if (name.Length == 0 || modules.ContainsKey(name))
                continue;

            if (ModuleRenames.TryGetValue(name, out var moved))
                issues.Add($"\"{groupName}\" -> {feature} listesindeki \"{name}\" kaldirildi, yerine {moved} kullanin.");
            else
                issues.Add($"\"{groupName}\" -> {feature} listesinde \"{name}\" diye bir modul yok.");
        }
    }

    private static void Check(JsonElement element, Node? node, string groupName, string feature, string path,
                              List<string> issues)
    {
        if (node == null)
            return;

        if (element.ValueKind == JsonValueKind.Array)
        {
            if (node.Item == null)
                return;
            foreach (var item in element.EnumerateArray())
                Check(item, node.Item, groupName, feature, path, issues);
            return;
        }

        if (element.ValueKind != JsonValueKind.Object)
            return;

        if (node.FreeKeys)
        {
            foreach (var prop in element.EnumerateObject())
                Check(prop.Value, node.DictValue, groupName, feature, $"{path}.{prop.Name}", issues);
            return;
        }

        if (node.Props.Count == 0)
            return;

        foreach (var prop in element.EnumerateObject())
        {
            string child = $"{path}.{prop.Name}";

            if (node.Props.TryGetValue(prop.Name, out var childNode))
            {
                Check(prop.Value, childNode, groupName, feature, child, issues);
                continue;
            }

            if (KeyRenames.TryGetValue(child, out var renamed))
            {
                issues.Add(renamed.Length > 0
                    ? $"\"{groupName}\" -> {child} artik \"{renamed}\" olarak adlandiriliyor."
                    : $"\"{groupName}\" -> {child} kaldirildi, satiri silin.");
                continue;
            }

            string hint = Nearest(prop.Name, node.Props.Keys);
            issues.Add(hint.Length > 0
                ? $"\"{groupName}\" -> {child} bilinmiyor, \"{hint}\" mi olacakti?"
                : $"\"{groupName}\" -> {child} bilinmiyor.");
        }
    }

    private static Node? Schema(VipModule module)
    {
        var type = module.GetType();
        var root = type.GetNestedType("Cfg", BindingFlags.Public | BindingFlags.NonPublic);
        if (root == null)
        {
            var entry = type.GetNestedType("Entry", BindingFlags.Public | BindingFlags.NonPublic);
            if (entry == null)
                return null;
            root = typeof(List<>).MakeGenericType(entry);
        }

        return Build(root, 0);
    }

    private static Node? Build(Type type, int depth)
    {
        if (depth > 6)
            return null;

        type = Nullable.GetUnderlyingType(type) ?? type;

        if (depth == 0 && _schemas.TryGetValue(type, out var cached))
            return cached;

        var node = BuildCore(type, depth);

        if (depth == 0)
            _schemas[type] = node;

        return node;
    }

    private static Node? BuildCore(Type type, int depth)
    {
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
            || type == typeof(JsonElement) || type == typeof(object))
            return null;

        var dict = Interface(type, typeof(IDictionary<,>));
        if (dict != null)
            return new Node { FreeKeys = true, DictValue = Build(dict.GetGenericArguments()[1], depth + 1) };

        if (type.IsArray)
            return new Node { Item = Build(type.GetElementType()!, depth + 1) };

        var list = Interface(type, typeof(IEnumerable<>));
        if (list != null)
            return new Node { Item = Build(list.GetGenericArguments()[0], depth + 1) };

        if (!type.IsClass && !type.IsValueType)
            return null;

        var node = new Node();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetIndexParameters().Length > 0 || prop.GetMethod == null)
                continue;

            var named = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
            string name = named?.Name ?? JsonNamingPolicy.SnakeCaseLower.ConvertName(prop.Name);
            node.Props[name] = Build(prop.PropertyType, depth + 1);
        }

        return node.Props.Count > 0 ? node : null;
    }

    private static Type? Interface(Type type, Type open)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == open)
            return type;

        foreach (var iface in type.GetInterfaces())
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == open)
                return iface;

        return null;
    }

    public static int FillMissing(Type type, JsonObject defaults, JsonObject user, string path,
                                  List<string> added, List<string> unknown)
    {
        int count = 0;
        var props = MapProperties(type);

        foreach (var pair in defaults)
        {
            var value = pair.Value;
            if (value == null)
                continue;

            if (!user.TryGetPropertyValue(pair.Key, out var mine) || mine == null)
            {
                user[pair.Key] = value.DeepClone();
                added.Add(path.Length > 0 ? $"{path}.{pair.Key}" : pair.Key);
                count++;
                continue;
            }

            if (value is not JsonObject defChild || mine is not JsonObject myChild)
                continue;

            var childType = props != null && props.TryGetValue(pair.Key, out var prop)
                ? prop.PropertyType
                : DictValueType(type);

            if (childType != null)
                count += FillMissing(childType, defChild, myChild, path.Length > 0 ? $"{path}.{pair.Key}" : pair.Key,
                                     added, unknown);
        }

        if (props == null)
            return count;

        foreach (var pair in user)
            if (!props.ContainsKey(pair.Key))
                unknown.Add(path.Length > 0 ? $"{path}.{pair.Key}" : pair.Key);

        return count;
    }

    private static Dictionary<string, PropertyInfo>? MapProperties(Type type)
    {
        if (Interface(type, typeof(IDictionary<,>)) != null)
            return null;

        var map = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetIndexParameters().Length > 0 || prop.GetMethod == null)
                continue;

            var named = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
            map[named?.Name ?? prop.Name] = prop;
        }

        return map;
    }

    private static Type? DictValueType(Type type)
    {
        var dict = Interface(type, typeof(IDictionary<,>));
        return dict?.GetGenericArguments()[1];
    }

    private static string Nearest(string name, IEnumerable<string> options)
    {
        string best = "";
        int bestScore = int.MaxValue;

        foreach (var option in options)
        {
            int score = Distance(name, option);
            if (score < bestScore)
            {
                bestScore = score;
                best = option;
            }
        }

        int allowed = name.Length <= 5 ? 1 : 2;
        return bestScore <= allowed ? best : "";
    }

    private static int Distance(string a, string b)
    {
        if (a.Equals(b, StringComparison.OrdinalIgnoreCase))
            return 0;

        var prev = new int[b.Length + 1];
        var cur = new int[b.Length + 1];

        for (int j = 0; j <= b.Length; j++)
            prev[j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            cur[0] = i;
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = char.ToLowerInvariant(a[i - 1]) == char.ToLowerInvariant(b[j - 1]) ? 0 : 1;
                cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, cur) = (cur, prev);
        }

        return prev[b.Length];
    }
}
