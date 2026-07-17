using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using static CounterStrikeSharp.API.Core.Listeners;

public class CTSpawnKillConfig : BasePluginConfig
{
  [JsonPropertyName("spawn_protect_seconds")] public int SpawnProtectSeconds { get; set; } = 5;
}

public class CTSpawnKill : BasePlugin, IPluginConfig<CTSpawnKillConfig>
{
  public override string ModuleName => "CTSpawnKill";
  public override string ModuleVersion => "1.0.4";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

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
    RegisterListener<OnEntityTakeDamagePre>(HandleEntityDamage);
    RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
  }

  private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null)
      return HookResult.Continue;

    var steamId = player.SteamID;
    _protectedCTs.Remove(steamId);

    if (_timers.TryGetValue(steamId, out var timer))
    {
      timer.Kill();
      _timers.Remove(steamId);
    }

    return HookResult.Continue;
  }

  public override void Unload(bool hotReload)
  {
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
    var steamId = player.SteamID;

    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    _protectedCTs[steamId] = now + Config.SpawnProtectSeconds * 1000L;
    var pawn = player.PlayerPawn.Value;
    if (pawn != null)
    {
      pawn.Render = Color.FromArgb(200, 254, 190, 0);
      Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }

    if (_timers.TryGetValue(steamId, out var t))
    {
      t.Kill();
      _timers.Remove(steamId);
    }

    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctspawnkill.protection_active", Config.SpawnProtectSeconds]}");

    var timer = AddTimer(Config.SpawnProtectSeconds, () => EndProtection(steamId), TimerFlags.STOP_ON_MAPCHANGE);
    _timers[steamId] = timer;
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
    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctspawnkill.protection_ended"]}");
  }

  private HookResult HandleEntityDamage(CEntityInstance victimEnt, CTakeDamageInfo info)
  {
    try
    {
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
      return HookResult.Continue;
    }
    catch { return HookResult.Continue; }
  }

  static bool IsAlive(CCSPlayerController? player)
  {
    return player?.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
  }
}
