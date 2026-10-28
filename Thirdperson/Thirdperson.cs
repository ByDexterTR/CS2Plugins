using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using ByDexter.Shared;

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
  public override string ModuleVersion => "1.0.2";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public ThirdpersonConfig Config { get; set; } = new();

  private const int MaxSlots = 64;

  private readonly CCSCustomPlayerCamera?[] _cameras = new CCSCustomPlayerCamera?[MaxSlots];
  private readonly uint[] _cameraHandles = new uint[MaxSlots];
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

    RegisterEventHandler<EventPlayerDeath>((ev, _) =>
    {
      Disable(ev.Userid?.Slot ?? -1);
      return HookResult.Continue;
    });

    RegisterEventHandler<EventPlayerDisconnect>((ev, _) =>
    {
      Disable(ev.Userid?.Slot ?? -1);
      return HookResult.Continue;
    });

    RegisterEventHandler<EventPlayerSpawn>((ev, _) =>
    {
      Disable(ev.Userid?.Slot ?? -1);
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

    var pawn = player.PlayerPawn.Value;
    if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["thirdperson.alive_only"]}");
      return;
    }

    if (Enable(player.Slot, pawn))
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["thirdperson.enabled"]}");
  }

  private bool Enable(int slot, CCSPlayerPawn pawn)
  {
    var services = pawn.CameraServices;
    if (services == null || (uint)slot >= MaxSlots)
      return false;

    var camera = Utilities.CreateEntityByName<CCSCustomPlayerCamera>("custom_player_camera");
    if (camera == null || !camera.IsValid)
      return false;

    uint pawnHandle = pawn.EntityHandle.Raw;
    camera.Pawn.Raw = pawnHandle;
    camera.DispatchSpawn();

    camera.Pawn.Raw = pawnHandle;
    camera.CameraMode = CustomCameraMode_t.CUSTOM_CAMERA_MODE_FOLLOW_POSITION;
    camera.FollowEntity.Raw = pawnHandle;
    camera.CameraOffset.X = -Config.Distance;
    camera.CameraOffset.Y = 0f;
    camera.CameraOffset.Z = 0f;
    camera.FollowEyes = true;
    camera.ClipCameraOffset = Config.BlockWall;
    NetState.Changed(camera, "CCSCustomPlayerCamera", "m_hPawn");
    NetState.Changed(camera, "CCSCustomPlayerCamera", "m_nCameraMode");
    NetState.Changed(camera, "CCSCustomPlayerCamera", "m_hFollowEntity");
    NetState.Changed(camera, "CCSCustomPlayerCamera", "m_vecCameraOffset");
    NetState.Changed(camera, "CCSCustomPlayerCamera", "m_bFollowEyes");
    NetState.Changed(camera, "CCSCustomPlayerCamera", "m_bClipCameraOffset");

    _cameras[slot] = camera;
    _cameraHandles[slot] = camera.EntityHandle.Raw;
    _originalView[slot] = services.ViewEntity.Raw;
    services.ViewEntity.Raw = camera.EntityHandle.Raw;
    NetState.Changed(pawn, "CBasePlayerPawn", "m_pCameraServices");
    return true;
  }

  private void Disable(int slot)
  {
    if ((uint)slot >= MaxSlots)
      return;

    var camera = _cameras[slot];
    if (camera == null)
      return;

    _cameras[slot] = null;

    var pawn = Utilities.GetPlayerFromSlot(slot)?.PlayerPawn.Value;
    var services = pawn != null && pawn.IsValid ? pawn.CameraServices : null;
    if (services != null && services.ViewEntity.Raw == _cameraHandles[slot])
    {
      services.ViewEntity.Raw = _originalView[slot];
      NetState.Changed(pawn!, "CBasePlayerPawn", "m_pCameraServices");
    }

    if (camera.IsValid)
      camera.Remove();
  }
}
