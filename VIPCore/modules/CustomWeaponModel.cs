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
        Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
        Core.RegisterEventHandler<EventItemPickup>(OnPickup);
        Core.RegisterListener<OnServerPrecacheResources>(manifest =>
        {
            foreach (var entries in Core.GetAllGroupValues<List<Entry>>(Name))
                foreach (var entry in entries)
                    if (entry.Model.Length > 0 && !int.TryParse(entry.Model, out _))
                        manifest.AddResource(entry.Model);
        });
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        Schedule(ev.Userid);
        return HookResult.Continue;
    }

    private HookResult OnPickup(EventItemPickup ev, GameEventInfo info)
    {
        Schedule(ev.Userid);
        return HookResult.Continue;
    }

    private void Schedule(CCSPlayerController? player)
    {
        if (!Active(player))
            return;

        Server.NextFrame(() => ApplyAll(player));
    }

    private void ApplyAll(CCSPlayerController? player)
    {
        if (!IsAlive(player) || !Active(player))
            return;

        var entries = GroupValue<List<Entry>>(player!) ?? new();
        var entry = entries.FirstOrDefault(e => e.Name == Setting(player!));
        if (entry == null || entry.Weapon.Length == 0 || entry.Model.Length == 0)
            return;

        var weapons = player!.PlayerPawn.Value?.WeaponServices?.MyWeapons;
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

            if (WeaponUtil.NormalizeWeaponName(weapon.DesignerName, itemDef) != entry.Weapon)
                continue;

            if (int.TryParse(entry.Model, out _))
                weapon.AcceptInput("ChangeSubclass", weapon, weapon, entry.Model);
            else
                weapon.SetModel(entry.Model);
        }
    }
}
