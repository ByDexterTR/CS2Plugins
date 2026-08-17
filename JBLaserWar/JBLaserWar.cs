using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;
using ByDexter.Shared;

namespace JBLaserWar;

public class LaserTeamConfig
{
  [JsonPropertyName("name")]
  public string Name { get; set; } = "";

  [JsonPropertyName("color")]
  public string Color { get; set; } = "#FF3C28";

  [JsonPropertyName("model")]
  public string Model { get; set; } = "";
}

public class LaserBeamConfig
{
  [JsonPropertyName("width")]
  public float Width { get; set; } = 0.5f;

  [JsonPropertyName("speed")]
  public float Speed { get; set; } = 3000f;

  [JsonPropertyName("length")]
  public float Length { get; set; } = 260f;

  [JsonPropertyName("max_active")]
  public int MaxActive { get; set; } = 128;
}

public class LaserSoundConfig
{
  [JsonPropertyName("fire")]
  public string Fire { get; set; } = "Weapon_Taser.ChargeReady_Zap";

  [JsonPropertyName("fire_volume")]
  public float FireVolume { get; set; } = 1f;

  [JsonPropertyName("bounce")]
  public string Bounce { get; set; } = "FX_RicochetSound.Ricochet_Legacy";

  [JsonPropertyName("bounce_volume")]
  public float BounceVolume { get; set; } = 0.8f;
}

public class LaserFlashConfig
{
  [JsonPropertyName("r")]
  public int R { get; set; } = 255;

  [JsonPropertyName("g")]
  public int G { get; set; } = 0;

  [JsonPropertyName("b")]
  public int B { get; set; } = 0;

  [JsonPropertyName("a")]
  public int A { get; set; } = 90;

  [JsonPropertyName("duration")]
  public int Duration { get; set; } = 150;

  [JsonPropertyName("hold_time")]
  public int HoldTime { get; set; } = 500;
}

public class JBLaserWarConfig : BasePluginConfig
{
  [JsonPropertyName("laserwar_cmd")]
  public string Commands { get; set; } = "css_lw,css_laserwar";

  [JsonPropertyName("laserwar_flag")]
  public string Flag { get; set; } = "@css/generic,@jailbreak/warden";

  [JsonPropertyName("laserwar_weapons")]
  public List<string> Weapons { get; set; } = new()
  {
    "weapon_m4a1_silencer", "weapon_mp5sd", "weapon_usp_silencer"
  };

  [JsonPropertyName("laserwar_gravity")]
  public List<float> Gravity { get; set; } = new() { 0.3f, 0.5f, 0.8f, 1.0f };

  [JsonPropertyName("laserwar_max_distance")]
  public float MaxDistance { get; set; } = 4096f;

  [JsonPropertyName("laserwar_hit_radius")]
  public float HitRadius { get; set; } = 20f;

  [JsonPropertyName("laserwar_killfeed_icon")]
  public string KillfeedIcon { get; set; } = "spray0";

  [JsonPropertyName("laserwar_beam")]
  public LaserBeamConfig Beam { get; set; } = new();

  [JsonPropertyName("laserwar_teams")]
  public List<LaserTeamConfig> Teams { get; set; } = new()
  {
    new()
    {
      Name = "Sith",
      Color = "#FF3C28",
      Model = "agents/models/tm_leet/tm_leet_varianti.vmdl"
    },
    new()
    {
      Name = "Jedi",
      Color = "#28C8FF",
      Model = "agents/models/tm_phoenix/tm_phoenix_varianti.vmdl"
    },
    new()
    {
      Name = "Mandalor",
      Color = "#5CE05C",
      Model = "agents/models/tm_jungle_raider/tm_jungle_raider_varianta.vmdl"
    },
    new()
    {
      Name = "Klon",
      Color = "#FFD24A",
      Model = "agents/models/tm_professional/tm_professional_varf.vmdl"
    }
  };

  [JsonPropertyName("laserwar_sound")]
  public LaserSoundConfig Sound { get; set; } = new();

  [JsonPropertyName("laserwar_flash")]
  public LaserFlashConfig Flash { get; set; } = new();
}

public class LaserSettings
{
  [JsonPropertyName("weapon")]
  public string Weapon { get; set; } = "";

  [JsonPropertyName("hits")]
  public int Hits { get; set; } = 1;

  [JsonPropertyName("bounces")]
  public int Bounces { get; set; } = 2;

