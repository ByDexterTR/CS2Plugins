using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class Sacrifice : VipModule
{
    private class Cfg
    {
        public int Hp { get; set; } = 25;
        public int Armor { get; set; } = 25;
        public bool Helmet { get; set; } = false;
        public string Weapons { get; set; } = "";

        private List<string>? _give;
        public List<string> Give => _give ??= WeaponUtil.ParseCsv(Weapons);
    }

    private static readonly Cfg DefaultCfg = new();

    public override string Name => "Sacrifice";
    public override string DisplayName => Core.Localizer["vip.module.sacrifice"];

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerDeath>(OnDeath);

    private HookResult OnDeath(EventPlayerDeath ev, GameEventInfo info)
    {
        var victim = ev.Userid;
        if (!Active(victim))
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(victim!) ?? DefaultCfg;
        if (cfg.Hp <= 0 && cfg.Armor <= 0 && !cfg.Helmet && cfg.Give.Count == 0)
            return HookResult.Continue;

        var team = victim!.Team;
        int slot = victim.Slot;

        foreach (var mate in Core.Players)
        {
            if (mate == null || !mate.IsValid || mate.Slot == slot || mate.Team != team || !IsAlive(mate))
                continue;

            var pawn = mate.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            if (cfg.Hp > 0)
            {
                int maxHp = pawn.MaxHealth > 0 ? pawn.MaxHealth : 100;
                if (pawn.Health < maxHp)
                {
                    pawn.Health = Math.Min(pawn.Health + cfg.Hp, maxHp);
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                }
            }

            if (cfg.Armor > 0 || cfg.Helmet)
            {
                int newArmor = Math.Min(pawn.ArmorValue + Math.Max(cfg.Armor, 0), 100);
                mate.GiveNamedItem(cfg.Helmet ? "item_assaultsuit" : "item_kevlar");
                pawn.ArmorValue = newArmor;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
            }

            foreach (var weapon in cfg.Give)
            {
                if (!HasWeapon(mate, weapon))
                    mate.GiveNamedItem(weapon);
            }
        }

        return HookResult.Continue;
    }
}
