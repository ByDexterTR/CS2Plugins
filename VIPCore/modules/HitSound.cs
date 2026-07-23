using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace VIPCore;

public class HitSound : VipModule
{
    private static ConVar? _cvFfa;

    private class Entry
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }

    public override string Name => "HitSound";
    public override string DisplayName => Core.Localizer["vip.module.hitsound"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player)
    {
        var entries = GroupValue<List<Entry>>(player) ?? new();
        return entries.Where(e => e.Name.Length > 0 && e.Path.Length > 0)
            .Select(e => new VipFeatureOption(e.Name, e.Name)).ToList();
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

        var entries = GroupValue<List<Entry>>(attacker) ?? new();
        var entry = entries.FirstOrDefault(e => e.Name == Setting(attacker));
        if (entry == null || entry.Path.Length == 0)
            return HookResult.Continue;

        attacker.ExecuteClientCommand($"play {entry.Path}");
        return HookResult.Continue;
    }
}