  [JsonPropertyName("team_count")]
  public int TeamCount { get; set; } = 2;

  [JsonPropertyName("gravity")]
  public float Gravity { get; set; } = 1f;

  [JsonPropertyName("laser_sound")]
  public bool LaserSound { get; set; } = true;

  [JsonPropertyName("infinite_ammo")]
  public bool InfiniteAmmo { get; set; } = true;
}

public readonly record struct Credit(int From, int Tick);

public readonly record struct Health(int Current, int Max);

public partial class JBLaserWar : BasePlugin, IPluginConfig<JBLaserWarConfig>
{
  public override string ModuleName => "JBLaserWar";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public JBLaserWarConfig Config { get; set; } = new();

  private const int MinTeams = 1;
  private const int MaxTeams = 4;
  private const string BeamSprite = "materials/sprites/laserbeam.vmat";
  private const string AmmoCvar = "sv_infinite_ammo";
  private const string GravityCvar = "sv_gravity";
  private const float BaseGravity = 800f;
  private const string BombWeapon = "weapon_c4";
  private const int GameHealth = 100;
  private const float KillDelay = 0.1f;
  private const int MaxBounces = 4;

  private static readonly Dictionary<ushort, string> VariantNames = new()
  {
    [23] = "weapon_mp5sd",
    [60] = "weapon_m4a1_silencer",
    [61] = "weapon_usp_silencer",
    [63] = "weapon_cz75a",
    [64] = "weapon_revolver"
  };

  private static readonly (string Name, float Value)[] GameCvars =
  {
    ("weapon_accuracy_nospread", 1f),
    ("weapon_recoil_scale", 0f),
    ("mp_death_drop_gun", 0f)
  };

  private bool _active;
  private string _weapon = "weapon_m4a1_silencer";
  private int _hitsToKill = 1;
  private int _bounces = 2;
  private int _teamCount = MinTeams;
  private float _gravity = 1f;
  private bool _laserSound = true;
  private bool _infiniteAmmo = true;

  private string SettingsPath => Path.Combine(ModuleDirectory, "JBLaserWar.json");

  private readonly Dictionary<int, int> _hits = new();
  private readonly HashSet<int> _dying = new();
  private readonly Dictionary<int, int> _team = new();
  private readonly Dictionary<int, Credit> _credit = new();
  private readonly Dictionary<int, List<string>> _loadout = new();
  private readonly Dictionary<int, string> _savedModel = new();
  private readonly Dictionary<int, Health> _savedHealth = new();

  private readonly Dictionary<string, object> _savedCvars = new();
  private readonly List<System.Drawing.Color> _teamColors = new();

  private WasdMenuManager _menus = null!;

  public void OnConfigParsed(JBLaserWarConfig config)
  {
    config.Weapons = config.Weapons
      .Select(NormalizeWeapon)
      .Where(name => name.Length > 0)
      .Distinct()
      .ToList();

    if (config.Weapons.Count == 0)
      config.Weapons.Add("weapon_m4a1_silencer");

    config.Gravity = config.Gravity
      .Select(value => Math.Clamp(value, 0.1f, 2f))
      .Distinct()
      .ToList();

    if (config.Gravity.Count == 0)
      config.Gravity.Add(1f);

    config.MaxDistance = Math.Max(config.MaxDistance, 256f);
    config.HitRadius = Math.Clamp(config.HitRadius, 1f, 128f);

    config.Beam.Width = Math.Max(config.Beam.Width, 0.1f);
    config.Beam.Speed = Math.Max(config.Beam.Speed, 200f);
    config.Beam.Length = Math.Max(config.Beam.Length, 16f);
    config.Beam.MaxActive = Math.Clamp(config.Beam.MaxActive, 1, 256);

    var defaults = new JBLaserWarConfig().Teams;

    if (config.Teams.Count > MaxTeams)
      config.Teams = config.Teams.Take(MaxTeams).ToList();

    while (config.Teams.Count < MaxTeams)
      config.Teams.Add(defaults[config.Teams.Count]);

    for (int i = 0; i < config.Teams.Count; i++)
    {
      var team = config.Teams[i];
      team.Name = team.Name.Trim();

      if (team.Name.Length == 0)
        team.Name = defaults[i].Name;

      team.Model = team.Model.Trim();
    }

    config.Sound.FireVolume = Math.Clamp(config.Sound.FireVolume, 0f, 1f);
    config.Sound.BounceVolume = Math.Clamp(config.Sound.BounceVolume, 0f, 1f);

    config.Flash.A = Math.Clamp(config.Flash.A, 0, 255);

    Config = config;

    _teamColors.Clear();
    foreach (var team in config.Teams)
      _teamColors.Add(Util.ParseColor(team.Color, System.Drawing.Color.OrangeRed));

    _weapon = config.Weapons[0];
    _gravity = config.Gravity[0];
  }

