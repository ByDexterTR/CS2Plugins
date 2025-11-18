using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;

namespace CTRev;

public class CTRevConfig : BasePluginConfig
{
  [JsonPropertyName("chat_prefix")] public string ChatPrefix { get; set; } = "[ByDexter]";
  [JsonPropertyName("cooldown")] public int RespawnCooldownSeconds { get; set; } = 15;
  [JsonPropertyName("revive_count")] public int MaxRespawnsPerRound { get; set; } = 3;
}

public class CTRev : BasePlugin, IPluginConfig<CTRevConfig>
{
  public override string ModuleName => "CTRev";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "[JB] CT Revive Menü";

  public CTRevConfig Config { get; set; } = new();

  private int _remainingRespawns;
  private bool _autoRespawnEnabled;
  private readonly Dictionary<ulong, DateTime> _ctDeathEligibleAt = new();
  private readonly HashSet<ulong> _playersWithMenuOpen = new();

  public void OnConfigParsed(CTRevConfig config)
  {
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    RegisterEventHandler<EventRoundStart>(OnRoundStart);
    RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
    RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);

    AddTimer(1.0f, OnTimerTick, TimerFlags.REPEAT);
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
    if (victim == null || !victim.IsValid || victim.TeamNum != 3)
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
        if (p == null || !p.IsValid || p.TeamNum != 3 || IsAlive(p) || _remainingRespawns <= 0)
          continue;

        if (_ctDeathEligibleAt.TryGetValue(p.SteamID, out var eligibleAt) && now >= eligibleAt)
        {
          p.Respawn();
          _ctDeathEligibleAt.Remove(p.SteamID);
          _remainingRespawns--;
          Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Otomatik: {CC.Gold}{p.PlayerName}{CC.Default} canlandırıldı. Kalan hak: {CC.Gold}{_remainingRespawns}");
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

        if (MenuManager.GetActiveMenu(player) != null)
        {
          MenuManager.CloseActiveMenu(player);
          ShowMainMenu(player);
        }
        else
        {
          _playersWithMenuOpen.Remove(steamId);
        }
      }
    }
  }

  [ConsoleCommand("css_ctr", "CT respawn menu")]
  [ConsoleCommand("css_ctrev", "CT respawn menu")]
  [ConsoleCommand("css_ctrevmenu", "CT respawn menu")]
  [RequiresPermissionsOr("@css/generic", "@jailbreak/warden")]
  public void OnCtrMenu(CCSPlayerController? player, CommandInfo command)
  {
    if (player == null || !player.IsValid)
      return;

    _playersWithMenuOpen.Add(player.SteamID);
    ShowMainMenu(player);
  }

  [ConsoleCommand("css_hak0", "Hakları sıfırla")]
  [ConsoleCommand("css_haksifir", "Hakları sıfırla")]
  [ConsoleCommand("css_haksifirla", "Hakları sıfırla")]
  [RequiresPermissions("@css/generic")]
  public void OnHakSifirla(CCSPlayerController? player, CommandInfo command)
  {
    if (player == null || !player.IsValid)
      return;

    _remainingRespawns = Config.MaxRespawnsPerRound;
    Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {CC.Gold}{player?.PlayerName}{CC.Default} canlandırma haklarını {CC.Gold}sıfırlandı{CC.Default}. Toplam hak: {CC.Gold}{_remainingRespawns}");
  }

  private void ShowMainMenu(CCSPlayerController? player)
  {
    if (player == null || !player.IsValid)
      return;

    var now = DateTime.UtcNow;
    var menu = new CenterHtmlMenu($"<font color='#d63f25' class='fontSize-l'><img src='https://images.weserv.nl/?url=em-content.zobj.net/source/facebook/158/syringe_1f489.png&w=24&h=24&fit=cover'> CTRev (Hak: {_remainingRespawns}) <img src='https://images.weserv.nl/?url=em-content.zobj.net/source/facebook/158/syringe_1f489.png&w=24&h=24&fit=cover'></font>", this);

    menu.AddMenuOption($"Oto Rev: {(_autoRespawnEnabled ? "AÇIK" : "KAPALI")}", (p, option) =>
    {
      _autoRespawnEnabled = !_autoRespawnEnabled;
      p.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Oto Rev: {(_autoRespawnEnabled ? CC.Green + "AÇIK" + CC.Default : CC.Red + "KAPALI" + CC.Default)}");
    });

    var deadCts = Utilities.GetPlayers().Where(pl => pl != null && pl.IsValid && pl.TeamNum == 3 && !IsAlive(pl) && pl.SteamID != player.SteamID).ToList();

    if (!IsAlive(player))
    {
      var selfRemain = _ctDeathEligibleAt.TryGetValue(player.SteamID, out var selfAt) ? Math.Max(0, (int)Math.Ceiling((selfAt - now).TotalSeconds)) : 0;
      var label = selfRemain == 0 ? $"{player.PlayerName} (Hazır)" : $"{player.PlayerName} ({selfRemain}s)";
      menu.AddMenuOption(label, (p, option) =>
      {
        TryRespawn(p, p);
      });
    }

    if (deadCts.Count != 0)
    {
      foreach (var ct in deadCts)
      {
        var remain = _ctDeathEligibleAt.TryGetValue(ct.SteamID, out var at) ? Math.Max(0, (int)Math.Ceiling((at - now).TotalSeconds)) : 0;
        var label = remain == 0 ? $"{ct.PlayerName} (Hazır)" : $"{ct.PlayerName} ({remain}s)";
        menu.AddMenuOption(label, (p, option) =>
        {
          TryRespawn(p, ct);
        });
      }
    }

    MenuManager.OpenCenterHtmlMenu(this, player, menu);
  }

  private void TryRespawn(CCSPlayerController actor, CCSPlayerController target)
  {
    if (_remainingRespawns <= 0)
    {
      actor.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Canlandırma hakkı {CC.Red}bitti{CC.Default}.");
      return;
    }

    if (target == null || !target.IsValid || target.TeamNum != 3)
    {
      return;
    }

    if (IsAlive(target))
    {
      actor.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Gardiyan {CC.Gold}zaten canlı{CC.Default}.");
      return;
    }

    var now = DateTime.UtcNow;
    if (_ctDeathEligibleAt.TryGetValue(target.SteamID, out var at) && now < at)
    {
      var remain = Math.Max(0, (int)Math.Ceiling((at - now).TotalSeconds));
      actor.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} Bekleme süresi: {CC.Gold}{remain}sn{CC.Default}.");
      return;
    }

    target.Respawn();
    _ctDeathEligibleAt.Remove(target.SteamID);
    _remainingRespawns--;
    Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {CC.Gold}{actor.PlayerName}{CC.Default}: {CC.Gold}{target.PlayerName}{CC.Default} canlandırıldı. Kalan hak: {CC.Gold}{_remainingRespawns}");
  }

  private static bool IsAlive(CCSPlayerController? player)
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
