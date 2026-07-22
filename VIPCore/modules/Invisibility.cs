using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class Invisibility : VipModule
{
    private class Cfg
    {
        public bool OnlyStopped { get; set; } = true;
        public float DmgAfterInvis { get; set; } = 0f;
        public string OnlyWithWeapon { get; set; } = "";

        private List<string>? _allow;
        public List<string> Allow => _allow ??= WeaponUtil.ParseCsv(OnlyWithWeapon);
    }

    private static readonly Cfg DefaultCfg = new();

    private const float VisibleAlpha = 0.5f;

    private readonly bool[] _invisible = new bool[64];
    private readonly float[] _revealedUntil = new float[64];
    private int _invisCount;
    private readonly List<(uint PawnIndex, nint PawnHandle, CsTeam Team)> _invisPawns = new();

    public override string Name => "Invisibility";
    public override string DisplayName => Core.Localizer["vip.module.invisibility"];

    public override void OnLoad()
    {
        Core.RegisterListener<OnTick>(OnTick);
        Core.RegisterListener<CheckTransmit>(OnCheckTransmit);
        Core.RegisterEventHandler<EventPlayerHurt>((ev, _) => { Reveal(ev.Userid); return HookResult.Continue; });
        Core.RegisterEventHandler<EventWeaponFire>((ev, _) => { Reveal(ev.Userid); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) =>
        {
            int slot = ev.Userid?.Slot ?? -1;
            if (slot >= 0 && slot < 64 && _invisible[slot])
            {
                _invisible[slot] = false;
                _invisCount--;
                SetVisible(slot);
            }
            return HookResult.Continue;
        });
        Core.RegisterEventHandler<EventRoundStart>((_, __) =>
        {
            Array.Clear(_invisible);
            Array.Clear(_revealedUntil);
            _invisCount = 0;
            return HookResult.Continue;
        });
    }

    public override void OnUnload()
    {
        for (int slot = 0; slot < 64; slot++)
            if (_invisible[slot])
                SetVisible(slot);
    }

    private void Reveal(CCSPlayerController? player)
    {
        int slot = player?.Slot ?? -1;
        if (slot < 0 || slot >= 64 || !Active(player))
            return;

        float dmgAfter = (GroupValue<Cfg>(player!) ?? DefaultCfg).DmgAfterInvis;
        if (dmgAfter > 0)
            _revealedUntil[slot] = Server.CurrentTime + dmgAfter;
    }

    private void OnTick()
    {
        foreach (var player in Core.Players)
        {
            if (player == null || !player.IsValid || player.IsBot)
                continue;

            int slot = player.Slot;
            bool wants = ShouldBeInvisible(player, slot);

            if (wants && !_invisible[slot])
            {
                _invisible[slot] = true;
                _invisCount++;
                SetInvisible(slot);
            }
            else if (!wants && _invisible[slot])
            {
                _invisible[slot] = false;
                _invisCount--;
                SetVisible(slot);
            }
        }
    }

    private bool ShouldBeInvisible(CCSPlayerController? player, int slot)
    {
        if (!Active(player) || !IsAlive(player))
            return false;

        var cfg = GroupValue<Cfg>(player!) ?? DefaultCfg;

        if (cfg.DmgAfterInvis > 0 && Server.CurrentTime < _revealedUntil[slot])
            return false;

        var allow = cfg.Allow;
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(player!)))
            return false;

        if (cfg.OnlyStopped)
        {
            var vel = player!.PlayerPawn.Value?.AbsVelocity;
            if (vel == null)
                return false;

            double speed = Math.Sqrt(vel.X * vel.X + vel.Y * vel.Y);
            if (speed > 5.0)
                return false;
        }

        return true;
    }

    private static void SetInvisible(int slot)
    {
        var pawn = Utilities.GetPlayerFromSlot(slot)?.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        pawn.Render = Color.FromArgb((int)(255 * (1 - VisibleAlpha)), 255, 255, 255);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }

    private static void SetVisible(int slot)
    {
        var pawn = Utilities.GetPlayerFromSlot(slot)?.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        int alpha = PlayerModel.LegsHidden(slot) ? 254 : 255;
        pawn.Render = Color.FromArgb(alpha, 255, 255, 255);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }

    private void OnCheckTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList infoList)
    {
        if (_invisCount == 0)
            return;

        _invisPawns.Clear();
        for (int slot = 0; slot < 64; slot++)
        {
            if (!_invisible[slot])
                continue;

            var invisPlayer = Utilities.GetPlayerFromSlot(slot);
            if (invisPlayer == null || !invisPlayer.IsValid)
            {
                _invisible[slot] = false;
                _invisCount--;
                continue;
            }

            var invisPawn = invisPlayer.PlayerPawn.Value;
            if (invisPawn == null || !invisPawn.IsValid)
                continue;

            _invisPawns.Add((invisPawn.Index, invisPawn.Handle, invisPlayer.Team));
        }

        if (_invisPawns.Count == 0)
            return;

        foreach (var (info, player) in infoList)
        {
            if (player == null || !player.IsValid || player.Team == CsTeam.Spectator)
                continue;

            var targetHandle = player.Pawn.Value?.ObserverServices?.ObserverTarget.Value?.Handle ?? nint.Zero;

            foreach (var (pawnIndex, pawnHandle, team) in _invisPawns)
            {
                if (team == player.Team)
                    continue;

                if (targetHandle != nint.Zero && pawnHandle == targetHandle)
                    continue;

                if (info.TransmitEntities.Contains(pawnIndex))
                    info.TransmitEntities.Remove(pawnIndex);
            }
        }
    }
}
