using System.Text.Json.Nodes;

namespace VIPCore;

internal static class ConfigMigrate
{
    private static readonly Dictionary<string, string> ModuleRenames = new()
    {
        ["ChatTag"] = "Tag",
        ["Zeus"] = "GiveZeus"
    };

    private static readonly (string Path, string From, string To)[] KeyRenames =
    {
        ("FallDamage", "count", "limit"),
        ("Respawn", "timer", "time"),
        ("Respawn", "time", "delay"),
        ("C4Effect", "time", "lifetime"),
        ("KillEffect", "time", "lifetime"),
        ("KillEffect", "lastkill", "last_kill"),
        ("AntiHS", "percent", "damage_percent"),
        ("FallDamage", "percent", "damage_percent"),
        ("FastDefuse", "time", "defuse_time"),
        ("FastPlant", "time", "plant_time"),
        ("SpawnProtection", "time", "duration"),
        ("Mole", "time", "duration"),
        ("HealthshotEffect.*", "time", "duration"),
        ("SmokeEffect.*", "time", "duration"),
        ("Aura.*", "tick", "interval"),
        ("BulletEffect.*", "tick", "interval"),
        ("DecoyEffect.*", "tick", "interval"),
        ("HealthshotEffect.*", "tick", "interval"),
        ("SmokeEffect.*", "tick", "interval"),
        ("Aura.*", "beamcolor", "beam_color"),
        ("Aura.*", "minspeed", "min_speed"),
        ("Aura.*", "maxspeed", "max_speed"),
        ("DecoyEffect.*", "minhp", "min_hp"),
        ("DecoyEffect.*", "minspeed", "min_speed"),
        ("HealthshotEffect.*", "minhp", "min_hp"),
        ("HealthshotEffect.*", "minspeed", "min_speed"),
        ("HealthshotEffect.*", "recoilpercent", "recoil_percent"),
        ("SmokeEffect.*", "minhp", "min_hp"),
        ("SmokeEffect.*", "minspeed", "min_speed"),
        ("SmokeEffect.*", "smokecolor", "smoke_color"),
        ("TeamHeal", "minhp", "min_hp"),
        ("NoRecoil", "recoilpercent", "recoil_percent"),
        ("RapidFire", "firepercent", "fire_percent"),
        ("Adrenaline", "spk", "speed_per_kill"),
        ("Adrenaline", "maxspk", "max_speed_bonus"),
        ("Berserk", "dpk", "damage_per_kill"),
        ("Berserk", "maxdpk", "max_damage_bonus"),
        ("HealthRegen", "delay_after_dmg", "delay_after_damage"),
        ("ArmorRegen", "delay_after_dmg", "delay_after_damage"),
        ("Invisibility", "dmg_after_invis", "reveal_time"),
        ("GrenadeDamage", "damage_multiplier", "he_damage_multiplier"),
        ("GrenadeDamage", "range_multiplier", "he_damage_range")
    };

    public static string? RenamedKey(string path)
    {
        int dot = path.LastIndexOf('.');
        if (dot <= 0)
            return null;

        string parent = path[..dot];
        string key = path[(dot + 1)..];
        string? result = null;
        foreach (var (pattern, from, to) in KeyRenames)
        {
            if (!string.Equals(key, from, StringComparison.OrdinalIgnoreCase) || !PathMatches(pattern, parent))
                continue;

            result = to;
            key = to;
        }

        return result;
    }

    private static bool PathMatches(string pattern, string path)
    {
        var want = pattern.Split('.');
        var have = path.Split('.');
        if (want.Length != have.Length)
            return false;

        for (int i = 0; i < want.Length; i++)
            if (want[i] != "*" && !string.Equals(want[i], have[i], StringComparison.OrdinalIgnoreCase))
                return false;
        return true;
    }

    private static readonly (string Module, string Parent, string Key)[] Removed =
    {
        ("HealthRegen", "", "max_hp"),
        ("TeamHeal", "", "maxhp"),
        ("SmokeEffect", "heal", "maxhp"),
        ("WeaponGlow", "", "visible")
    };

    private static readonly string[] ModuleLists = { "PistolRoundDisable", "Force" };