  private void LoadSettings()
  {
    try
    {
      if (!File.Exists(SettingsPath))
        return;

      var saved = System.Text.Json.JsonSerializer.Deserialize<LaserSettings>(File.ReadAllText(SettingsPath));
      if (saved == null)
        return;

      _weapon = Config.Weapons.Contains(saved.Weapon) ? saved.Weapon : Config.Weapons[0];
      _hitsToKill = Math.Clamp(saved.Hits, 1, 2);
      _bounces = Math.Clamp(saved.Bounces, 1, MaxBounces);
      _teamCount = Math.Clamp(saved.TeamCount, MinTeams, MaxTeams);
      _gravity = Config.Gravity.Contains(saved.Gravity) ? saved.Gravity : Config.Gravity[0];
      _laserSound = saved.LaserSound;
      _infiniteAmmo = saved.InfiniteAmmo;
    }
    catch
    {
    }
  }

  private void SaveSettings()
  {
    var snapshot = new LaserSettings
    {
      Weapon = _weapon,
      Hits = _hitsToKill,
      Bounces = _bounces,
      TeamCount = _teamCount,
      Gravity = _gravity,
      LaserSound = _laserSound,
      InfiniteAmmo = _infiniteAmmo
    };

    var path = SettingsPath;

    Task.Run(() =>
    {
      try
      {
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(snapshot,
          new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
      }
      catch
      {
      }
    });
  }

  public override void Load(bool hotReload)
  {
    LoadSettings();

    _menus = new WasdMenuManager(this,
      () => Localizer["menu.scroll"],
      () => Localizer["menu.select"],
      () => Localizer["menu.exit"]);

    HudGuard.Install(this);

    foreach (var name in Util.Split(Config.Commands))
      AddCommand(name, "LaserWar menusunu acar", OnLaserWarCommand);

    RegisterListener<OnServerPrecacheResources>(PrecacheResources);
    RegisterListener<OnTick>(OnTick);
    RegisterListener<OnMapStart>(OnMapStart);
    RegisterListener<OnMapEnd>(ClearBeams);
    RegisterListener<OnEntityTakeDamagePre>(OnTakeDamagePre);

    RegisterEventHandler<EventWeaponFire>(OnWeaponFire);
    RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    RegisterEventHandler<EventPlayerDeath>(OnPlayerDeathPre, HookMode.Pre);
    RegisterEventHandler<EventPlayerDeath>(OnPlayerDeathPost, HookMode.Post);
    RegisterEventHandler<EventRoundStart>(OnRoundStart);
    RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
  }

  public override void Unload(bool hotReload)
  {
    _menus.Clear();
    ClearBeams();
    RestoreCvars();
  }

  private void PrecacheResources(ResourceManifest manifest)
  {
    manifest.AddResource(BeamSprite);

    foreach (var team in Config.Teams)
    {
      if (team.Model.Length > 0)
        manifest.AddResource(team.Model);
    }
  }

  private void OnMapStart(string map)
  {
    _savedCvars.Clear();
    _active = false;
    ResetRound();
  }

  private HookResult OnRoundStart(EventRoundStart ev, GameEventInfo info)
  {
    ClearBeams();
    ResetRound();
    return HookResult.Continue;
  }

  private HookResult OnRoundEnd(EventRoundEnd ev, GameEventInfo info)
  {
    ClearBeams();
    ResetGame();
    return HookResult.Continue;
  }

  private HookResult OnPlayerSpawn(EventPlayerSpawn ev, GameEventInfo info)
  {
    var player = ev.Userid;
    int id = Util.UserId(player);
    if (id < 0)
      return HookResult.Continue;

    _hits.Remove(id);
    _dying.Remove(id);

    if (!_active)
      return HookResult.Continue;

    Server.NextFrame(() =>
    {
      if (!_active)
        return;

      if (_teamCount > 1 && InGameTeam(player!))
        AssignTeam(player!, SmallestTeam());

      Equip(player);
    });

    return HookResult.Continue;
  }

  private HookResult OnPlayerDeathPost(EventPlayerDeath ev, GameEventInfo info)
  {
    if (_active)
      Server.NextFrame(CheckSurvivors);

    return HookResult.Continue;
  }

  private HookResult OnWeaponFire(EventWeaponFire ev, GameEventInfo info)
  {
    if (!_active)
      return HookResult.Continue;

    var player = ev.Userid;
    if (player == null || !player.IsValid)
      return HookResult.Continue;

    var pawn = player.PlayerPawn.Value;
    var weapon = pawn?.WeaponServices?.ActiveWeapon?.Value;
    if (weapon == null || !weapon.IsValid)
      return HookResult.Continue;

    if (InGameTeam(player) && WeaponName(weapon) != _weapon)
    {
      Punish(player);
      return HookResult.Continue;
    }

    if (!FiresLaser(weapon.DesignerName))
      return HookResult.Continue;

    Shoot(player);
    return HookResult.Continue;
  }

  private void Punish(CCSPlayerController player)
  {
    int id = Util.UserId(player);
    if (id < 0 || _dying.Contains(id) || !Util.IsAlive(player))
      return;

    var pawn = player.PlayerPawn.Value;
    if (pawn == null || !pawn.IsValid)
      return;

    _dying.Add(id);
    _hits.Remove(id);
    _credit.Remove(id);

    Announce(Text("lw.cheater", player.PlayerName));

    Server.NextFrame(() =>
    {
      if (pawn.IsValid && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
        pawn.CommitSuicide(false, true);
    });
  }

  private HookResult OnTakeDamagePre(CEntityInstance victimEnt, CTakeDamageInfo info)
  {
    if (!_active || victimEnt == null || victimEnt.DesignerName != "player" || info.Damage <= 0f)
      return HookResult.Continue;

    var attacker = info.Attacker.Value;
    if (attacker == null || !attacker.IsValid || attacker.DesignerName != "player")
      return HookResult.Continue;

    if (attacker.Handle == victimEnt.Handle)
      return HookResult.Continue;

    info.Damage = 0f;
    return HookResult.Handled;
  }

  private HookResult OnPlayerDeathPre(EventPlayerDeath ev, GameEventInfo info)
  {
    var victim = ev.Userid;
    int id = Util.UserId(victim);
    if (id < 0)
      return HookResult.Continue;

    _hits.Remove(id);

    if (!_credit.TryGetValue(id, out var credit) || credit.Tick < Server.TickCount)
      return HookResult.Continue;

    _credit.Remove(id);

    var attacker = Utilities.GetPlayerFromUserid(credit.From);
    if (attacker == null || !attacker.IsValid || Util.UserId(attacker) == id)
      return HookResult.Continue;

    ev.Attacker = attacker;
    ev.Weapon = Config.KillfeedIcon;
    ev.Headshot = false;

    var stats = attacker.ActionTrackingServices?.MatchStats;
    if (stats != null)
    {
      stats.Kills++;
      Utilities.SetStateChanged(attacker, "CCSPlayerController", "m_pActionTrackingServices");
    }

    return HookResult.Continue;
  }

  private bool Start(CCSPlayerController starter)
  {
    var players = Utilities.GetPlayers().Where(InGameTeam).ToList();

    if (players.Count < _teamCount || players.Count % _teamCount != 0)
    {
      Tell(starter, Text("lw.uneven", players.Count, _teamCount));
      return false;
    }

    _active = true;
    ResetRound();
    ClearBeams();
    ApplyCvars();

    if (_teamCount > 1)
      SplitTeams(players);

    foreach (var player in players)
      Equip(player);

    Announce(Text("lw.started", starter.PlayerName, WeaponLabel(_weapon), DamageLabel(), TeamCountLabel()));

    if (_teamCount > 1)
      foreach (var player in players)
        Tell(player, Text("lw.your_team", TeamName(TeamOf(player))));

    return true;
  }

  private void Stop(CCSPlayerController stopper)
  {
    ResetGame();
    Announce(Text("lw.stopped", stopper.PlayerName));
  }

  private void ResetGame()
  {
    if (!_active)
      return;

    _active = false;
    ClearBeams();
    RestoreCvars();

    foreach (var player in Utilities.GetPlayers())
    {
      bool alive = Util.IsAlive(player);

      ResetAppearance(player, alive);

      if (!alive)
      {
        _savedHealth.Remove(Util.UserId(player));
        continue;
      }

      RestoreHealth(player);
      RestoreLoadout(player);
    }

    ResetRound();
  }

  private void ResetRound()
  {
    _hits.Clear();
    _dying.Clear();
    _credit.Clear();
    _team.Clear();
    _loadout.Clear();
    _savedModel.Clear();
    _savedHealth.Clear();
  }

  private void CheckSurvivors()
  {
    if (!_active)
      return;

    var survivors = Utilities.GetPlayers()
      .Where(player => player.IsValid && InGameTeam(player) && Util.IsAlive(player))
      .ToList();

    if (_teamCount <= 1)
    {
      if (survivors.Count > 1)
        return;

      if (survivors.Count == 1)
        Announce(Text("lw.winner", survivors[0].PlayerName));

      ResetGame();
      return;
    }

    var sides = survivors.Select(TeamOf).Distinct().ToList();
    if (sides.Count > 1)
      return;

    if (sides.Count == 1)
      Announce(Text("lw.team_won", TeamName(sides[0])));

    ResetGame();
  }

  private void Equip(CCSPlayerController? player)
  {
    int id = Util.UserId(player);
    if (id < 0 || !Util.IsAlive(player!))
      return;

    _hits.Remove(id);

    SaveHealth(player!);
    SaveLoadout(player!);
    GiveOnly(player!, _weapon);
  }

  private void SaveHealth(CCSPlayerController player)
  {
    int id = Util.UserId(player);
    var pawn = player.PlayerPawn.Value;

    if (id < 0 || pawn == null || !pawn.IsValid)
      return;

    if (!_savedHealth.ContainsKey(id))
      _savedHealth[id] = new Health(pawn.Health, pawn.MaxHealth);

    SetHealth(pawn, GameHealth, GameHealth);
  }

  private void RestoreHealth(CCSPlayerController player)
  {
    int id = Util.UserId(player);
    if (id < 0 || !_savedHealth.Remove(id, out var health))
      return;

    var pawn = player.PlayerPawn.Value;
    if (pawn == null || !pawn.IsValid)
      return;

    SetHealth(pawn, health.Current, health.Max);
  }

  private static void SetHealth(CCSPlayerPawn pawn, int current, int max)
  {
    pawn.MaxHealth = Math.Max(1, max);
    pawn.Health = Math.Clamp(current, 1, pawn.MaxHealth);

    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
  }

  private void ReEquipAll()
  {
    if (!_active)
      return;

    foreach (var player in Utilities.GetPlayers())
    {
      if (Util.IsAlive(player))
        GiveOnly(player, _weapon);
    }
  }

  private static void GiveOnly(CCSPlayerController player, string weapon)
  {
    if (!StripWeapons(player, weapon))
      player.GiveNamedItem(weapon);
  }

  private static bool StripWeapons(CCSPlayerController player, string? keep = null)
  {
    var weapons = player.PlayerPawn.Value?.WeaponServices?.MyWeapons;
    if (weapons == null)
      return false;

    bool kept = false;
    var doomed = new List<CBasePlayerWeapon>();

    foreach (var handle in weapons)
    {
      var entity = handle?.Value;
      if (entity == null || !entity.IsValid)
        continue;

      var name = WeaponName(entity);
      if (string.IsNullOrEmpty(name) || !name.StartsWith("weapon_", StringComparison.Ordinal))
        continue;

      if (name == BombWeapon)
        continue;

      if (keep != null && name == keep && !kept)
      {
        kept = true;
        continue;
      }

      doomed.Add(entity);
    }

    foreach (var entity in doomed)
    {
      if (entity.IsValid)
        entity.AddEntityIOEvent("Kill", entity, null, "", KillDelay);
    }

    return kept;
  }

  private void SaveLoadout(CCSPlayerController player)
  {
    int id = Util.UserId(player);
    if (id < 0 || _loadout.ContainsKey(id))
      return;

    var weapons = player.PlayerPawn.Value?.WeaponServices?.MyWeapons;
    if (weapons == null)
      return;

    var saved = new List<string>();

    foreach (var handle in weapons)
    {
      var weapon = handle?.Value;
      if (weapon == null || !weapon.IsValid)
        continue;

      var name = WeaponName(weapon);
      if (string.IsNullOrEmpty(name) || name == BombWeapon)
        continue;

      if (name.StartsWith("weapon_", StringComparison.Ordinal))
        saved.Add(name);
    }

    _loadout[id] = saved;
  }

  private void RestoreLoadout(CCSPlayerController player)
  {
    int id = Util.UserId(player);
    if (id < 0 || !_loadout.Remove(id, out var saved))
      return;

    StripWeapons(player);

    foreach (var name in saved)
      player.GiveNamedItem(name);
  }

  private void SplitTeams(List<CCSPlayerController> players)
  {
    var shuffled = players.OrderBy(_ => Random.Shared.Next()).ToList();

    for (int i = 0; i < shuffled.Count; i++)
      AssignTeam(shuffled[i], i % _teamCount);
  }

  private void AssignTeam(CCSPlayerController player, int team)
  {
    int id = Util.UserId(player);
    if (id < 0)
      return;

    _team[id] = team;

    var pawn = player.PlayerPawn.Value;
    if (pawn == null || !pawn.IsValid)
      return;

    pawn.Render = TeamColor(team);
    Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

    ApplyModel(player, pawn, Config.Teams[Math.Clamp(team, 0, Config.Teams.Count - 1)].Model);
  }

  private void ApplyModel(CCSPlayerController player, CCSPlayerPawn pawn, string model)
  {
    if (model.Length == 0)
      return;

    int id = Util.UserId(player);
    if (id < 0)
      return;

    try
    {
      var current = CurrentModel(pawn);
      if (!_savedModel.ContainsKey(id) && !string.IsNullOrEmpty(current))
        _savedModel[id] = current;

      pawn.SetModel(model);
    }
    catch
    {
    }
  }

  private static string? CurrentModel(CCSPlayerPawn pawn)
  {
    try
    {
      return pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance().ModelState.ModelName;
    }
    catch
    {
      return null;
    }
  }

  private int SmallestTeam()
  {
    var counts = new int[_teamCount];

    foreach (var player in Utilities.GetPlayers())
    {
      int team = TeamOf(player);
      if (team >= 0 && team < _teamCount && Util.IsAlive(player))
        counts[team]++;
    }

    int smallest = 0;

    for (int team = 1; team < counts.Length; team++)
    {
      if (counts[team] < counts[smallest])
        smallest = team;
    }

    return smallest;
  }

  private void ResetAppearance(CCSPlayerController player, bool alive)
  {
    int id = Util.UserId(player);
    string? model = null;

    if (id >= 0)
      _savedModel.Remove(id, out model);

    var pawn = player.PlayerPawn.Value;
    if (pawn == null || !pawn.IsValid)
      return;

    pawn.Render = System.Drawing.Color.FromArgb(255, 255, 255, 255);
    Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

    if (!alive || string.IsNullOrEmpty(model))
      return;

    try
    {
      pawn.SetModel(model);
    }
    catch
    {
    }
  }

  private System.Drawing.Color TeamColor(int team) =>
    _teamColors[Math.Clamp(team, 0, _teamColors.Count - 1)];

  private string TeamName(int team) =>
    Config.Teams[Math.Clamp(team, 0, Config.Teams.Count - 1)].Name;

  private static bool InGameTeam(CCSPlayerController player) =>
    player.IsValid && player.TeamNum == (byte)CsTeam.Terrorist;
  private int TeamOf(CCSPlayerController player)
  {
    int id = Util.UserId(player);
    return id >= 0 && _team.TryGetValue(id, out var team) ? team : -1;
  }

  private static string WeaponName(CBasePlayerWeapon weapon)
  {
    try
    {
      if (VariantNames.TryGetValue(weapon.AttributeManager.Item.ItemDefinitionIndex, out var name))
        return name;
    }
    catch
    {
    }

    return weapon.DesignerName;
  }

  private static string NormalizeWeapon(string name)
  {
    name = name.Trim().ToLowerInvariant();
    if (name.Length == 0)
      return "";

    return name.StartsWith("weapon_", StringComparison.Ordinal) || name.StartsWith("item_", StringComparison.Ordinal)
      ? name
      : "weapon_" + name;
  }

  private static bool FiresLaser(string designerName)
  {
    if (string.IsNullOrEmpty(designerName) || !designerName.StartsWith("weapon_", StringComparison.Ordinal))
      return false;

    return designerName switch
    {
      "weapon_c4" or "weapon_taser" or "weapon_healthshot" => false,
      _ => !designerName.Contains("knife")
        && !designerName.Contains("bayonet")
        && !designerName.Contains("grenade")
        && !designerName.Contains("molotov")
        && !designerName.Contains("incgrenade")
        && !designerName.Contains("flashbang")
        && !designerName.Contains("decoy")
    };
  }

  private void ApplyCvars()
  {
    RestoreCvars();

    foreach (var (name, value) in GameCvars)
      SetCvar(name, value);

    ApplyAmmoCvar();
    ApplyGravityCvar();
  }

  private void ApplyAmmoCvar()
  {
    if (_active)
      SetCvar(AmmoCvar, _infiniteAmmo ? 1f : 0f);
  }

  private void ApplyGravityCvar()
  {
    if (_active)
      SetCvar(GravityCvar, BaseGravity * _gravity);
  }

  private void SetCvar(string name, float value)
  {
    var cvar = ConVar.Find(name);
    if (cvar == null)
      return;

    try
    {
      string kind = cvar.Type.ToString().ToLowerInvariant();

      if (kind.Contains("bool"))
      {
        if (!_savedCvars.ContainsKey(name))
          _savedCvars[name] = cvar.GetPrimitiveValue<bool>();

        cvar.SetValue(value != 0f);
      }
      else if (kind.Contains("float") || kind.Contains("double"))
      {
        if (!_savedCvars.ContainsKey(name))
          _savedCvars[name] = cvar.GetPrimitiveValue<float>();

        cvar.SetValue(value);
      }
      else if (kind.Contains("int"))
      {
        if (!_savedCvars.ContainsKey(name))
          _savedCvars[name] = cvar.GetPrimitiveValue<int>();

        cvar.SetValue((int)value);
      }
    }
    catch
    {
      _savedCvars.Remove(name);
    }
  }

  private void RestoreCvars()
  {
    foreach (var (name, value) in _savedCvars)
    {
      var cvar = ConVar.Find(name);
      if (cvar == null)
        continue;

      try
      {
        switch (value)
        {
          case bool flag:
            cvar.SetValue(flag);
            break;
          case int number:
            cvar.SetValue(number);
            break;
          case float number:
            cvar.SetValue(number);
            break;
        }
      }
      catch
      {
      }
    }

    _savedCvars.Clear();
  }

  private void OnLaserHit(CCSPlayerController attacker, CCSPlayerController victim)
  {
    int id = Util.UserId(victim);
    int attackerId = Util.UserId(attacker);

    if (id < 0 || attackerId < 0 || id == attackerId)
      return;

    if (_dying.Contains(attackerId) || !Util.IsAlive(attacker) || _dying.Contains(id))
      return;

    var pawn = victim.PlayerPawn.Value;
    if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
      return;

    _hits.TryGetValue(id, out int hits);
    hits++;

    if (hits < _hitsToKill)
    {
      _hits[id] = hits;

      pawn.Health = Math.Max(1, GameHealth / 2);
      Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

      Flash(victim);
      return;
    }

    _hits.Remove(id);
    _dying.Add(id);
    _credit[id] = new Credit(attackerId, Server.TickCount + 64);

    pawn.Health = 0;
    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

    Server.NextFrame(() =>
    {
      if (pawn.IsValid && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
        pawn.CommitSuicide(false, true);
    });
  }

  private void Flash(CCSPlayerController player)
  {
    if (Config.Flash.A <= 0)
      return;

    using var msg = UserMessage.FromPartialName("Fade");
    if (msg == null)
      return;

    int color = (Config.Flash.A << 24) | (Config.Flash.B << 16) | (Config.Flash.G << 8) | Config.Flash.R;

    msg.SetInt("duration", Config.Flash.Duration);
    msg.SetInt("hold_time", Config.Flash.HoldTime);
    msg.SetInt("flags", 1);
    msg.SetInt("color", color);
    msg.Send(player);
  }

  private string Text(string key, params object[] args)
  {
    string raw = CC.Parse(Localizer[key].ToString());
    return args.Length == 0 ? raw : string.Format(raw, args);
  }

  private void Announce(string message)
  {
    Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {message}");
  }

  private void Tell(CCSPlayerController player, string message)
  {
    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {message}");
  }
}
