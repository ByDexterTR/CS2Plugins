using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class GlueGrenade : VipModule
{
    private class Cfg
    {
        public string OnlyGrenades { get; set; } = "";

        private List<string>? _allow;
        public List<string> Allow => _allow ??= WeaponUtil.ParseCsv(OnlyGrenades);
    }

    private static readonly Cfg DefaultCfg = new();

    public override string Name => "GlueGrenade";
    public override string DisplayName => Core.Localizer["vip.module.gluegrenade"];

    public override void OnLoad() => Core.RegisterListener<OnEntitySpawned>(OnEntitySpawned);

    private void OnEntitySpawned(CEntityInstance entity)
    {
        var name = entity.DesignerName;
        if (string.IsNullOrEmpty(name) || !name.EndsWith("_projectile"))
            return;

        var grenade = entity.As<CBaseCSGrenadeProjectile>();
        if (grenade == null || !grenade.IsValid || grenade.OwnerEntity?.Value == null)
            return;

        var player = PawnController(grenade.OwnerEntity.Value);
        if (!Active(player))
            return;

        var cfg = GroupValue<Cfg>(player!) ?? DefaultCfg;
        var allow = cfg.Allow;
        if (allow.Count > 0)
        {
            string type = name[..^"_projectile".Length];
            if (!allow.Any(a => a.Equals(type, StringComparison.OrdinalIgnoreCase)))
                return;
        }

        grenade.Bounces = 555;
    }
}
