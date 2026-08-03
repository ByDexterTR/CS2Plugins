using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class HitSound : VipModule
{
    private static ConVar? _cvFfa;

    private class Entry
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string Emit { get; set; } = "";
        public float Volume { get; set; } = 1f;
        public bool Hs { get; set; }
    }

    public override string Name => "HitSound";
    public override string DisplayName => Core.Localizer["vip.module.hitsound"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectCategories(CCSPlayerController player)
    {
        var cats = new List<VipFeatureOption>();
        if (Entries(player, "normal").Count > 0)
            cats.Add(new VipFeatureOption(Core.Localizer["vip.hitsound.normal"], "normal"));
        if (Entries(player, "hs").Count > 0)
            cats.Add(new VipFeatureOption(Core.Localizer["vip.hitsound.hs"], "hs"));
        return cats;
    }

    public override List<VipFeatureOption> CategoryOptions(CCSPlayerController player, string category) =>
        Entries(player, category).Select(e => new VipFeatureOption(e.Name, e.Name)).ToList();

    private List<Entry> Entries(CCSPlayerController player, string category)
    {
        var all = (GroupValue<List<Entry>>(player) ?? new())
            .Where(e => e.Name.Length > 0 && (e.Path.Length > 0 || e.Emit.Length > 0));

        return category == "hs"
            ? all.Where(e => e.Hs).ToList()
            : all.Where(e => !e.Hs).ToList();
    }

    private Entry? Pick(CCSPlayerController player, string category)
    {
        string setting = CategorySetting(player, category);
        return setting == "off" ? null : Entries(player, category).FirstOrDefault(e => e.Name == setting);
    }

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerHurt>(OnHurt);

    private HookResult OnHurt(EventPlayerHurt ev, GameEventInfo info)
    {
        var attacker = ev.Attacker;
        var victim = ev.Userid;
        if (attacker == null || !attacker.IsValid || attacker.IsBot || victim == null || attacker.Slot == victim.Slot)
            return HookResult.Continue;

        if (!Active(attacker))
            return HookResult.Continue;

        _cvFfa ??= ConVar.Find("mp_teammates_are_enemies");
        bool ffa = _cvFfa?.GetPrimitiveValue<bool>() ?? false;
        if (!ffa && victim.Team == attacker.Team)
            return HookResult.Continue;

        bool headshot = ev.Hitgroup == (int)HitGroup_t.HITGROUP_HEAD;
        var entry = headshot ? Pick(attacker, "hs") : null;
        entry ??= Pick(attacker, "normal");

        if (entry == null)
            return HookResult.Continue;

        var listeners = new List<CCSPlayerController> { attacker };
        AddSpectators(attacker, listeners);

        SoundUtil.PlayFor(attacker, listeners, entry.Path, entry.Emit, entry.Volume);
        return HookResult.Continue;
    }
}
