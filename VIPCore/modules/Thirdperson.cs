using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class Thirdperson : VipModule
{
    private class Cfg
    {
        public float Distance { get; set; } = 110f;
    }

    public override string Name => "Thirdperson";
    public override string DisplayName => Core.Localizer["vip.module.thirdperson"];
    public override bool ShowInMenu => false;

    private readonly CDynamicProp?[] _cameras = new CDynamicProp?[64];
    private readonly uint[] _original = new uint[64];

    public override void OnLoad()
    {
        Core.RegisterAliasedCommand(Core.TpCommands, OnToggle);
        Core.RegisterListener<OnTick>(OnTick);
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) =>
        {
            int slot = ev.Userid?.Slot ?? -1;
            if (slot >= 0 && slot < 64)
                Disable(slot);
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
        for (int slot = 0; slot < 64; slot++)
            if (_cameras[slot] != null)
                Disable(slot);
    }

    private void OnToggle(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
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
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.CameraServices == null)
            return;

        var camera = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (camera == null || !camera.IsValid)
            return;

        int slot = player.Slot;
        _cameras[slot] = camera;
        _original[slot] = pawn.CameraServices.ViewEntity.Raw;

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

            camera.SetModel("models/sprays/spray_plane.vmdl");
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
            pawn.CameraServices.ViewEntity.Raw = _original[slot];
            Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");
        }

        if (camera != null && camera.IsValid)
            camera.Remove();
    }

    private void OnTick()
    {
        for (int slot = 0; slot < 64; slot++)
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

            if (!Granted(player))
            {
                Disable(slot);
                continue;
            }

            float distance = (GroupValue<Cfg>(player!) ?? new Cfg()).Distance;
            var angles = pawn.EyeAngles;
            float ry = angles.Y * MathF.PI / 180f;
            float rp = angles.X * MathF.PI / 180f;

            float fx = MathF.Cos(rp) * MathF.Cos(ry);
            float fy = MathF.Cos(rp) * MathF.Sin(ry);
            float fz = -MathF.Sin(rp);

            var origin = pawn.AbsOrigin;
            var pos = new Vector(
                origin.X - fx * distance,
                origin.Y - fy * distance,
                origin.Z + pawn.ViewOffset.Z - fz * distance);

            camera.Teleport(pos, pawn.V_angle);
        }
    }
}
