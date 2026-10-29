using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class GrenadeDamage : VipModule
{
    private class Cfg
    {
        public float HeDamageMultiplier { get; set; } = 2f;
        public float HeDamageRange { get; set; } = 2f;
        public float MolotovDamageMultiplier { get; set; } = 0f;
        public int Limit { get; set; } = 0;
    }

    private static readonly Cfg DefaultCfg = new();

    private readonly Dictionary<uint, float> _infernos = new();

    public override string Name => "GrenadeDamage";
    public override string DisplayName => Core.Localizer["vip.module.grenadedamage"];

    public override void OnLoad()
    {
        Core.HookEntitySpawned(OnEntitySpawned);
        Core.HookEntityDeleted(entity =>
        {
            if (_infernos.Count > 0 && entity.DesignerName == "inferno")
                _infernos.Remove(entity.EntityHandle.Raw);
        });
        Core.HookDamage(OnDamage);
        Core.HookMapStart(_ => _infernos.Clear());
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        string designer = entity.DesignerName;
        if (designer == "hegrenade_projectile")
            Server.NextFrame(() => BoostHe(entity));
        else if (designer == "inferno")
            Server.NextFrame(() => BoostInferno(entity));
    }

    private void BoostHe(CEntityInstance entity)
    {
        if (!entity.IsValid)
            return;

        var grenade = entity.As<CHEGrenadeProjectile>();
        if (grenade == null || !grenade.IsValid)
            return;

        var owner = PawnController(grenade.Thrower.Value ?? grenade.OwnerEntity.Value);
        if (!Active(owner))
            return;

        var cfg = GroupValue<Cfg>(owner) ?? DefaultCfg;
        if (cfg.HeDamageMultiplier <= 0f && cfg.HeDamageRange <= 0f)
            return;

        if (LimitReached(owner.Slot, cfg.Limit))
            return;

        if (cfg.HeDamageMultiplier > 0f)
            grenade.Damage *= cfg.HeDamageMultiplier;
        if (cfg.HeDamageRange > 0f)
            grenade.DmgRadius *= cfg.HeDamageRange;

        LimitUse(owner.Slot);
    }

    private void BoostInferno(CEntityInstance entity)
    {
        if (!entity.IsValid)
            return;

        var inferno = entity.As<CInferno>();
        if (inferno == null || !inferno.IsValid)
            return;

        var owner = PawnController(inferno.OwnerEntity.Value);
        if (!Active(owner))
            return;

        var cfg = GroupValue<Cfg>(owner) ?? DefaultCfg;
        if (cfg.MolotovDamageMultiplier <= 0f || cfg.MolotovDamageMultiplier == 1f)
            return;

        if (LimitReached(owner.Slot, cfg.Limit))
            return;

        _infernos[inferno.EntityHandle.Raw] = cfg.MolotovDamageMultiplier;
        LimitUse(owner.Slot);
    }

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        if (_infernos.Count == 0 || ((long)info.BitsDamageType & (long)DamageTypes_t.DMG_BURN) == 0)
            return HookResult.Continue;

        if (!_infernos.TryGetValue(info.Inflictor.Raw, out float multiplier))
            return HookResult.Continue;

        info.Damage *= multiplier;
        return HookResult.Changed;
    }
}
