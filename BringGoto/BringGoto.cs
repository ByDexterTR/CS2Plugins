using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using ByDexter.Shared;

namespace BringGoto;

public class BringGotoConfig : BasePluginConfig
{
  [JsonPropertyName("bring_cmd")]
  public string BringCommands { get; set; } = "css_bring,css_gel";

  [JsonPropertyName("goto_cmd")]
  public string GotoCommands { get; set; } = "css_goto,css_git";

  [JsonPropertyName("bring_flag")]
  public string BringFlag { get; set; } = "@css/cheats";

  [JsonPropertyName("goto_flag")]
  public string GotoFlag { get; set; } = "@css/generic";

  [JsonPropertyName("ignore_immunity")]
  public bool IgnoreImmunity { get; set; } = false;
}

public class BringGoto : BasePlugin, IPluginConfig<BringGotoConfig>
{
  public override string ModuleName => "BringGoto";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public BringGotoConfig Config { get; set; } = new();

  public void OnConfigParsed(BringGotoConfig config)
  {
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    foreach (var name in Split(Config.BringCommands))
      AddCommand(name, "Oyuncuyu nisangaha isinlar", OnBringCommand);

    foreach (var name in Split(Config.GotoCommands))
      AddCommand(name, "Oyuncuya isinlanir", OnGotoCommand);
  }

  private static string[] Split(string names) =>
    names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

  private void OnBringCommand(CCSPlayerController? player, CommandInfo info)
  {
    var pawn = GetCallerPawn(player, Config.BringFlag);
    if (player == null || pawn == null)
      return;

    if (info.ArgCount < 2)
    {
      Reply(player, Localizer["bring.usage"]);
      return;
    }

    var targets = info.GetArgTargetResult(1).Players
      .Where(p => p != null && p.IsValid && !p.IsHLTV && p.Slot != player.Slot && IsAlive(p))
      .ToList();

    if (targets.Count == 0)
    {
      Reply(player, Localizer["not_found"]);
      return;
    }

    var allowed = targets.Where(t => CanTarget(player, t)).ToList();
    if (allowed.Count == 0)
    {
      Reply(player, Localizer["immunity", targets[0].PlayerName]);
      return;
    }

    var pos = GetAimPoint(pawn);
    foreach (var target in allowed)
    {
      target.PlayerPawn.Value!.Teleport(new Vector(pos.X, pos.Y, pos.Z), null, new Vector(0, 0, 0));
      if (!target.IsBot)
        Reply(target, Localizer["bring.notify", player.PlayerName]);
    }

    Reply(player, allowed.Count == 1
      ? Localizer["bring.done", allowed[0].PlayerName]
      : Localizer["bring.done_multi", allowed.Count]);
  }

  private void OnGotoCommand(CCSPlayerController? player, CommandInfo info)
  {
    var pawn = GetCallerPawn(player, Config.GotoFlag);
    if (player == null || pawn == null)
      return;

    if (info.ArgCount < 2)
    {
      Reply(player, Localizer["goto.usage"]);
      return;
    }

    var targets = info.GetArgTargetResult(1).Players
      .Where(p => p != null && p.IsValid && !p.IsHLTV && IsAlive(p))
      .ToList();

    if (targets.Count == 0)
    {
      Reply(player, Localizer["not_found"]);
      return;
    }

    if (targets.Count > 1)
    {
      Reply(player, Localizer["goto.multiple"]);
      return;
    }

    var target = targets[0];
    if (target.Slot == player.Slot)
    {
      Reply(player, Localizer["goto.self"]);
      return;
    }

    if (!CanTarget(player, target))
    {
      Reply(player, Localizer["immunity", target.PlayerName]);
      return;
    }

    var origin = target.PlayerPawn.Value!.AbsOrigin;
    if (origin == null)
      return;

    pawn.Teleport(new Vector(origin.X, origin.Y, origin.Z + 80f), null, new Vector(0, 0, 0));
    Reply(player, Localizer["goto.done", target.PlayerName]);
    if (!target.IsBot)
      Reply(target, Localizer["goto.notify", player.PlayerName]);
  }

  private CCSPlayerPawn? GetCallerPawn(CCSPlayerController? player, string flag)
  {
    if (player == null || !player.IsValid)
      return null;

    if (!string.IsNullOrWhiteSpace(flag) && !AdminManager.PlayerHasPermissions(player, flag))
    {
      Reply(player, Localizer["no_permission"]);
      return null;
    }

    var pawn = player.PlayerPawn.Value;
    if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE || pawn.AbsOrigin == null)
    {
      Reply(player, Localizer["must_alive"]);
      return null;
    }

    return pawn;
  }

  private bool CanTarget(CCSPlayerController caller, CCSPlayerController target) =>
    Config.IgnoreImmunity || target.IsBot ||
    AdminManager.GetPlayerImmunity(caller) >= AdminManager.GetPlayerImmunity(target);

  private static bool IsAlive(CCSPlayerController player) =>
    player.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE
    && player.PlayerPawn.Value?.AbsOrigin != null;

  private static System.Numerics.Vector3 GetAimPoint(CCSPlayerPawn pawn)
  {
    var angles = pawn.EyeAngles;
    float ry = angles.Y * MathF.PI / 180f;
    float rp = angles.X * MathF.PI / 180f;

    var forward = new System.Numerics.Vector3(
      MathF.Cos(rp) * MathF.Cos(ry),
      MathF.Cos(rp) * MathF.Sin(ry),
      -MathF.Sin(rp));

    var origin = pawn.AbsOrigin!;
    var eye = new System.Numerics.Vector3(origin.X, origin.Y, origin.Z + pawn.ViewOffset.Z);
    var end = eye + forward * 8192f;

    var hit = NativeTrace.TraceLine(pawn, eye, end) ?? (eye + forward * 128f);
    var pos = hit - forward * 24f;
    pos.Z += 6f;
    return pos;
  }

  private void Reply(CCSPlayerController player, string text) =>
    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {text}");
}
