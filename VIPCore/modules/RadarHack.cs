using CounterStrikeSharp.API;

namespace VIPCore;

public class RadarHack : VipModule
{
    private class Cfg
    {
        public float DurationOn { get; set; } = 1f;
        public float DurationOff { get; set; } = 0f;
        public bool SeeTeammates { get; set; }
        public int OnlyMode { get; set; } = 0;
    }

    private struct Viewers
    {
        public byte Team;
        public bool Mates;
        public int Mode;
        public ulong Bits;
    }

    private static readonly Cfg DefaultCfg = new();
    private readonly Viewers[] _viewers = new Viewers[64];

    public override string Name => "RadarHack";
    public override string DisplayName => Core.Localizer["vip.module.radarhack"];

    public override void OnLoad()
    {
        ActivityFilter.Ensure(Core);
        Core.HookTick(OnTick);
    }

    private unsafe void OnTick()
    {
        var vips = ActivePlayers();
        if (vips.Count == 0)
            return;

        float now = Server.CurrentTime;
        int groups = 0;

        foreach (var player in vips)
        {
            int slot = player.Slot;
            nint pawn = PlayerCache.Pointer(player);
            if ((uint)slot >= 64 || pawn == 0)
                continue;

            var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;
            if (cfg.DurationOff > 0)
            {
                float on = Math.Max(cfg.DurationOn, 1f);
                if (now % (on + cfg.DurationOff) >= on)
                    continue;
            }

            byte team = PlayerCache.Team(pawn);
            int index = 0;
            while (index < groups && (_viewers[index].Team != team || _viewers[index].Mates != cfg.SeeTeammates || _viewers[index].Mode != cfg.OnlyMode))
                index++;

            if (index == groups)
                _viewers[groups++] = new Viewers { Team = team, Mates = cfg.SeeTeammates, Mode = cfg.OnlyMode };

            _viewers[index].Bits |= 1UL << slot;
        }

        if (groups == 0)
            return;

        foreach (ref readonly var target in PlayerCache.AlivePlayers(Core.Players))
        {
            if (PlayerCache.RenderAlpha(target.Pawn) < 200)
                continue;

            ulong mask = 0;
            for (int i = 0; i < groups; i++)
            {
                ref var viewers = ref _viewers[i];
                if ((viewers.Mates || viewers.Team != target.Team) && ActivityFilter.Matches(viewers.Mode, target.Slot, target.Pawn))
                    mask |= viewers.Bits;
            }

            mask &= ~(1UL << target.Slot);
            if (mask == 0)
                continue;

            uint* spotted = PlayerCache.SpottedByMask(target.Pawn);
            spotted[0] |= (uint)mask;
            spotted[1] |= (uint)(mask >> 32);
        }
    }
}
