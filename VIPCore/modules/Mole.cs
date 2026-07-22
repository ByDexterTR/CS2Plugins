using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class Mole : VipModule
{
    private class Cfg
    {
        public float Time { get; set; } = 2.5f;
        public float Unit { get; set; } = 30f;
        public string OnlyWithWeapon { get; set; } = "";
        public bool IgnoreTeammates { get; set; } = true;
        public bool IgnoreEnemy { get; set; } = false;
        public bool IgnoreSelf { get; set; } = true;

        private List<string>? _allow;
        public List<string> Allow => _allow ??= WeaponUtil.ParseCsv(OnlyWithWeapon);
    }

    private static readonly Cfg DefaultCfg = new();

    private readonly bool[] _buried = new bool[64];
    private int _round;

    public override string Name => "Mole";
    public override string DisplayName => Core.Localizer["vip.module.mole"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerHurt>(OnHurt);
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) => { ClearSlot(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) => { ClearSlot(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        Core.RegisterEventHandler<EventRoundStart>((_, __) =>
        {
            _round++;
            Array.Clear(_buried);
            return HookResult.Continue;
        });
    }

    private void ClearSlot(int slot)
    {
        if (slot >= 0 && slot < 64)
            _buried[slot] = false;
    }

    private HookResult OnHurt(EventPlayerHurt ev, GameEventInfo info)
    {
        var attacker = ev.Attacker;
        if (!Active(attacker))
            return HookResult.Continue;

        var victim = ev.Userid;
        int slot = victim?.Slot ?? -1;
        if (slot < 0 || slot >= 64 || _buried[slot] || !IsAlive(victim))
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(attacker!) ?? DefaultCfg;

        bool isSelf = victim!.Slot == attacker!.Slot;
        bool isTeammate = !isSelf && victim.Team == attacker.Team;
        bool isEnemy = !isSelf && !isTeammate;

        if (isSelf && cfg.IgnoreSelf)
            return HookResult.Continue;
        if (isTeammate && cfg.IgnoreTeammates)
            return HookResult.Continue;
        if (isEnemy && cfg.IgnoreEnemy)
            return HookResult.Continue;

        var allow = cfg.Allow;
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, "weapon_" + ev.Weapon))
            return HookResult.Continue;

        var pawn = victim.PlayerPawn.Value;
        var origin = pawn?.AbsOrigin;
        if (pawn == null || !pawn.IsValid || origin == null)
            return HookResult.Continue;

        _buried[slot] = true;
        var restore = new Vector(origin.X, origin.Y, origin.Z);

        pawn.Teleport(new Vector(origin.X, origin.Y, origin.Z - cfg.Unit), null, new Vector(0, 0, 0));
        Freeze(pawn, true);

        int round = _round;
        int userId = victim.UserId ?? -1;

        Core.AddTimer(Math.Max(cfg.Time, 0.1f), () =>
        {
            if (round != _round || userId < 0)
                return;

            var p = Utilities.GetPlayerFromUserid(userId);
            if (p == null || !p.IsValid || p.Slot >= 64)
                return;

            _buried[p.Slot] = false;

            var pw = p.PlayerPawn.Value;
            if (pw == null || !pw.IsValid || !IsAlive(p))
                return;

            pw.Teleport(restore, null, new Vector(0, 0, 0));
            Freeze(pw, false);
        }, TimerFlags.STOP_ON_MAPCHANGE);

        return HookResult.Continue;
    }

    private static void Freeze(CCSPlayerPawn pawn, bool freeze)
    {
        pawn.MoveType = freeze ? MoveType_t.MOVETYPE_NONE : MoveType_t.MOVETYPE_WALK;
        Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", freeze ? 0 : 2);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
    }
}
