using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class CustomWeaponModel : VipModule
{
    private class Entry
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

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player)
    {
        var entries = GroupValue<List<Entry>>(player) ?? new();
        return entries.Where(e => e.Name.Length > 0 && e.Weapon.Length > 0 && e.Model.Length > 0)
            .Select(e => new VipFeatureOption(e.Name, e.Name)).ToList();
    }

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerSpawn>((ev, _) => { Schedule(ev.Userid); return HookResult.Continue; });
        Core.RegisterEventHandler<EventItemPickup>((ev, _) => { Schedule(ev.Userid); return HookResult.Continue; });
        Core.RegisterListener<OnEntityDeleted>(entity => _applied.Remove(entity.Index));
        Core.RegisterListener<OnServerPrecacheResources>(manifest =>
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

        Entry? wanted = null;
        if (IsAlive(player) && Active(player))
        {
            var entries = GroupValue<List<Entry>>(player) ?? new();
            wanted = entries.FirstOrDefault(e => e.Name == Setting(player));
        }

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

            string weaponName = WeaponUtil.NormalizeWeaponName(weapon.DesignerName, itemDef);
            bool matches = wanted != null && wanted.Weapon.Length > 0 && wanted.Model.Length > 0 && wanted.Weapon == weaponName;

            if (matches)
                Apply(weapon, wanted!.Model);
            else if (_applied.TryGetValue(weapon.Index, out var applied))
                Revert(weapon, applied);
        }
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
