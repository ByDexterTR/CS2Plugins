using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Drawing;

public class CTSpawnKillConfig : BasePluginConfig
{
  [JsonPropertyName("chat_prefix")] public string ChatPrefix { get; set; } = "[ByDexter]";
  [JsonPropertyName("spawn_protect_seconds")] public int SpawnProtectSeconds { get; set; } = 5;
}

public class CTSpawnKill : BasePlugin, IPluginConfig<CTSpawnKillConfig>
{
  public override string ModuleName => "CTSpawnKill";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "CT doğumunda geçici ölümsüzlük (spawn kill önleme)";

  public CTSpawnKillConfig Config { get; set; } = new();

  private readonly Dictionary<ulong, long> _protectedCTs = new();
  private readonly Dictionary<ulong, CounterStrikeSharp.API.Modules.Timers.Timer> _timers = new();

  public void OnConfigParsed(CTSpawnKillConfig config)
  {
    if (config.SpawnProtectSeconds < 1)
      config.SpawnProtectSeconds = 1;
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
    RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
  }

  public override void Unload(bool hotReload)
  {
    try { VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre); } catch { }
    foreach (var kv in _timers)
      kv.Value.Kill();
    _timers.Clear();
    _protectedCTs.Clear();
  }

  private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid || player.Team != CsTeam.CounterTerrorist || !IsAlive(player))
      return HookResult.Continue;

    StartProtection(player);
    return HookResult.Continue;
  }

  private void StartProtection(CCSPlayerController player)
  {
    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    _protectedCTs[player.SteamID] = now + Config.SpawnProtectSeconds * 1000L;
    var pawn = player.PlayerPawn.Value;
    if (pawn != null)
    {
      pawn.Render = Color.FromArgb(200, 254, 190, 0);
      Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }

    if (_timers.TryGetValue(player.SteamID, out var t))
    {
      t.Kill();
      _timers.Remove(player.SteamID);
    }

    player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {CC.Green}{Config.SpawnProtectSeconds} saniye{CC.Default} spawn koruması aktif!");

    var timer = AddTimer(Config.SpawnProtectSeconds, () => EndProtection(player.SteamID), TimerFlags.STOP_ON_MAPCHANGE);
    _timers[player.SteamID] = timer;
  }

  private void EndProtection(ulong steamId)
  {
    _protectedCTs.Remove(steamId);
    _timers.Remove(steamId);

    var player = Utilities.GetPlayers().FirstOrDefault(p => p.SteamID == steamId);
    if (player == null || !player.IsValid)
      return;

    var pawn = player.PlayerPawn.Value;
    if (pawn != null)
    {
      pawn.Render = Color.FromArgb(255, 255, 255, 255);
      Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }
    player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Spawn koruması {CC.Red}sona erdi{CC.Default}!");
  }

  public HookResult OnTakeDamage(DynamicHook h)
  {
    try
    {
      var victimEnt = h.GetParam<CEntityInstance>(0);
      var info = h.GetParam<CTakeDamageInfo>(1);
      if (victimEnt == null || info == null)
        return HookResult.Continue;

      CCSPlayerPawn? victimPawn = victimEnt as CCSPlayerPawn ?? new CCSPlayerPawn(victimEnt.Handle);
      var victimController = victimPawn?.OriginalController.Value;
      if (victimController == null || !victimController.IsValid)
        return HookResult.Continue;

      if (victimController.Team != CsTeam.CounterTerrorist)
        return HookResult.Continue;

      if (!_protectedCTs.TryGetValue(victimController.SteamID, out var until))
        return HookResult.Continue;

      var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
      if (now > until)
      {
        EndProtection(victimController.SteamID);
        return HookResult.Continue;
      }

      info.Damage = 0f;
      return HookResult.Changed;
    }
    catch { return HookResult.Continue; }
  }

  static bool IsAlive(CCSPlayerController? player)
  {
    return player?.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
  }
}

public static class CC
{
  public static char Default => '\x01';
  public static char Red => '\x07';
  public static char LightRed => '\x0F';
  public static char DarkRed => '\x02';
  public static char BlueGrey => '\x0A';
  public static char Blue => '\x0B';
  public static char DarkBlue => '\x0C';
  public static char Purple => '\x0C';
  public static char Orchid => '\x0E';
  public static char Yellow => '\x09';
  public static char Gold => '\x10';
  public static char LightGreen => '\x05';
  public static char Green => '\x04';
  public static char Lime => '\x06';
  public static char Grey => '\x08';
  public static char Grey2 => '\x0D';
}
