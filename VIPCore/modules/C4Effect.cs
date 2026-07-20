using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class C4Effect : VipModule
{
    private class Entry
    {
        public string Name { get; set; } = "";
        public string Particle { get; set; } = "";
        public bool Defuse { get; set; }
    }

    private CParticleSystem? _plantParticle;
    private ulong _planterSteamId;

    public override string Name => "C4Effect";
    public override string DisplayName => Core.Localizer["vip.module.c4effect"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectCategories(CCSPlayerController player) => new()
    {
        new VipFeatureOption(Core.Localizer["vip.c4effect.plant"], "plant"),
        new VipFeatureOption(Core.Localizer["vip.c4effect.defuse"], "defuse")
    };

    public override List<VipFeatureOption> CategoryOptions(CCSPlayerController player, string category) =>
        Entries(player, category).Select(e => new VipFeatureOption(e.Name, e.Name)).ToList();

    private List<Entry> Entries(CCSPlayerController player, string category)
    {
        bool defuse = category == "defuse";
        return (GroupValue<List<Entry>>(player) ?? new())
            .Where(e => e.Name.Length > 0 && e.Particle.Length > 0 && e.Defuse == defuse)
            .ToList();
    }

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventBombPlanted>(OnPlanted);
        Core.RegisterEventHandler<EventBombDefused>(OnDefused);
        Core.RegisterEventHandler<EventBombExploded>((_, __) => { RemovePlantParticle(); return HookResult.Continue; });
        Core.RegisterEventHandler<EventRoundStart>((_, __) => { RemovePlantParticle(); return HookResult.Continue; });
        Core.RegisterListener<OnServerPrecacheResources>(manifest =>
        {
            foreach (var entries in Core.GetAllGroupValues<List<Entry>>(Name))
                foreach (var entry in entries)
                    if (entry.Particle.Length > 0)
                        manifest.AddResource(entry.Particle);
        });
    }

    public override void OnUnload() => RemovePlantParticle();

    private Entry? Pick(CCSPlayerController player, string category)
    {
        string selected = CategorySetting(player, category);
        if (selected == "off" || selected.Length == 0)
            return null;

        return Entries(player, category).FirstOrDefault(e => e.Name == selected);
    }

    private HookResult OnPlanted(EventBombPlanted ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        var entry = Pick(player!, "plant");
        if (entry == null)
            return HookResult.Continue;

        _planterSteamId = player!.SteamID;

        Server.NextFrame(() =>
        {
            var c4 = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault(c => c != null && c.IsValid);
            if (c4?.AbsOrigin == null)
                return;

            RemovePlantParticle();
            _plantParticle = ParticleUtil.Spawn(entry.Particle, c4.AbsOrigin, c4);
        });

        return HookResult.Continue;
    }

    private HookResult OnDefused(EventBombDefused ev, GameEventInfo info)
    {
        var bombPos = _plantParticle != null && _plantParticle.IsValid && _plantParticle.AbsOrigin != null
            ? new Vector(_plantParticle.AbsOrigin.X, _plantParticle.AbsOrigin.Y, _plantParticle.AbsOrigin.Z)
            : null;

        if (bombPos == null)
        {
            var c4 = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault(c => c != null && c.IsValid);
            if (c4?.AbsOrigin != null)
                bombPos = new Vector(c4.AbsOrigin.X, c4.AbsOrigin.Y, c4.AbsOrigin.Z);
        }

        RemovePlantParticle();

        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        var entry = Pick(player!, "defuse");
        if (entry == null)
            return HookResult.Continue;

        if (bombPos == null)
        {
            var origin = player!.PlayerPawn.Value?.AbsOrigin;
            if (origin == null)
                return HookResult.Continue;

            bombPos = new Vector(origin.X, origin.Y, origin.Z);
        }

        var pos = new Vector(bombPos.X, bombPos.Y, bombPos.Z + 10f);
        ParticleUtil.Burst(Core, entry.Particle, pos, 3.0f);
        return HookResult.Continue;
    }

    private void RemovePlantParticle()
    {
        if (_plantParticle != null && _plantParticle.IsValid)
            _plantParticle.Remove();
        _plantParticle = null;
        _planterSteamId = 0;
    }
}
