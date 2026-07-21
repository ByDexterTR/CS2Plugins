using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class KillEffect : VipModule
{
    private class Entry
    {
        public string Name { get; set; } = "";
        public string Particle { get; set; } = "";
        public bool Hs { get; set; }

        [JsonPropertyName("lastkill")]
        public bool LastKill { get; set; }
    }

    public override string Name => "KillEffect";
    public override string DisplayName => Core.Localizer["vip.module.killeffect"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectCategories(CCSPlayerController player) => new()
    {
        new VipFeatureOption(Core.Localizer["vip.killeffect.normal"], "normal"),
        new VipFeatureOption(Core.Localizer["vip.killeffect.hs"], "hs"),
        new VipFeatureOption(Core.Localizer["vip.killeffect.last"], "last")
    };

    public override List<VipFeatureOption> CategoryOptions(CCSPlayerController player, string category) =>
        Entries(player, category).Select(e => new VipFeatureOption(e.Name, e.Name)).ToList();

    private List<Entry> Entries(CCSPlayerController player, string category)
    {
        var all = (GroupValue<List<Entry>>(player) ?? new())
            .Where(e => e.Name.Length > 0 && e.Particle.Length > 0);

        return category switch
        {
            "hs" => all.Where(e => e.Hs && !e.LastKill).ToList(),
            "last" => all.Where(e => e.LastKill).ToList(),
            _ => all.Where(e => !e.Hs && !e.LastKill).ToList()
        };
    }

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerDeath>(OnDeath);
        Core.RegisterListener<OnServerPrecacheResources>(manifest =>
        {
            foreach (var entries in Core.GetAllGroupValues<List<Entry>>(Name))
                foreach (var entry in entries)
                    if (entry.Particle.Length > 0)
                        manifest.AddResource(entry.Particle);
        });
    }

    private HookResult OnDeath(EventPlayerDeath ev, GameEventInfo info)
    {
        var attacker = ev.Attacker;
        var victim = ev.Userid;
        if (attacker == null || !attacker.IsValid || attacker.IsBot || victim == null
            || attacker.Slot == victim.Slot || !Active(attacker))
            return HookResult.Continue;

        var origin = victim.PlayerPawn.Value?.AbsOrigin;
        if (origin == null)
            return HookResult.Continue;

        string category = IsLastEnemy(victim) ? "last" : ev.Headshot ? "hs" : "normal";
        var entry = Pick(attacker, category);

        if (entry == null)
            return HookResult.Continue;

        var pos = new Vector(origin.X, origin.Y, origin.Z + 40f);
        ParticleUtil.Burst(Core, entry.Particle, pos, 2.0f);
        return HookResult.Continue;
    }

    private Entry? Pick(CCSPlayerController attacker, string category)
    {
        string selected = CategorySetting(attacker, category);
        if (selected == "off" || selected.Length == 0)
            return null;

        return Entries(attacker, category).FirstOrDefault(e => e.Name == selected);
    }

    private static bool IsLastEnemy(CCSPlayerController victim)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || player.Slot == victim.Slot)
                continue;

            if (player.Team != victim.Team)
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn != null && pawn.IsValid && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                return false;
        }

        return true;
    }
}
