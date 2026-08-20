using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class GrenadeDamage : VipModule
{
    private class Cfg
    {
        public float DamageMultiplier { get; set; } = 2f;
        public float RangeMultiplier { get; set; } = 2f;
        public int Limit { get; set; } = 0;
    }

    private static readonly Cfg DefaultCfg = new();

    public override string Name => "GrenadeDamage";
    public override string DisplayName => Core.Localizer["vip.module.grenadedamage"];

    public override void OnLoad() => Core.RegisterListener<OnEntitySpawned>(OnEntitySpawned);

    private void OnEntitySpawned(CEntityInstance entity)
    {
        if (entity.DesignerName != "hegrenade_projectile")
            return;

        Server.NextFrame(() =>
        {
            if (!entity.IsValid)
                return;

            var grenade = entity.As<CHEGrenadeProjectile>();
            if (grenade == null || !grenade.IsValid)
                return;

            var owner = PawnController(grenade.Thrower.Value ?? grenade.OwnerEntity.Value);
            if (!Active(owner))
                return;

            var cfg = GroupValue<Cfg>(owner!) ?? DefaultCfg;
            if (cfg.DamageMultiplier <= 0f && cfg.RangeMultiplier <= 0f)
                return;

            if (LimitReached(owner!.Slot, cfg.Limit))
                return;

            if (cfg.DamageMultiplier > 0f)
                grenade.Damage *= cfg.DamageMultiplier;
            if (cfg.RangeMultiplier > 0f)
                grenade.DmgRadius *= cfg.RangeMultiplier;

            LimitUse(owner.Slot);
        });
    }
}
