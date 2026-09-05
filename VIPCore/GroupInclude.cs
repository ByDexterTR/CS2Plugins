using System.Text.Json;
using System.Text.Json.Nodes;

namespace VIPCore;

internal static class GroupInclude
{
    public const string Key = "Include";

    private static readonly HashSet<string> ReplaceFeatures = new(StringComparer.OrdinalIgnoreCase)
    {
        "PistolRoundDisable",
        "Force"
    };

    private static readonly HashSet<string> LowerWinsPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "FallDamage.percent",
        "Respawn.time",
        "FastDefuse.time",
        "FastPlant.time",
        "Soul.respawn_time"
    };

    private static readonly HashSet<string> LowerWinsKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "tick",
        "interval",
        "cooldown",
        "duration_off",
        "delay_after_dmg",
        "dmg_after_invis",
        "recoilpercent",
        "minspeed",
        "minhp"
    };

    private static readonly string[] IdentityKeys = { "name", "weapon_name", "weapon", "sound", "file", "model" };

    public static List<string> Resolve(Dictionary<string, Dictionary<string, JsonElement>> groups)
    {
        var issues = new List<string>();
        var resolved = new Dictionary<string, Dictionary<string, JsonElement>>(StringComparer.OrdinalIgnoreCase);
        var state = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string name in groups.Keys)
            names[name] = name;

        foreach (string name in groups.Keys.ToList())
            groups[name] = Build(name, groups, names, resolved, state, issues);

        return issues;
    }

    private static Dictionary<string, JsonElement> Build(string name,
        Dictionary<string, Dictionary<string, JsonElement>> groups,
        Dictionary<string, string> names,
        Dictionary<string, Dictionary<string, JsonElement>> resolved,
        Dictionary<string, int> state,
        List<string> issues)
    {
        if (resolved.TryGetValue(name, out var done))
            return done;

        if (!groups.TryGetValue(name, out var own))
            return new Dictionary<string, JsonElement>();

        if (state.TryGetValue(name, out int mark) && mark == 1)
        {
            issues.Add($"\"{name}\" -> Include dongusu var, bu grubun kalitimi atlandi.");
            return Strip(own);
        }

        state[name] = 1;

        var merged = new Dictionary<string, JsonNode?>(StringComparer.OrdinalIgnoreCase);

        foreach (string parent in Parents(name, own, names, issues))
        {
            var parentFeats = Build(parent, groups, names, resolved, state, issues);
            foreach (var (feature, element) in parentFeats)
                Add(merged, feature, element);
        }

        foreach (var (feature, element) in own)
        {
            if (Key.Equals(feature, StringComparison.OrdinalIgnoreCase))
                continue;

            Add(merged, feature, element);
        }

        var output = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var (feature, node) in merged)
        {
            if (node == null)
                continue;

            try { output[feature] = JsonSerializer.Deserialize<JsonElement>(node.ToJsonString()); }
            catch { }
        }

        state[name] = 2;
        resolved[name] = output;
        return output;
    }

    private static List<string> Parents(string name, Dictionary<string, JsonElement> own,
        Dictionary<string, string> names, List<string> issues)
    {
        var parents = new List<string>();

        if (!own.TryGetValue(Key, out var element))
            return parents;

        var raw = new List<string>();
        if (element.ValueKind == JsonValueKind.String)
            raw.Add(element.GetString() ?? "");
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String)
                    raw.Add(item.GetString() ?? "");
        else
            issues.Add($"\"{name}\" -> Include bir grup adi listesi olmali.");

        foreach (string parent in raw)
        {
            if (parent.Length == 0)
                continue;

            if (parent.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add($"\"{name}\" -> Include kendini gosteriyor.");
                continue;
            }

            if (!names.TryGetValue(parent, out string? actual))
            {
                issues.Add($"\"{name}\" -> Include icindeki \"{parent}\" diye bir grup yok.");
                continue;
            }

            parents.Add(actual);
        }

        return parents;
    }

    private static Dictionary<string, JsonElement> Strip(Dictionary<string, JsonElement> own)
    {
        var output = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var (feature, element) in own)
            if (!Key.Equals(feature, StringComparison.OrdinalIgnoreCase))
                output[feature] = element;
        return output;
    }

    private static void Add(Dictionary<string, JsonNode?> merged, string feature, JsonElement element)
    {
        var incoming = Parse(element);

        if (!merged.TryGetValue(feature, out var current) || ReplaceFeatures.Contains(feature))
        {
            merged[feature] = incoming;
            return;
        }

        merged[feature] = Merge(current, incoming, feature);
    }

    private static JsonNode? Parse(JsonElement element)
    {
        try { return JsonNode.Parse(element.GetRawText()); }
        catch { return null; }
    }

    private static JsonNode? Merge(JsonNode? current, JsonNode? incoming, string path)
    {
        if (incoming == null)
            return current;

        if (current == null)
            return incoming;

        if (current is JsonObject target && incoming is JsonObject source)
        {
            foreach (var (key, value) in source.ToList())
            {
                source.Remove(key);

                var existing = target[key];
                var result = Merge(existing, value, $"{path}.{key}");
                if (!ReferenceEquals(existing, result))
                    target[key] = result;
            }
            return target;
        }

        if (current is JsonArray left && incoming is JsonArray right)
            return Union(left, right, path);

        if (IsNumber(current, out double a) && IsNumber(incoming, out double b))
            return Pick(path, a, b);

        return incoming;
    }

    private static JsonNode Union(JsonArray left, JsonArray right, string path)
    {
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < left.Count; i++)
            index[Identity(left[i])] = i;

        foreach (var item in right.ToList())
        {
            right.Remove(item);
            string id = Identity(item);

            if (!index.TryGetValue(id, out int at))
            {
                index[id] = left.Count;
                left.Add(item);
                continue;
            }

            var existing = left[at];
            var result = Merge(existing, item, path);
            if (!ReferenceEquals(existing, result))
                left[at] = result;
        }

        return left;
    }

    private static string Identity(JsonNode? node)
    {
        if (node is JsonObject entry)
            foreach (string key in IdentityKeys)
                if (entry.TryGetPropertyValue(key, out var value) && value != null)
                    return $"{key}={value.ToJsonString()}";

        return node?.ToJsonString() ?? "null";
    }

    private static JsonNode Pick(string path, double current, double incoming)
    {
        string key = path[(path.LastIndexOf('.') + 1)..];

        if (key.Equals("limit", StringComparison.OrdinalIgnoreCase))
            return JsonValue.Create(current == 0 || incoming == 0 ? 0 : Math.Max(current, incoming));

        bool lower = LowerWinsPaths.Contains(path) || LowerWinsKeys.Contains(key);
        return JsonValue.Create(lower ? Math.Min(current, incoming) : Math.Max(current, incoming));
    }

    private static bool IsNumber(JsonNode node, out double value)
    {
        value = 0;
        return node is JsonValue item && item.TryGetValue(out value);
    }
}
