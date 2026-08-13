using System.Text.Json;

namespace VIPCore;

public class JsonStorage : IVipStorage
{
    private readonly string _vipsPath;
    private readonly string _settingsPath;
    private readonly object _ioLock = new();
    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    public JsonStorage(string directory)
    {
        _vipsPath = Path.Combine(directory, "vips.json");
        _settingsPath = Path.Combine(directory, "players.json");
    }

    public bool SupportsLiveRefresh => false;

    public void Init() { }

    public Dictionary<ulong, VipEntry> LoadVips()
    {
        var result = new Dictionary<ulong, VipEntry>();
        lock (_ioLock)
        {
            if (!File.Exists(_vipsPath))
                return result;

            var raw = JsonSerializer.Deserialize<Dictionary<string, VipEntry>>(File.ReadAllText(_vipsPath));
            if (raw == null)
                return result;

            foreach (var (key, value) in raw)
                if (ulong.TryParse(key, out var steamId))
                    result[steamId] = value;
        }
        return result;
    }

    public Dictionary<ulong, Dictionary<string, string>> LoadSettings()
    {
        var result = new Dictionary<ulong, Dictionary<string, string>>();
        lock (_ioLock)
        {
            if (!File.Exists(_settingsPath))
                return result;

            var raw = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(_settingsPath));
            if (raw == null)
                return result;

            foreach (var (key, value) in raw)
                if (ulong.TryParse(key, out var steamId))
                    result[steamId] = value;
        }
        return result;
    }

    public VipEntry? LoadVip(ulong steamId) => LoadVips().TryGetValue(steamId, out var e) ? e : null;

    public Dictionary<string, string>? LoadSettings(ulong steamId) =>
        LoadSettings().TryGetValue(steamId, out var s) ? s : null;

    public void UpsertVip(ulong steamId, VipEntry entry)
    {
        lock (_ioLock)
        {
            var all = LoadVipsUnlocked();
            all[steamId.ToString()] = entry;
            WriteVips(all);
        }
    }

    public void DeleteVip(ulong steamId)
    {
        lock (_ioLock)
        {
            var all = LoadVipsUnlocked();
            if (all.Remove(steamId.ToString()))
                WriteVips(all);

            var settings = LoadSettingsUnlocked();
            if (settings.Remove(steamId.ToString()))
                File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, Opts));
        }
    }

    public void ApplySettings(ulong steamId, Dictionary<string, string?> ops)
    {
        if (ops.Count == 0)
            return;

        lock (_ioLock)
        {
            var all = LoadSettingsUnlocked();
            string key = steamId.ToString();
            all.TryGetValue(key, out var dict);
            bool changed = false;

            foreach (var (feature, value) in ops)
            {
                if (value == null)
                {
                    changed |= dict != null && dict.Remove(feature);
                    continue;
                }

                if (dict == null)
                {
                    dict = new();
                    all[key] = dict;
                }

                if (dict.TryGetValue(feature, out var current) && current == value)
                    continue;

                dict[feature] = value;
                changed = true;
            }

            if (!changed)
                return;

            if (dict is { Count: 0 })
                all.Remove(key);

            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(all, Opts));
        }
    }

    private Dictionary<string, VipEntry> LoadVipsUnlocked()
    {
        if (!File.Exists(_vipsPath))
            return new();
        return JsonSerializer.Deserialize<Dictionary<string, VipEntry>>(File.ReadAllText(_vipsPath)) ?? new();
    }

    private Dictionary<string, Dictionary<string, string>> LoadSettingsUnlocked()
    {
        if (!File.Exists(_settingsPath))
            return new();
        return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(_settingsPath)) ?? new();
    }

    private void WriteVips(Dictionary<string, VipEntry> all) =>
        File.WriteAllText(_vipsPath, JsonSerializer.Serialize(all, Opts));
}
