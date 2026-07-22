using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class Pyro : VipModule
{
    private class Cfg
    {
        public float Multiplier { get; set; } = 1.5f;
        public bool IgnoreTeammates { get; set; } = false;
        public bool IgnoreEnemy { get; set; } = true;
        public bool IgnoreSelf { get; set; } = false;
    }

    private static readonly Cfg DefaultCfg = new();

    public override string Name => "Pyro";
    public override string DisplayName => Core.Localizer["vip.module.pyro"];

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerHurt>(OnHurt);

    private HookResult OnHurt(EventPlayerHurt ev, GameEventInfo info)
    {
        if (ev.Weapon != "inferno")
            return HookResult.Continue;

        var attacker = ev.Attacker;
        if (!Active(attacker))
            return HookResult.Continue;

        var victim = ev.Userid;
        if (victim == null || !victim.IsValid)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(attacker!) ?? DefaultCfg;

        bool isSelf = victim.Slot == attacker!.Slot;
        bool isTeammate = !isSelf && victim.Team == attacker.Team;
        bool isEnemy = !isSelf && !isTeammate;

        if (isSelf && cfg.IgnoreSelf)
            return HookResult.Continue;
        if (isTeammate && cfg.IgnoreTeammates)
            return HookResult.Continue;
        if (isEnemy && cfg.IgnoreEnemy)
            return HookResult.Continue;

        var pawn = victim.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.Health <= 0)
            return HookResult.Continue;

        int heal = (int)(ev.DmgHealth * cfg.Multiplier);
        if (heal <= 0)
            return HookResult.Continue;

        int maxHp = pawn.MaxHealth > 0 ? pawn.MaxHealth : 100;
        int newHp = Math.Min(pawn.Health + heal, maxHp);
        if (newHp > pawn.Health)
        {
            pawn.Health = newHp;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }

        return HookResult.Continue;
    }
}