    public static List<string> Groups(JsonObject root)
    {
        var log = new List<string>();

        foreach (var (groupName, node) in root.ToList())
        {
            if (node is not JsonObject group)
                continue;

            foreach (var (from, to) in ModuleRenames)
            {
                if (!group.ContainsKey(from) || group.ContainsKey(to))
                    continue;

                RenameKey(group, from, to);
                log.Add($"\"{groupName}\" -> \"{from}\" modulu \"{to}\" olarak yeniden adlandirildi.");
            }

            foreach (var list in ModuleLists)
            {
                if (group[list] is not JsonArray array)
                    continue;

                for (int i = 0; i < array.Count; i++)
                {
                    if (array[i] is not JsonValue value || !value.TryGetValue(out string? name)
                        || name == null || !ModuleRenames.TryGetValue(name, out var renamed))
                        continue;

                    array[i] = renamed;
                    log.Add($"\"{groupName}\" -> {list} listesindeki \"{name}\" \"{renamed}\" yapildi.");
                }
            }

            foreach (var (path, from, to) in KeyRenames)
            {
                int count = 0;
                foreach (var obj in Resolve(group, path))
                {
                    string? actual = FindKey(obj, from);
                    if (actual == null || FindKey(obj, to) != null)
                        continue;

                    RenameKey(obj, actual, to);
                    count++;
                }

                if (count > 0)
                    log.Add($"\"{groupName}\" -> {path}.{from} \"{to}\" olarak yeniden adlandirildi ({count}).");
            }

            foreach (var (module, parent, key) in Removed)
            {
                foreach (var obj in Objects(group[module]))
                {
                    var target = parent.Length == 0 ? obj : FindKey(obj, parent) is string p ? obj[p] as JsonObject : null;
                    string? actual = target == null ? null : FindKey(target, key);
                    if (actual == null)
                        continue;

                    target!.Remove(actual);
                    log.Add($"\"{groupName}\" -> {module}.{(parent.Length > 0 ? parent + "." : "")}{key} artik kullanilmiyor, silindi.");
                }
            }

            MoveRecoil(groupName, group, log);
            SplitGrenadeResist(groupName, group, log);
        }

        return log;
    }

    private static void MoveRecoil(string groupName, JsonObject group, List<string> log)
    {
        if (group["RapidFire"] is not JsonObject rapid)
            return;

        string? percentKey = FindKey(rapid, "recoilpercent");
        string? legacyKey = FindKey(rapid, "norecoil");
        if (percentKey == null && legacyKey == null)
            return;

        float? keep = null;
        if (percentKey != null && rapid[percentKey] is JsonValue percent && percent.TryGetValue(out double value))
            keep = (float)value;
        else if (legacyKey != null && rapid[legacyKey] is JsonValue legacy && legacy.TryGetValue(out bool on) && on)
            keep = 0f;

        bool idle = keep is not float k || k >= 1f;
        if (!idle && group.ContainsKey("NoRecoil"))
            return;

        if (percentKey != null)
            rapid.Remove(percentKey);
        if (legacyKey != null)
            rapid.Remove(legacyKey);

        if (idle)
        {
            log.Add($"\"{groupName}\" -> RapidFire icindeki sekme ayari etkisizdi, silindi.");
            return;
        }

        var noRecoil = new JsonObject
        {
            ["only_with_weapon"] = rapid[FindKey(rapid, "only_with_weapon") ?? "only_with_weapon"]?.DeepClone() ?? "",
            ["only_stance"] = rapid[FindKey(rapid, "only_stance") ?? "only_stance"]?.DeepClone() ?? 0,
            ["recoil_percent"] = Math.Clamp(keep ?? 0f, 0f, 1f)
        };

        InsertAfter(group, "RapidFire", "NoRecoil", noRecoil);
        log.Add($"\"{groupName}\" -> RapidFire icindeki sekme ayari yeni NoRecoil modulune tasindi.");
    }

