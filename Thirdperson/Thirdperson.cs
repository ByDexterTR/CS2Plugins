using System.Drawing;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace Thirdperson;

public class ThirdpersonConfig : BasePluginConfig
{
  [JsonPropertyName("thirdperson_cmd")]
  public string Commands { get; set; } = "css_tp,css_thirdperson";

  [JsonPropertyName("thirdperson_flag")]
  public string Flag { get; set; } = "@css/thirdperson";

  [JsonPropertyName("thirdperson_distance")]
  public float Distance { get; set; } = 110f;

  [JsonPropertyName("thirdperson_blockwall")]
  public bool BlockWall { get; set; } = true;
}

public class Thirdperson : BasePlugin, IPluginConfig<ThirdpersonConfig>
{
  public override string ModuleName => "Thirdperson";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public ThirdpersonConfig Config { get; set; } = new();

  private const int MaxSlots = 64;
  private const float WallPadding = 16f;
  private const string CameraModel = "models/sprays/spray_plane.vmdl";

  private readonly CDynamicProp?[] _cameras = new CDynamicProp?[MaxSlots];
  private readonly uint[] _originalView = new uint[MaxSlots];

  public void OnConfigParsed(ThirdpersonConfig config)
  {
    if (config.Distance < 20f)
      config.Distance = 20f;
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    foreach (var name in Config.Commands.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
      AddCommand(name, "Thirdperson toggle", OnToggleCommand);

    RegisterListener<OnServerPrecacheResources>(manifest => manifest.AddResource(CameraModel));
    RegisterListener<OnTick>(OnTick);

    RegisterEventHandler<EventPlayerDeath>((ev, _) =>
    {
      int slot = ev.Userid?.Slot ?? -1;
      if (slot >= 0 && slot < MaxSlots)
        Disable(slot);
      return HookResult.Continue;
    });

    RegisterEventHandler<EventPlayerDisconnect>((ev, _) =>
    {
      int slot = ev.Userid?.Slot ?? -1;
      if (slot >= 0 && slot < MaxSlots)
        Disable(slot);
      return HookResult.Continue;
    });

    RegisterEventHandler<EventRoundStart>((_, _) =>
    {
      DisableAll();
      return HookResult.Continue;
    });

    RegisterEventHandler<EventRoundEnd>((_, _) =>
    {
      DisableAll();
      return HookResult.Continue;
    });
  }

  public override void Unload(bool hotReload)
  {
    DisableAll();
  }

  private void DisableAll()
  {
    for (int slot = 0; slot < MaxSlots; slot++)
      if (_cameras[slot] != null)
        Disable(slot);
  }

  private void OnToggleCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return;

    if (!string.IsNullOrWhiteSpace(Config.Flag) && !AdminManager.PlayerHasPermissions(player, Config.Flag))
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["thirdperson.no_permission"]}");
      return;
    }

    if (_cameras[player.Slot] != null)
    {
      Disable(player.Slot);
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["thirdperson.disabled"]}");
      return;
    }

    if (!IsAlive(player))
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["thirdperson.alive_only"]}");
      return;
    }

    Enable(player);
    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["thirdperson.enabled"]}");
  }

  private void Enable(CCSPlayerController player)
  {
    var pawn = player.PlayerPawn.Value;
    if (pawn == null || !pawn.IsValid || pawn.CameraServices == null)
      return;

    var camera = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
    if (camera == null || !camera.IsValid)
      return;

    int slot = player.Slot;
    _cameras[slot] = camera;
    _originalView[slot] = pawn.CameraServices.ViewEntity.Raw;

    Server.NextFrame(() =>
    {
      if (camera == null || !camera.IsValid || !pawn.IsValid || pawn.CameraServices == null)
        return;

      try
      {
        camera.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags =
            (uint)(camera.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
      }
      catch { }

      camera.SetModel(CameraModel);
      camera.Render = Color.FromArgb(0, 255, 255, 255);
      camera.Teleport(pawn.AbsOrigin, pawn.EyeAngles, new Vector());
      camera.DispatchSpawn();

      pawn.CameraServices.ViewEntity.Raw = camera.EntityHandle.Raw;
      Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");
    });
  }

  private void Disable(int slot)
  {
    var camera = _cameras[slot];
    _cameras[slot] = null;

    var player = Utilities.GetPlayerFromSlot(slot);
    var pawn = player?.PlayerPawn.Value;
    if (pawn != null && pawn.IsValid && pawn.CameraServices != null)
    {
      pawn.CameraServices.ViewEntity.Raw = _originalView[slot];
      Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");
    }

    if (camera != null && camera.IsValid)
      camera.Remove();
  }

  private void OnTick()
  {
    for (int slot = 0; slot < MaxSlots; slot++)
    {
      var camera = _cameras[slot];
      if (camera == null || !camera.IsValid)
        continue;

      var player = Utilities.GetPlayerFromSlot(slot);
      var pawn = player?.PlayerPawn.Value;
      if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
      {
        Disable(slot);
        continue;
      }

      var angles = pawn.EyeAngles;
      float ry = angles.Y * MathF.PI / 180f;
      float rp = angles.X * MathF.PI / 180f;

      float fx = MathF.Cos(rp) * MathF.Cos(ry);
      float fy = MathF.Cos(rp) * MathF.Sin(ry);
      float fz = -MathF.Sin(rp);

      var origin = pawn.AbsOrigin;
      var eye = new System.Numerics.Vector3(origin.X, origin.Y, origin.Z + pawn.ViewOffset.Z);
      var back = new System.Numerics.Vector3(-fx, -fy, -fz);
      var target = eye + back * Config.Distance;

      if (Config.BlockWall)
      {
        var traceEnd = eye + back * (Config.Distance + WallPadding);
        var hit = NativeTrace.TraceLine(pawn, eye, traceEnd);
        if (hit != null)
        {
          float hitDistance = System.Numerics.Vector3.Distance(eye, hit.Value);
          target = hitDistance > WallPadding
              ? eye + back * Math.Min(hitDistance - WallPadding, Config.Distance)
              : eye;
        }
      }

      camera.Teleport(new Vector(target.X, target.Y, target.Z), pawn.V_angle);
    }
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
