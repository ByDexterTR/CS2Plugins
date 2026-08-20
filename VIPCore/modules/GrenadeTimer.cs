using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class GrenadeTimer : VipModule
{
    private class Cfg
    {
        public List<double> Hegrenade { get; set; } = new();
        public List<double> Flashbang { get; set; } = new();
        public List<double> Molotov { get; set; } = new();
        public List<double> Decoy { get; set; } = new();
        public int Limit { get; set; } = 0;
    }

    private static readonly Cfg DefaultCfg = new();

    private static readonly (string Key, string Designer, string LangKey)[] Types =
    {
        ("hegrenade", "hegrenade_projectile", "vip.grenade.he"),
        ("flashbang", "flashbang_projectile", "vip.grenade.flash"),
        ("molotov", "molotov_projectile", "vip.grenade.molotov"),
        ("decoy", "decoy_projectile", "vip.grenade.decoy")
    };

    public override string Name => "GrenadeTimer";
    public override string DisplayName => Core.Localizer["vip.module.grenadetimer"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectCategories(CCSPlayerController player)
    {
        var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;
        var categories = new List<VipFeatureOption>();

        foreach (var type in Types)
            if (Values(cfg, type.Key).Count > 0)
                categories.Add(new VipFeatureOption(Core.Localizer[type.LangKey], type.Key));

        return categories;
    }

    public override List<VipFeatureOption> CategoryOptions(CCSPlayerController player, string category)
    {
        var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;

        return Values(cfg, category)
            .Select(value =>
            {
                string text = value.ToString(CultureInfo.InvariantCulture);
                return new VipFeatureOption($"+{text}", text);
            })
            .ToList();
    }

    public override void OnLoad() => Core.RegisterListener<OnEntitySpawned>(OnEntitySpawned);

    private void OnEntitySpawned(CEntityInstance entity)
    {
        string designer = entity.DesignerName;
        if (!designer.EndsWith("_projectile", StringComparison.Ordinal))
            return;

        string? category = Types.FirstOrDefault(type => type.Designer == designer).Key;
        if (category == null)
            return;

        Server.NextFrame(() =>
        {
            if (!entity.IsValid)
                return;

            var grenade = entity.As<CBaseCSGrenadeProjectile>();
            if (grenade == null || !grenade.IsValid)
                return;

            var owner = PawnController(grenade.Thrower.Value ?? grenade.OwnerEntity.Value);
            if (!Active(owner))
                return;

            string setting = CategorySetting(owner!, category);
            if (setting == "off" || !double.TryParse(setting, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                return;

            var cfg = GroupValue<Cfg>(owner!) ?? DefaultCfg;
            if (!Values(cfg, category).Contains(value))
                return;

            if (LimitReached(owner!.Slot, cfg.Limit))
                return;

            grenade.DetonateTime += (float)Math.Clamp(value, 0.1, 20.0);
            Utilities.SetStateChanged(grenade, "CBaseGrenade", "m_flDetonateTime");

            LimitUse(owner.Slot);
        });
    }

    private static List<double> Values(Cfg cfg, string category) => category switch
    {
        "hegrenade" => cfg.Hegrenade,
        "flashbang" => cfg.Flashbang,
        "molotov" => cfg.Molotov,
        "decoy" => cfg.Decoy,
        _ => new List<double>()
    };
}
