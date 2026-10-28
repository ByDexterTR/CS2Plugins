using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace VIPCore;

public class Thirdperson : VipModule
{
    private class Cfg
    {
        public float Distance { get; set; } = 110f;
    }

    private static readonly Cfg DefaultCfg = new();
    private const int CheckInterval = 32;

    public override string Name => "Thirdperson";
    public override string DisplayName => Core.Localizer["vip.module.thirdperson"];
    public override bool ShowInMenu => false;

    private readonly CCSCustomPlayerCamera?[] _cameras = new CCSCustomPlayerCamera?[64];
    private readonly uint[] _cameraHandles = new uint[64];
    private readonly uint[] _original = new uint[64];
    private int _active;

    public override void OnLoad()
    {
        Core.RegisterAliasedCommand(Core.TpCommands, OnToggle);
        Core.HookTick(OnTick);
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) =>
        {
            Disable(ev.Userid?.Slot ?? -1);
            return HookResult.Continue;
        });
        Core.RegisterEventHandler<EventPlayerSpawn>((ev, _) =>
        {
            Disable(ev.Userid?.Slot ?? -1);
            return HookResult.Continue;
        });
        Core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) =>
        {
            Disable(ev.Userid?.Slot ?? -1);
            return HookResult.Continue;
        });
        Core.RegisterEventHandler<EventRoundStart>((_, _) =>
        {
            DisableAll();
            return HookResult.Continue;
        });
        Core.RegisterEventHandler<EventRoundEnd>((_, _) =>
        {
            DisableAll();
            return HookResult.Continue;
        });
    }

    public override void OnUnload()
    {
        DisableAll();
    }

    private void DisableAll()
    {
        if (_active == 0)
            return;

        for (int slot = 0; slot < 64; slot++)
            Disable(slot);
    }

    private void OnToggle(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || (uint)player.Slot >= 64)
            return;

        if (!Granted(player))
        {
            player.PrintToChat($" {CC.Orchid}{Core.Localizer["chat_prefix"]}{CC.Default} {Core.Localizer["vip.no_access"]}");
            return;
        }

        if (_cameras[player.Slot] != null)
            Disable(player.Slot);
        else if (IsAlive(player))
            Enable(player);
    }

    private void Enable(CCSPlayerController player)
    {
        var pawn = PlayerCache.Pawn(player);
        var services = pawn?.CameraServices;
        if (pawn == null || services == null)
            return;

        var camera = Utilities.CreateEntityByName<CCSCustomPlayerCamera>("custom_player_camera");
        if (camera == null || !camera.IsValid)
            return;

        uint pawnHandle = pawn.EntityHandle.Raw;
        camera.Pawn.Raw = pawnHandle;
        camera.DispatchSpawn();

        camera.Pawn.Raw = pawnHandle;
        camera.CameraMode = CustomCameraMode_t.CUSTOM_CAMERA_MODE_FOLLOW_POSITION;
        camera.FollowEntity.Raw = pawnHandle;
        camera.CameraOffset.X = -(GroupValue<Cfg>(player) ?? DefaultCfg).Distance;
        camera.CameraOffset.Y = 0f;
        camera.CameraOffset.Z = 0f;
        camera.FollowEyes = true;
        camera.ClipCameraOffset = true;
        NetState.Changed(camera, "CCSCustomPlayerCamera", "m_hPawn");
        NetState.Changed(camera, "CCSCustomPlayerCamera", "m_nCameraMode");
        NetState.Changed(camera, "CCSCustomPlayerCamera", "m_hFollowEntity");
        NetState.Changed(camera, "CCSCustomPlayerCamera", "m_vecCameraOffset");
        NetState.Changed(camera, "CCSCustomPlayerCamera", "m_bFollowEyes");
        NetState.Changed(camera, "CCSCustomPlayerCamera", "m_bClipCameraOffset");

        int slot = player.Slot;
        _cameras[slot] = camera;
        _cameraHandles[slot] = camera.EntityHandle.Raw;
        _original[slot] = services.ViewEntity.Raw;
        _active++;

        services.ViewEntity.Raw = _cameraHandles[slot];
        NetState.Changed(pawn, "CBasePlayerPawn", "m_pCameraServices");
    }

    private void Disable(int slot)
    {
        if ((uint)slot >= 64)
            return;

        var camera = _cameras[slot];
        if (camera == null)
            return;

        _cameras[slot] = null;
        _active--;

        var pawn = PlayerCache.Pawn(Core.PlayerAt(slot));
        var services = pawn?.CameraServices;
        if (pawn != null && services != null && services.ViewEntity.Raw == _cameraHandles[slot])
        {
            services.ViewEntity.Raw = _original[slot];
            NetState.Changed(pawn, "CBasePlayerPawn", "m_pCameraServices");
        }

        if (camera.IsValid)
            camera.Remove();
    }

    private void OnTick()
    {
        if (_active == 0 || Server.TickCount % CheckInterval != 0)
            return;

        for (int slot = 0; slot < 64; slot++)
        {
            if (_cameras[slot] == null)
                continue;

            var player = Core.PlayerAt(slot);
            if (!IsAlive(player) || !Granted(player))
                Disable(slot);
        }
    }
}
