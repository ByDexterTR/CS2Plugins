using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class Outfit : VipModule
{
    public class Entry
    {
        public string Name { get; set; } = "";
        public string Model { get; set; } = "";
        public string Team { get; set; } = "";
    }

    public class Cfg : Dictionary<string, List<Entry>> { }

    private readonly List<CDynamicProp>?[] _worn = new List<CDynamicProp>?[64];

    public override string Name => "Outfit";
    public override string DisplayName => Core.Localizer["vip.module.outfit"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectCategories(CCSPlayerController player)
    {
        var cfg = GroupValue<Cfg>(player) ?? new Cfg();
        var categories = new List<VipFeatureOption>();

        foreach (var (name, entries) in cfg)
            if (Usable(entries, player).Count > 0)
                categories.Add(new VipFeatureOption(Label(name), name));

        return categories;
    }

    public override List<VipFeatureOption> CategoryOptions(CCSPlayerController player, string category)
    {
        var cfg = GroupValue<Cfg>(player) ?? new Cfg();
        if (!cfg.TryGetValue(category, out var entries))
            return new();

        return Usable(entries, player).Select(e => new VipFeatureOption(e.Name, e.Name)).ToList();
    }

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerSpawn>((ev, _) =>
        {
            var player = ev.Userid;
            Server.NextFrame(() => Apply(player));
            return HookResult.Continue;
        });
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) => { Remove(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) => { Remove(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        Core.RegisterEventHandler<EventRoundEnd>((_, __) => { RemoveAll(); return HookResult.Continue; });
        Core.HookMapStart(_ => Array.Clear(_worn));
        Core.HookPrecache(manifest =>
        {
            foreach (var cfg in Core.GetAllGroupValues<Cfg>(Name))
                foreach (var entries in cfg.Values)
                    foreach (var entry in entries)
                        if (entry.Model.Length > 0)
                            try { manifest.AddResource(entry.Model); }
                            catch { }
        });
    }

    public override void OnUnload() => RemoveAll();

    public override void OnSelect(CCSPlayerController player, string value) => Apply(player);

    private static string Label(string name) =>
        name.Length > 0 ? char.ToUpperInvariant(name[0]) + name[1..] : name;

    private static List<Entry> Usable(List<Entry> entries, CCSPlayerController player)
    {
        var list = new List<Entry>();
        foreach (var entry in entries)
        {
            if (entry.Name.Length == 0 || entry.Model.Length == 0)
                continue;

            if (entry.Team.Length > 0)
            {
                var team = entry.Team.Equals("CT", StringComparison.OrdinalIgnoreCase)
                    ? CsTeam.CounterTerrorist
                    : entry.Team.Equals("T", StringComparison.OrdinalIgnoreCase)
                        ? CsTeam.Terrorist
                        : CsTeam.None;

                if (team != CsTeam.None && player.Team != team)
                    continue;
            }

            list.Add(entry);
        }
        return list;
    }

    private void RemoveAll()
    {
        for (int slot = 0; slot < 64; slot++)
            Remove(slot);
    }

    private void Remove(int slot)
    {
        if (slot < 0 || slot >= 64)
            return;

        var worn = _worn[slot];
        _worn[slot] = null;

        if (worn == null)
            return;

        foreach (var prop in worn)
            if (prop.IsValid)
                prop.Remove();
    }

    private void Apply(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.Slot >= 64)
            return;

        Remove(player.Slot);

        if (!IsAlive(player) || !Active(player))
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
            return;

        var cfg = GroupValue<Cfg>(player) ?? new Cfg();
        if (cfg.Count == 0)
            return;

        var worn = new List<CDynamicProp>();

        foreach (var (category, entries) in cfg)
        {
            string selection = CategorySetting(player, category);
            if (selection == "off" || selection.Length == 0)
                continue;

            var entry = Usable(entries, player).FirstOrDefault(e => e.Name == selection);
            if (entry == null)
                continue;

            var prop = Wear(pawn, entry.Model);
            if (prop != null)
                worn.Add(prop);
        }

        if (worn.Count > 0)
            _worn[player.Slot] = worn;
    }

    private static CDynamicProp? Wear(CCSPlayerPawn pawn, string model)
    {
        var prop = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
        if (prop == null || !prop.IsValid)
            return null;

        prop.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
        prop.SetModel(model);
        prop.Teleport(pawn.AbsOrigin, new QAngle(), new Vector());
        prop.DispatchSpawn();
        prop.AcceptInput("FollowEntity", pawn, prop, "!activator");

        return prop;
    }
}
