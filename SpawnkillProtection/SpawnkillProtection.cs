using System.Drawing;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace SpawnkillProtection;

public class FlagProtection
{
  [JsonPropertyName("flag")]
  public string Flag { get; set; } = "";

  [JsonPropertyName("seconds")]
  public float Seconds { get; set; } = 5f;

  [JsonPropertyName("color")]
  public int[] Color { get; set; } = [255, 215, 0];
}

public class TeamProtection
{
  [JsonPropertyName("enabled")]
  public bool Enabled { get; set; } = true;

  [JsonPropertyName("seconds")]
  public float Seconds { get; set; } = 5f;

  [JsonPropertyName("color")]
  public int[] Color { get; set; } = [255, 255, 255];
}

public class SpawnkillProtectionConfig : BasePluginConfig
{
  [JsonPropertyName("flag_protections")]
  public List<FlagProtection> FlagProtections { get; set; } = new()
  {
    new FlagProtection { Flag = "@css/vip", Seconds = 8f, Color = [255, 215, 0] }
  };

  [JsonPropertyName("team_t")]
  public TeamProtection TeamT { get; set; } = new() { Enabled = true, Seconds = 5f, Color = [255, 64, 64] };

  [JsonPropertyName("team_ct")]
  public TeamProtection TeamCT { get; set; } = new() { Enabled = true, Seconds = 5f, Color = [64, 128, 255] };
}

public class SpawnkillProtection : BasePlugin, IPluginConfig<SpawnkillProtectionConfig>
{
  public override string ModuleName => "SpawnkillProtection";
  public override string ModuleVersion => "1.0.1";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public SpawnkillProtectionConfig Config { get; set; } = new();

  private class ProtectionState
  {
    public float Start;
    public float End;
    public Color BaseColor;
  }

  private readonly Dictionary<int, ProtectionState> _protections = new();

  public void OnConfigParsed(SpawnkillProtectionConfig config)
  {
    foreach (var flagProt in config.FlagProtections)
      if (flagProt.Seconds < 0f)
        flagProt.Seconds = 0f;

    if (config.TeamT.Seconds < 0f)
      config.TeamT.Seconds = 0f;
    if (config.TeamCT.Seconds < 0f)
      config.TeamCT.Seconds = 0f;

    Config = config;
  }

  public override void Load(bool hotReload)
  {
    RegisterListener<OnEntityTakeDamagePre>(HandleEntityDamage);
    RegisterListener<OnTick>(OnTick);
    RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
    RegisterEventHandler<EventRoundStart>(OnRoundStart);
  }

  public override void Unload(bool hotReload)
  {
    foreach (var userId in _protections.Keys.ToList())
      EndProtection(userId, resetColor: true, announce: false);
    _protections.Clear();
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
  {
    foreach (var userId in _protections.Keys.ToList())
      EndProtection(userId, resetColor: true, announce: false);
    _protections.Clear();
    return HookResult.Continue;
  }

  private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    var userId = @event.Userid?.UserId;
    if (userId != null)
      _protections.Remove(userId.Value);
    return HookResult.Continue;
  }

  private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid || player.UserId == null || !IsAlive(player))
      return HookResult.Continue;

    if (player.Team != CsTeam.Terrorist && player.Team != CsTeam.CounterTerrorist)
      return HookResult.Continue;

    var (seconds, color) = ResolveProtection(player);
    if (seconds <= 0f)
      return HookResult.Continue;

    float now = Server.CurrentTime;
    _protections[player.UserId.Value] = new ProtectionState
    {
      Start = now,
      End = now + seconds,
      BaseColor = color
    };

    ApplyColor(player, color);

    if (!player.IsBot)
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["spawnkillprotection.protection_active", (int)MathF.Ceiling(seconds)]}");

    return HookResult.Continue;
  }

  private (float Seconds, Color Color) ResolveProtection(CCSPlayerController player)
  {
    if (!player.IsBot)
    {
      foreach (var flagProt in Config.FlagProtections)
      {
        if (string.IsNullOrWhiteSpace(flagProt.Flag))
          continue;

        if (AdminManager.PlayerHasPermissions(player, flagProt.Flag))
          return (flagProt.Seconds, ToColor(flagProt.Color));
      }
    }

    var team = player.Team == CsTeam.Terrorist ? Config.TeamT : Config.TeamCT;
    if (!team.Enabled)
      return (0f, Color.White);

    return (team.Seconds, ToColor(team.Color));
  }

  private static Color ToColor(int[] rgb)
  {
    if (rgb.Length < 3)
      return Color.White;
    return Color.FromArgb(255,
      Math.Clamp(rgb[0], 0, 255),
      Math.Clamp(rgb[1], 0, 255),
      Math.Clamp(rgb[2], 0, 255));
  }

  private void OnTick()
  {
    if (_protections.Count == 0)
      return;

    float now = Server.CurrentTime;
    List<int>? expired = null;

    foreach (var (userId, state) in _protections)
    {
      var player = Utilities.GetPlayerFromUserid(userId);
      if (player == null || !player.IsValid || !IsAlive(player))
      {
        (expired ??= new List<int>()).Add(userId);
        continue;
      }

      if (now >= state.End)
      {
        (expired ??= new List<int>()).Add(userId);
        continue;
      }

      float duration = state.End - state.Start;
      float t = duration <= 0f ? 1f : Math.Clamp((now - state.Start) / duration, 0f, 1f);
      ApplyColor(player, Lerp(state.BaseColor, t));
    }

    if (expired == null)
      return;

    foreach (var userId in expired)
      EndProtection(userId, resetColor: true, announce: true);
  }

  private void EndProtection(int userId, bool resetColor, bool announce)
  {
    if (!_protections.Remove(userId))
      return;

    var player = Utilities.GetPlayerFromUserid(userId);
    if (player == null || !player.IsValid)
      return;

    if (resetColor && IsAlive(player))
      ApplyColor(player, Color.FromArgb(255, 255, 255, 255));

    if (announce && !player.IsBot && IsAlive(player))
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["spawnkillprotection.protection_ended"]}");
  }

  private static Color Lerp(Color baseColor, float t)
  {
    byte r = (byte)(baseColor.R + (255 - baseColor.R) * t);
    byte g = (byte)(baseColor.G + (255 - baseColor.G) * t);
    byte b = (byte)(baseColor.B + (255 - baseColor.B) * t);
    return Color.FromArgb(255, r, g, b);
  }

  private static void ApplyColor(CCSPlayerController player, Color color)
  {
    var pawn = player.PlayerPawn.Value;
    if (pawn == null || !pawn.IsValid)
      return;

    if (pawn.Render == color)
      return;

    pawn.Render = color;
    Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
  }

  private HookResult HandleEntityDamage(CEntityInstance victimEnt, CTakeDamageInfo info)
  {
    if (_protections.Count == 0 || victimEnt == null)
      return HookResult.Continue;

    try
    {
      var victimPawn = victimEnt as CCSPlayerPawn ?? new CCSPlayerPawn(victimEnt.Handle);
      var victimController = victimPawn?.OriginalController.Value;
      if (victimController == null || !victimController.IsValid || victimController.UserId == null)
        return HookResult.Continue;

      if (_protections.TryGetValue(victimController.UserId.Value, out var state) && Server.CurrentTime < state.End)
        info.Damage = 0f;

      return HookResult.Continue;
    }
    catch
    {
      return HookResult.Continue;
    }
  }

  private static bool IsAlive(CCSPlayerController? player)
  {
    return player?.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
  }
}
