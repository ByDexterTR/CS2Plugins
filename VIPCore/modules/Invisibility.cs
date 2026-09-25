using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

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

    private readonly float[] _revealedUntil = new float[64];

    public override string Name => "Invisibility";
    public override string DisplayName => Core.Localizer["vip.module.invisibility"];

    public override void OnLoad()
    {
        InvisPool.Ensure(Core);
        Core.HookTick(OnTick, 2);
        Core.RegisterEventHandler<EventPlayerHurt>((ev, _) => { Reveal(ev.Userid); return HookResult.Continue; });
        Core.RegisterEventHandler<EventWeaponFire>((ev, _) => { Reveal(ev.Userid); return HookResult.Continue; });
        Core.RegisterEventHandler<EventRoundStart>((_, __) => { Array.Clear(_revealedUntil); return HookResult.Continue; });
    }

    public override void OnUnload()
    {
        for (int slot = 0; slot < 64; slot++)
            InvisPool.Set(slot, InvisPool.Module, false);
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
            if (player == null || !player.IsValid || player.IsBot || player.Slot >= 64)
                continue;

            InvisPool.Set(player.Slot, InvisPool.Module, ShouldBeInvisible(player, player.Slot));
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
}
