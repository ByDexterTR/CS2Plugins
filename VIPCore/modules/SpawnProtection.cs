using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class SpawnProtection : VipModule
{
    private readonly float[] _until = new float[64];

    public override string Name => "SpawnProtection";
    public override string DisplayName => Core.Localizer["vip.module.spawnprotection"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
        Core.RegisterListener<OnEntityTakeDamagePre>(OnDamage);
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        int slot = player?.Slot ?? -1;
        if (slot < 0 || slot >= 64)
            return HookResult.Continue;

        _until[slot] = Active(player) ? Server.CurrentTime + GroupValue<float>(player!) : 0f;
        return HookResult.Continue;
    }

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        var victim = PawnController(entity);
        if (victim == null)
            return HookResult.Continue;

        int slot = victim.Slot;
        if (slot < 0 || slot >= 64 || _until[slot] <= Server.CurrentTime)
            return HookResult.Continue;

        info.Damage = 0;
        return HookResult.Handled;
    }
}
