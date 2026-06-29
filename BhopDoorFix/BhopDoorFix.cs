using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace BhopDoorFix;

public class BhopDoorFix : BasePlugin
{
    public override string ModuleName => "BhopDoorFix";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "ByDexter";
    public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

    private static readonly string[] DoorClasses =
    {
        "func_door"
    };

    public override void Load(bool hotReload)
    {
        RegisterListener<OnEntitySpawned>(OnEntitySpawned);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);

        if (hotReload)
            FreezeAllDoors();
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        if (entity == null || !entity.IsValid)
            return;

        if (Array.IndexOf(DoorClasses, entity.DesignerName) < 0)
            return;

        var door = entity.As<CBaseDoor>();
        Server.NextFrame(() => FreezeDoor(door));
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        FreezeAllDoors();
        return HookResult.Continue;
    }

    private void FreezeAllDoors()
    {
        foreach (var cls in DoorClasses)
        {
            foreach (var door in Utilities.FindAllEntitiesByDesignerName<CBaseDoor>(cls))
                FreezeDoor(door);
        }
    }

    private static void FreezeDoor(CBaseDoor? door)
    {
        if (door == null || !door.IsValid)
            return;

        try
        {
            door.Speed = 0f;
            door.AcceptInput("Lock");
        }
        catch { }
    }
}