    private static void SplitGrenadeResist(string groupName, JsonObject group, List<string> log)
    {
        if (FindKey(group, "GrenadeResist") is not string module)
            return;

        foreach (var obj in Objects(group[module]))
        {
            string? percentKey = FindKey(obj, "percent");
            if (percentKey == null)
                continue;

            string? onlyKey = FindKey(obj, "only_with_grenade");
            string only = onlyKey != null && obj[onlyKey] is JsonValue o && o.TryGetValue(out string? csv) ? csv ?? "" : "";
            var types = only.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.ToLowerInvariant()).ToList();
            bool he = types.Count == 0 || types.Contains("he");
            bool fire = types.Count == 0 || types.Any(t => t is "molotov" or "inferno" or "fire" or "incendiary");
            int percent = obj[percentKey] is JsonValue p && p.TryGetValue(out double value) ? (int)Math.Round(value) : 0;

            var pairs = obj.ToList();
            obj.Clear();
            obj["he_damage_percent"] = he ? percent : 0;
            obj["molotov_damage_percent"] = fire ? percent : 0;
            foreach (var (key, node) in pairs)
                if (key != percentKey && key != onlyKey)
                    obj[key] = node;

            log.Add($"\"{groupName}\" -> GrenadeResist percent/only_with_grenade \"he_damage_percent\" ve \"molotov_damage_percent\" olarak ayrildi.");
        }
    }

    public static List<string> Settings(JsonObject root)
    {
        var log = new List<string>();

        if (root["viptest"] is not JsonObject test)
            return log;

        string? groupKey = FindKey(test, "group");
        string? durationKey = FindKey(test, "duration");
        if (groupKey == null && durationKey == null)
            return log;

        string group = groupKey != null && test[groupKey] is JsonValue g && g.TryGetValue(out string? name) ? name ?? "" : "";
        int duration = durationKey != null && test[durationKey] is JsonValue d && d.TryGetValue(out int seconds) ? seconds : 1800;

        var rebuilt = new JsonObject { ["enabled"] = group.Length > 0 && duration > 0 };
        if (group.Length > 0)
            rebuilt["groups"] = new JsonArray(new JsonObject
            {
                ["group"] = group,
                ["time"] = duration,
                ["interval"] = 0
            });

        foreach (var (key, value) in test.ToList())
        {
            if (key == groupKey || key == durationKey || rebuilt.ContainsKey(key))
                continue;

            test.Remove(key);
            rebuilt[key] = value;
        }

        root["viptest"] = rebuilt;
        log.Add(group.Length > 0
            ? $"viptest \"group\"/\"duration\" yeni \"groups\" listesine tasindi ({group}, {duration} sn, tekrar kullanim yok)."
            : "viptest \"group\"/\"duration\" kaldirildi, deneme kapali.");
        return log;
    }

    private static IEnumerable<JsonObject> Resolve(JsonObject group, string path)
    {
        var parts = path.Split('.');
        IEnumerable<JsonObject> current = FindKey(group, parts[0]) is string module ? Objects(group[module]) : Enumerable.Empty<JsonObject>();

        foreach (var part in parts.Skip(1))
        {
            string name = part;
            current = current.SelectMany(obj => name == "*"
                ? obj.Select(pair => pair.Value).OfType<JsonObject>()
                : FindKey(obj, name) is string key ? Objects(obj[key]) : Enumerable.Empty<JsonObject>()).ToList();
        }

        return current.ToList();
    }

    private static IEnumerable<JsonObject> Objects(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            yield return obj;
            yield break;
        }

        if (node is not JsonArray array)
            yield break;

        foreach (var item in array)
            if (item is JsonObject child)
                yield return child;
    }

    private static string? FindKey(JsonObject obj, string key)
    {
        foreach (var pair in obj)
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                return pair.Key;
        return null;
    }

    private static void RenameKey(JsonObject obj, string from, string to)
    {
        var pairs = obj.ToList();
        obj.Clear();
        foreach (var (key, value) in pairs)
            obj[key == from ? to : key] = value;
    }

    private static void InsertAfter(JsonObject obj, string after, string key, JsonNode value)
    {
        var pairs = obj.ToList();
        obj.Clear();
        foreach (var (k, v) in pairs)
        {
            obj[k] = v;
            if (k == after)
                obj[key] = value;
        }
    }
}
