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
    public override string ModuleVersion => "1.0.7";
    public override string ModuleAuthor => "ByDexter";
    public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

    private VipConfig Config = new();

    public string ChatPrefix => Localizer["chat_prefix"];

    private IVipStorage _storage = null!;
    private readonly Dictionary<ulong, VipEntry> _vips = new();
    private readonly Dictionary<ulong, Dictionary<string, string>> _settings = new();
    private Dictionary<string, Dictionary<string, JsonElement>> _groups = new();
    private readonly HashSet<string> _enabled = new();
    private readonly HashSet<string> _loaded = new();
    private readonly List<VipModule> _modules = new();
    private readonly Dictionary<string, VipModule> _moduleByName = new();
    private readonly Dictionary<(string Group, string Feature, Type Type), object?> _groupValueCache = new();
    private Dictionary<string, HashSet<string>> _pistolDisable = new();
    private readonly Dictionary<ulong, Dictionary<string, string?>> _pendingSettings = new();
    private bool _isPistolRound;
    private List<CCSPlayerController> _tickPlayers = new();
    private int _tickPlayersAt = -1;
    private readonly object _lock = new();

    public IReadOnlyList<CCSPlayerController> Players
    {
        get
        {
            int tick = Server.TickCount;
            if (_tickPlayersAt != tick)
            {
                _tickPlayersAt = tick;
                _tickPlayers = Utilities.GetPlayers();
            }
            return _tickPlayers;
        }
    }

    private readonly System.Drawing.Color[] _roundColors = new System.Drawing.Color[64];

    public System.Drawing.Color RoundColor(int slot) =>
        slot >= 0 && slot < 64 ? _roundColors[slot] : TrailBeam.RandomColor();

    private void RollRoundColors()
    {
        for (int i = 0; i < 64; i++)
            _roundColors[i] = TrailBeam.RandomColor();
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions GroupOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private string GroupsPath => Path.Combine(ModuleDirectory, "vipgroups.json");

    private CCSGameRulesProxy? _gameRulesProxy;

    public bool IsFreezeTime()
    {
        if (_gameRulesProxy == null || !_gameRulesProxy.IsValid)
            _gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();

        return _gameRulesProxy?.GameRules?.FreezePeriod == true;
    }

    private ConVar? _cvHalftime;
    private ConVar? _cvMaxRounds;

    public bool IsPistolRound()
    {
        if (_gameRulesProxy == null || !_gameRulesProxy.IsValid)
            _gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();

        var rules = _gameRulesProxy?.GameRules;
        if (rules == null)
            return false;

        _cvHalftime ??= ConVar.Find("mp_halftime");
        _cvMaxRounds ??= ConVar.Find("mp_maxrounds");

        bool halftime = _cvHalftime?.GetPrimitiveValue<bool>() ?? false;
        int maxRounds = _cvMaxRounds?.GetPrimitiveValue<int>() ?? 0;

        return rules.TotalRoundsPlayed == 0
            || (halftime && maxRounds / 2 == rules.TotalRoundsPlayed)
            || rules.GameRestart;
    }

    private bool PistolRoundBlocked(CCSPlayerController player, string feature)
    {
        if (_pistolDisable.Count == 0)
            return false;

        var group = GetClientGroup(player);
        if (group == null || !_pistolDisable.TryGetValue(group, out var set) || !set.Contains(feature))
            return false;

        return IsPistolRound();
    }

    internal static VIPCore? Current;

    public override void Load(bool hotReload)
    {
        Current = this;
        RollRoundColors();
        LoadConfig();
        DiscoverModules();
        InitStorage();
        ReloadData();

        ActivateModules();

        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventRoundStart>((_, __) =>
        {
            _isPistolRound = IsPistolRound();
            RollRoundColors();
            return HookResult.Continue;
        });
        RegisterEventHandler<EventRoundEnd>((_, __) =>
        {
            FlushSettings();
            return HookResult.Continue;
        });
        RegisterEventHandler<EventPlayerDisconnect>((ev, _) =>
        {
            var p = ev.Userid;
            if (p != null && p.IsValid && !p.IsBot)
                FlushSettings(p.SteamID);
            return HookResult.Continue;
        });
        RegisterListener<OnMapStart>(_ => PurgeExpired());
        AddTimer(60f, PurgeExpired, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
        RegisterListener<OnServerPrecacheResources>(m =>
        {
            if (IsModuleEnabled("PlayerTrail") || IsModuleEnabled("GrenadeTrail") || IsModuleEnabled("BulletTrail"))
                m.AddResource(TrailBeam.Sprite);

            if (IsModuleEnabled("Thirdperson"))
                m.AddResource("models/sprays/spray_plane.vmdl");
        });
        RegisterCommands();
    }

    public override void Unload(bool hotReload)
    {
        FlushSettings(sync: true);

        foreach (var module in _modules)
            if (_loaded.Contains(module.Name))
                module.OnUnload();

        if (ReferenceEquals(Current, this))
            Current = null;
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
            _moduleByName[module.Name] = module;
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

        var pistolDisable = new Dictionary<string, HashSet<string>>();
        foreach (var (groupName, feats) in parsed)
        {
            if (!feats.TryGetValue("PistolRoundDisable", out var element))
                continue;
            try
            {
                var list = element.Deserialize<List<string>>(GroupOpts);
                if (list != null && list.Count > 0)
                    pistolDisable[groupName] = new HashSet<string>(list);
            }
            catch { }
        }

        lock (_lock)
        {
            _groups = parsed;
            _pistolDisable = pistolDisable;
            _groupValueCache.Clear();
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
            ScheduleStorageRefresh(player.Slot, player.UserId ?? -1, 1);
        }
        else
        {
            PurgeIfExpired(steamId);
        }

        return HookResult.Continue;
    }

    private const int MaxRefreshAttempts = 3;
    private const float RefreshRetryDelay = 5.0f;
    private const ulong MinValidSteamId = 76561197960265728UL;

    private void ScheduleStorageRefresh(int slot, int userId, int attempt)
    {
        if (userId < 0)
            return;

        var player = Utilities.GetPlayerFromSlot(slot);
        if (player == null || !player.IsValid || player.IsBot || player.UserId != userId)
            return;

        ulong steamId = player.AuthorizedSteamID?.SteamId64 ?? player.SteamID;

        if (steamId >= MinValidSteamId)
        {
            RefreshFromStorage(steamId);
            return;
        }

        if (attempt >= MaxRefreshAttempts)
        {
            Logger.LogWarning("VIPCore: slot {0} icin SteamID {1} denemede dogrulanamadi, VIP yenilemesi atlandi.",
                slot, MaxRefreshAttempts);
            return;
        }

        AddTimer(RefreshRetryDelay, () => ScheduleStorageRefresh(slot, userId, attempt + 1),
            CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
    }

    private void RefreshFromStorage(ulong steamId)
    {
        var storage = _storage;

        Task.Run(() =>
        {
            try
            {
                var entry = storage.LoadVip(steamId);
                var settings = storage.LoadSettings(steamId);

                lock (_lock)
                {
                    if (entry != null)
                        _vips[steamId] = entry;
                    else
                        _vips.Remove(steamId);

                    if (settings != null && settings.Count > 0)
                        _settings[steamId] = settings;
                }

                PurgeIfExpired(steamId);
            }
            catch (Exception ex)
            {
                Logger.LogError("VIPCore: {0} icin depo yenilemesi basarisiz: {1}", steamId, ex.Message);
            }
        });
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
        List<ulong> expired;
        lock (_lock)
            expired = _vips.Where(kv => kv.Value.Expires != 0 && kv.Value.Expires <= now).Select(kv => kv.Key).ToList();

        foreach (var id in expired)
            RemoveVip(id);
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

        var key = (group, feature, typeof(T));
        lock (_lock)
        {
            if (_groupValueCache.TryGetValue(key, out var cached))
                return (T?)cached;

            object? value = null;
            if (_groups.TryGetValue(group, out var feats) && feats.TryGetValue(feature, out var element))
            {
                try { value = element.Deserialize<T>(GroupOpts); }
                catch { value = null; }
            }

            _groupValueCache[key] = value;
            return (T?)value;
        }
    }

    public List<T> GetAllGroupValues<T>(string feature)
    {
        var result = new List<T>();
        lock (_lock)
        {
            foreach (var feats in _groups.Values)
            {
                if (!feats.TryGetValue(feature, out var element))
                    continue;
                try
                {
                    var value = element.Deserialize<T>(GroupOpts);
                    if (value != null)
                        result.Add(value);
                }
                catch { }
            }
        }
        return result;
    }

    public bool IsModuleEnabled(string name)
    {
        lock (_lock)
            return _enabled.Contains(name);
    }

    public bool IsGranted(CCSPlayerController player, string feature) =>
        IsClientVip(player) && IsModuleEnabled(feature) && GroupGrants(player, feature);

    public bool IsActive(CCSPlayerController player, string feature) =>
        IsGranted(player, feature) && GetSetting(player.SteamID, feature) != "off" && !PistolRoundBlocked(player, feature);

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

    private VipModule? FindModule(string feature) =>
        _moduleByName.TryGetValue(feature, out var module) ? module : null;

    public void SetSetting(CCSPlayerController player, string feature, string value)
    {
        ulong steamId = player.SteamID;

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
                PendingOp(steamId, feature, null);
            }
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
            PendingOp(steamId, feature, stored);
        }
    }

    private void PendingOp(ulong steamId, string feature, string? value)
    {
        if (!_pendingSettings.TryGetValue(steamId, out var ops))
        {
            ops = new();
            _pendingSettings[steamId] = ops;
        }
        ops[feature] = value;
    }

    public void FlushSettings(ulong? steamId = null, bool sync = false)
    {
        List<(ulong SteamId, Dictionary<string, string?> Ops)> batches = new();
        lock (_lock)
        {
            if (steamId is ulong id)
            {
                if (_pendingSettings.Remove(id, out var ops))
                    batches.Add((id, ops));
            }
            else
            {
                foreach (var (k, v) in _pendingSettings)
                    batches.Add((k, v));
                _pendingSettings.Clear();
            }
        }

        if (batches.Count == 0)
            return;

        var storage = _storage;
        void Apply()
        {
            foreach (var (id, ops) in batches)
            {
                foreach (var (feature, value) in ops)
                {
                    try
                    {
                        if (value == null)
                            storage.DeleteSetting(id, feature);
                        else
                            storage.UpsertSetting(id, feature, value);
                    }
                    catch { }
                }
            }
        }

        if (sync)
            Apply();
        else
            Task.Run(Apply);
    }
}
