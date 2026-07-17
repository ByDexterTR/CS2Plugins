using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using ByDexter.Shared;

namespace CTRev;

public class CTRevConfig : BasePluginConfig
{
  [JsonPropertyName("cooldown")] public int RespawnCooldownSeconds { get; set; } = 15;
  [JsonPropertyName("revive_count")] public int MaxRespawnsPerRound { get; set; } = 3;
  [JsonPropertyName("ctrev_cmd")] public string CtrevCommands { get; set; } = "css_ctrev,css_ctr,css_ctrevmenu";
  [JsonPropertyName("ctrev_flag")] public string CtrevFlag { get; set; } = "@jailbreak/warden,@css/generic";
  [JsonPropertyName("haksifirla_cmd")] public string HakResetCommands { get; set; } = "css_hak0,css_haksifir,css_hakreset";
  [JsonPropertyName("hak_flag")] public string HakFlag { get; set; } = "@jailbreak/warden,@css/generic";
}

public class CTRev : BasePlugin, IPluginConfig<CTRevConfig>
{
  public override string ModuleName => "CTRev";
  public override string ModuleVersion => "1.0.6";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public CTRevConfig Config { get; set; } = new();

  private int _remainingRespawns;
  private bool _autoRespawnEnabled;
  private readonly Dictionary<ulong, DateTime> _ctDeathEligibleAt = new();
  private readonly HashSet<ulong> _playersWithMenuOpen = new();

  private WasdMenuManager _menus = null!;

  public void OnConfigParsed(CTRevConfig config)
  {
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    _menus = new WasdMenuManager(this,
      () => Localizer["menu.scroll"],
      () => Localizer["menu.select"],
      () => Localizer["menu.exit"]);
    RegisterEventHandler<EventRoundStart>(OnRoundStart);
    RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
    RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);

    foreach (var name in Util.Split(Config.CtrevCommands))
      AddCommand(name, "CT canlandirma menusu", OnCtrMenu);

    foreach (var name in Util.Split(Config.HakResetCommands))
      AddCommand(name, "Canlandirma haklarini sifirlar", OnHakSifirla);

    AddTimer(1.0f, OnTimerTick, TimerFlags.REPEAT);
  }

  public override void Unload(bool hotReload)
  {
    _menus.Clear();
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
  {
    _remainingRespawns = Config.MaxRespawnsPerRound;
    _ctDeathEligibleAt.Clear();
    _playersWithMenuOpen.Clear();
    return HookResult.Continue;
  }

  private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
  {
    var victim = @event.Userid;
    if (victim == null || !victim.IsValid || victim.Team != CsTeam.CounterTerrorist)
      return HookResult.Continue;

    _ctDeathEligibleAt[victim.SteamID] = DateTime.UtcNow.AddSeconds(Config.RespawnCooldownSeconds);
    return HookResult.Continue;
  }

  private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid)
      return HookResult.Continue;

    _ctDeathEligibleAt.Remove(player.SteamID);
    return HookResult.Continue;
  }

  private void OnTimerTick()
  {
    if (_autoRespawnEnabled && _remainingRespawns > 0)
    {
      var now = DateTime.UtcNow;
      foreach (var p in Utilities.GetPlayers())
      {
        if (p == null || !p.IsValid || p.Team != CsTeam.CounterTerrorist || IsAlive(p) || _remainingRespawns <= 0)
          continue;

        if (_ctDeathEligibleAt.TryGetValue(p.SteamID, out var eligibleAt) && now >= eligibleAt)
        {
          p.Respawn();
          _ctDeathEligibleAt.Remove(p.SteamID);
          _remainingRespawns--;
          Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctrev.auto_revived", p.PlayerName, _remainingRespawns]}");
        }
      }
    }

    if (_playersWithMenuOpen.Count > 0)
    {
      foreach (var steamId in _playersWithMenuOpen.ToList())
      {
        var player = Utilities.GetPlayerFromSteamId(steamId);
        if (player == null || !player.IsValid)
        {
          _playersWithMenuOpen.Remove(steamId);
          continue;
        }

        if (_menus.IsOpen(player))
          ShowMainMenu(player);
        else
          _playersWithMenuOpen.Remove(steamId);
      }
    }
  }

  public void OnCtrMenu(CCSPlayerController? player, CommandInfo command)
  {
    if (player == null || !player.IsValid)
      return;

    if (!Util.HasAccess(player, Config.CtrevFlag))
      return;

    _playersWithMenuOpen.Add(player.SteamID);
    ShowMainMenu(player);
  }

  public void OnHakSifirla(CCSPlayerController? player, CommandInfo command)
  {
    if (player == null || !player.IsValid)
      return;

    if (!Util.HasAccess(player, Config.HakFlag))
      return;

    _remainingRespawns = Config.MaxRespawnsPerRound;
    Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctrev.rights_reset", player?.PlayerName ?? "", _remainingRespawns]}");
  }

  private void ShowMainMenu(CCSPlayerController? player)
  {
    if (player == null || !player.IsValid)
      return;

    var now = DateTime.UtcNow;
    var items = new List<WasdItem>
    {
      new()
      {
        Text = Localizer["ctrev.menu_auto_rev", _autoRespawnEnabled ? Localizer["ctrev.state_on"] : Localizer["ctrev.state_off"]],
        OnSelect = p =>
        {
          _autoRespawnEnabled = !_autoRespawnEnabled;
          p.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctrev.menu_auto_rev", _autoRespawnEnabled ? Localizer["ctrev.state_on"] : Localizer["ctrev.state_off"]]}");
          ShowMainMenu(p);
        }
      }
    };

    var deadCts = Utilities.GetPlayers().Where(pl => pl != null && pl.IsValid && pl.Team == CsTeam.CounterTerrorist && !IsAlive(pl)).ToList();

    foreach (var ct in deadCts)
    {
      var remain = _ctDeathEligibleAt.TryGetValue(ct.SteamID, out var at) ? Math.Max(0, (int)Math.Ceiling((at - now).TotalSeconds)) : 0;
      if (remain == 0)
      {
        items.Add(new WasdItem
        {
          Text = $"<font color='#76C97A'>{ct.PlayerName}</font>",
          OnSelect = p => TryRespawn(p, ct)
        });
      }
      else
      {
        items.Add(new WasdItem
        {
          Text = Localizer["ctrev.menu_cooldown", ct.PlayerName, remain],
          Enabled = false
        });
      }
    }

    _menus.Open(player, Localizer["ctrev.menu_title", _remainingRespawns], items);
  }

  private void TryRespawn(CCSPlayerController actor, CCSPlayerController target)
  {
    if (_remainingRespawns <= 0)
    {
      actor.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctrev.no_rights"]}");
      return;
    }

    if (target == null || !target.IsValid || target.Team != CsTeam.CounterTerrorist)
    {
      return;
    }

    if (IsAlive(target))
    {
      actor.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctrev.already_alive"]}");
      return;
    }

    var now = DateTime.UtcNow;
    if (_ctDeathEligibleAt.TryGetValue(target.SteamID, out var at) && now < at)
    {
      var remain = Math.Max(0, (int)Math.Ceiling((at - now).TotalSeconds));
      actor.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctrev.cooldown_wait", remain]}");
      return;
    }

    target.Respawn();
    _ctDeathEligibleAt.Remove(target.SteamID);
    _remainingRespawns--;
    Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctrev.revived", actor.PlayerName, target.PlayerName, _remainingRespawns]}");
  }

  private static bool IsAlive(CCSPlayerController? player)
  {
    return player?.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
  }
}
