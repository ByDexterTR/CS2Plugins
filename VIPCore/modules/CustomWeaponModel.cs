using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class CustomWeaponModel : VipModule
{
    public class Entry
    {
        public string Name { get; set; } = "";
        public string Weapon { get; set; } = "";
        public string Model { get; set; } = "";
    }

    private class Applied
    {
        public bool IsSubclass;
        public string? OriginalModel;
    }

    private readonly Dictionary<uint, Applied> _applied = new();

    public override string Name => "CustomWeaponModel";
    public override string DisplayName => Core.Localizer["vip.module.customweaponmodel"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public static List<Entry> Usable(List<Entry>? entries) =>
        entries == null
            ? new()
            : entries.Where(e => e.Name.Length > 0 && e.Weapon.Length > 0 && e.Model.Length > 0).ToList();

    public static string Category(string weapon) => weapon.ToLowerInvariant();

    public override List<VipFeatureOption> SelectCategories(CCSPlayerController player)
    {
        var categories = new List<VipFeatureOption>();
        var seen = new HashSet<string>();

        foreach (var entry in Usable(GroupValue<List<Entry>>(player)))
        {
            string category = Category(entry.Weapon);
            if (seen.Add(category))
                categories.Add(new VipFeatureOption(WeaponUtil.Label(entry.Weapon), category));
        }

        return categories;
    }

    public override List<VipFeatureOption> CategoryOptions(CCSPlayerController player, string category) =>
        Usable(GroupValue<List<Entry>>(player))
            .Where(e => Category(e.Weapon) == category)
            .Select(e => new VipFeatureOption(e.Name, e.Name))
            .ToList();

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerSpawn>((ev, _) => { Schedule(ev.Userid); return HookResult.Continue; });
        Core.RegisterEventHandler<EventItemPickup>((ev, _) => { Schedule(ev.Userid); return HookResult.Continue; });
        Core.HookEntityDeleted(entity => _applied.Remove(entity.Index));
        Core.HookPrecache(manifest =>
        {
            foreach (var entries in Core.GetAllGroupValues<List<Entry>>(Name))
                foreach (var entry in entries)
                    if (entry.Model.Length > 0 && !int.TryParse(entry.Model, out _))
                        manifest.AddResource(entry.Model);
        });
    }

    public override void OnUnload()
    {
        foreach (var (index, applied) in _applied)
        {
            var weapon = Utilities.GetEntityFromIndex<CBasePlayerWeapon>((int)index);
            if (weapon != null && weapon.IsValid)
                Revert(weapon, applied);
        }
        _applied.Clear();
    }

    public override void OnSelect(CCSPlayerController player, string value) => Schedule(player);

    private void Schedule(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        Server.NextFrame(() => ApplyAll(player));
    }

    private void ApplyAll(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid)
            return;

        var entries = IsAlive(player) && Active(player) ? Usable(GroupValue<List<Entry>>(player)) : new();

        var weapons = player.PlayerPawn.Value?.WeaponServices?.MyWeapons;
        if (weapons == null)
            return;

        foreach (var handle in weapons)
        {
            var weapon = handle.Value;
            if (weapon == null || !weapon.IsValid || string.IsNullOrEmpty(weapon.DesignerName))
                continue;

            int itemDef = 0;
            try { itemDef = weapon.AttributeManager.Item.ItemDefinitionIndex; }
            catch { }

            var wanted = entries.Count > 0
                ? Pick(player, entries, WeaponUtil.NormalizeWeaponName(weapon.DesignerName, itemDef))
                : null;

            if (wanted != null)
                Apply(weapon, wanted.Model);
            else if (_applied.TryGetValue(weapon.Index, out var applied))
                Revert(weapon, applied);
        }
    }

    private Entry? Pick(CCSPlayerController player, List<Entry> entries, string weaponName)
    {
        var entry = Choose(player, entries, weaponName);
        if (entry != null)
            return entry;

        if (WeaponUtil.IsKnife(weaponName) && !weaponName.Equals("weapon_knife", StringComparison.OrdinalIgnoreCase))
            return Choose(player, entries, "weapon_knife");

        return null;
    }

    private Entry? Choose(CCSPlayerController player, List<Entry> entries, string weaponName)
    {
        string category = Category(weaponName);
        string selection = CategorySetting(player, category);
        if (selection.Length == 0 || selection == "off")
            return null;

        return entries.FirstOrDefault(e => e.Name == selection && Category(e.Weapon) == category);
    }

    private void Apply(CBasePlayerWeapon weapon, string model)
    {
        bool isSubclass = int.TryParse(model, out _);

        if (!_applied.TryGetValue(weapon.Index, out var applied))
        {
            applied = new Applied
            {
                IsSubclass = isSubclass,
                OriginalModel = isSubclass ? null : weapon.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState.ModelName
            };
            _applied[weapon.Index] = applied;
        }

        if (isSubclass)
            weapon.AcceptInput("ChangeSubclass", weapon, weapon, model);
        else
            weapon.SetModel(model);
    }

    private void Revert(CBasePlayerWeapon weapon, Applied applied)
    {
        if (applied.IsSubclass)
            weapon.AcceptInput("ChangeSubclass", weapon, weapon, "0");
        else if (!string.IsNullOrEmpty(applied.OriginalModel))
            weapon.SetModel(applied.OriginalModel);

        _applied.Remove(weapon.Index);
    }
}
