using System.Reflection;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using Microsoft.Extensions.Logging;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public partial class VIPCore : BasePlugin
{
    public override string ModuleName => "VIPCore";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "ByDexter";
    public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

    private VipConfig Config = new();

    private string ChatPrefix => Localizer["chat_prefix"];

    private IVipStorage _storage = null!;
    private readonly Dictionary<ulong, VipEntry> _vips = new();
    private readonly Dictionary<ulong, Dictionary<string, string>> _settings = new();
    private Dictionary<string, Dictionary<string, JsonElement>> _groups = new();
    private readonly HashSet<string> _enabled = new();
    private readonly HashSet<string> _loaded = new();
    private readonly List<VipModule> _modules = new();
    private readonly object _lock = new();

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions GroupOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private string GroupsPath => Path.Combine(ModuleDirectory, "vipgroups.json");

    public override void Load(bool hotReload)
    {
        LoadConfig();
        DiscoverModules();
        InitStorage();
        ReloadData();
        ActivateModules();

        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterListener<OnMapStart>(_ => PurgeExpired());
        RegisterListener<OnServerPrecacheResources>(m =>
        {
            if (IsModuleEnabled("PlayerTrail") || IsModuleEnabled("GrenadeTrail") || IsModuleEnabled("BulletTrail"))
                m.AddResource(TrailBeam.Sprite);
        });
        RegisterCommands();
    }

    public override void Unload(bool hotReload)
    {
        foreach (var module in _modules)
            if (_loaded.Contains(module.Name))
                module.OnUnload();
    }

    private void DiscoverModules()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => !t.IsAbstract && typeof(VipModule).IsAssignableFrom(t));

        foreach (var type in types)
        {
            if (Activator.CreateInstance(type) is not VipModule module)
                continue;

            module.Bind(this);
            _modules.Add(module);
        }
    }

    public IReadOnlyList<string> GetGroupFeatures(CCSPlayerController player)
    {
        var group = GetClientGroup(player);
        if (group == null)
            return Array.Empty<string>();

        lock (_lock)
            return _groups.TryGetValue(group, out var feats)
                ? feats.Keys.ToList()
                : Array.Empty<string>();
    }

    private void LoadConfig()
    {
        string path = Path.Combine(ModuleDirectory, "settings.json");
        try
        {
            if (!File.Exists(path))
                File.WriteAllText(path, JsonSerializer.Serialize(new VipConfig(), JsonOpts));

            Config = JsonSerializer.Deserialize<VipConfig>(File.ReadAllText(path)) ?? new VipConfig();
        }
        catch
        {
            Config = new VipConfig();
        }
    }

    private void InitStorage()
    {
        if (Config.Storage.Equals("mysql", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var sql = new MySqlStorage(Config.MySql);
                sql.Init();
                _storage = sql;
                Logger.LogInformation("VIPCore: MySQL storage aktif.");
                return;
            }
            catch (Exception ex)
            {
                Logger.LogError("VIPCore: MySQL baglantisi basarisiz, JSON'a dusuluyor. {0}", ex.Message);
            }
        }

        _storage = new JsonStorage(ModuleDirectory);
        _storage.Init();
    }

    private void ActivateModules()
    {
        foreach (var module in _modules)
        {
            if (!IsModuleEnabled(module.Name) || _loaded.Contains(module.Name))
                continue;

            module.OnLoad();
            _loaded.Add(module.Name);
        }
    }

    private void ReloadData()
    {
        LoadGroups();

        var vips = _storage.LoadVips();
        var settings = _storage.LoadSettings();

        lock (_lock)
        {
            _vips.Clear();
            foreach (var (k, v) in vips)
                _vips[k] = v;

            _settings.Clear();
            foreach (var (k, v) in settings)
                _settings[k] = v;
        }

        PurgeExpired();
    }

    private void LoadGroups()
    {
        if (!File.Exists(GroupsPath))
            File.WriteAllText(GroupsPath, DefaultGroupsJson);

        Dictionary<string, Dictionary<string, JsonElement>> parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, JsonElement>>>(File.ReadAllText(GroupsPath))
                     ?? new();
        }
        catch
        {
            parsed = new();
        }

        lock (_lock)
        {
            _groups = parsed;
            _enabled.Clear();

            foreach (var (groupName, feats) in _groups)
                foreach (var feature in feats.Keys)
                    _enabled.Add(feature);
        }
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        ulong steamId = player.SteamID;

        if (_storage.SupportsLiveRefresh)
        {
            Task.Run(() =>
            {
                var entry = _storage.LoadVip(steamId);
                lock (_lock)
                {
                    if (entry != null)
                        _vips[steamId] = entry;
                    else
                        _vips.Remove(steamId);
                }
                PurgeIfExpired(steamId);
            });
        }
        else
        {
            PurgeIfExpired(steamId);
        }

        return HookResult.Continue;
    }

    public void PurgeIfExpired(ulong steamId)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        bool expired;
        lock (_lock)
            expired = _vips.TryGetValue(steamId, out var entry) && entry.Expires != 0 && entry.Expires <= now;

        if (expired)
            RemoveVip(steamId);
    }

    private void PurgeExpired()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        lock (_lock)
        {
            foreach (var id in _vips.Where(kv => kv.Value.Expires != 0 && kv.Value.Expires <= now).Select(kv => kv.Key).ToList())
                _vips.Remove(id);
        }
    }

    public IEnumerable<VipModule> EnabledModules()
    {
        foreach (var module in _modules)
            if (IsModuleEnabled(module.Name))
                yield return module;
    }

    public bool IsClientVip(CCSPlayerController player) =>
        player != null && player.IsValid && !player.IsBot && IsSteamIdVip(player.SteamID);

    public bool IsSteamIdVip(ulong steamId)
    {
        lock (_lock)
        {
            if (!_vips.TryGetValue(steamId, out var entry))
                return false;
            return entry.Expires == 0 || entry.Expires > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    public string? GetClientGroup(CCSPlayerController player)
    {
        if (!IsClientVip(player))
            return null;
        lock (_lock)
            return _vips.TryGetValue(player.SteamID, out var entry) ? entry.Group : null;
    }

    public bool GroupGrants(CCSPlayerController player, string feature)
    {
        var group = GetClientGroup(player);
        if (group == null)
            return false;
        lock (_lock)
            return _groups.TryGetValue(group, out var feats) && feats.ContainsKey(feature);
    }

    public T? GetGroupValue<T>(CCSPlayerController player, string feature)
    {
        var group = GetClientGroup(player);
        if (group == null)
            return default;

        lock (_lock)
        {
            if (_groups.TryGetValue(group, out var feats) && feats.TryGetValue(feature, out var element))
            {
                try { return element.Deserialize<T>(GroupOpts); }
                catch { return default; }
            }
        }
        return default;
    }

    public bool IsModuleEnabled(string name)
    {
        lock (_lock)
            return _enabled.Contains(name);
    }

    public bool IsGranted(CCSPlayerController player, string feature) =>
        IsClientVip(player) && IsModuleEnabled(feature) && GroupGrants(player, feature);

    public bool IsActive(CCSPlayerController player, string feature) =>
        IsGranted(player, feature) && GetSetting(player.SteamID, feature) != "off";

    public string GetSetting(ulong steamId, string feature)
    {
        string? raw = GetSettingRaw(steamId, feature);
        if (raw == "0")
            return "off";
        if (raw == "1")
            return "on";
        if (raw != null)
            return raw;

        return DefaultSetting(feature);
    }

    private string DefaultSetting(string feature) =>
        FindModule(feature)?.MenuType == VipFeatureType.Toggle ? "on" : "off";

    private string? GetSettingRaw(ulong steamId, string feature)
    {
        lock (_lock)
            if (_settings.TryGetValue(steamId, out var dict) && dict.TryGetValue(feature, out var value))
                return value;
        return null;
    }

    private VipModule? FindModule(string feature)
    {
        foreach (var module in _modules)
            if (module.Name == feature)
                return module;
        return null;
    }

    public void SetSetting(CCSPlayerController player, string feature, string value)
    {
        ulong steamId = player.SteamID;
        var storage = _storage;

        if (value == DefaultSetting(feature))
        {
            lock (_lock)
            {
                if (_settings.TryGetValue(steamId, out var dict))
                {
                    dict.Remove(feature);
                    if (dict.Count == 0)
                        _settings.Remove(steamId);
                }
            }

            Task.Run(() =>
            {
                try { storage.DeleteSetting(steamId, feature); }
                catch { }
            });
            return;
        }

        string stored = value == "off" ? "0" : value == "on" ? "1" : value;
        lock (_lock)
        {
            if (!_settings.TryGetValue(steamId, out var dict))
            {
                dict = new();
                _settings[steamId] = dict;
            }
            dict[feature] = stored;
        }

        Task.Run(() =>
        {
            try { storage.UpsertSetting(steamId, feature, stored); }
            catch { }
        });
    }
}

public static class CC
{
    public static char Default => '\x01';
    public static char Red => '\x07';
    public static char Orchid => '\x0E';
    public static char Green => '\x04';
    public static char Gold => '\x10';
}
