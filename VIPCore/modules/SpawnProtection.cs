using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
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

        if (!Active(player))
        {
            _until[slot] = 0f;
            return HookResult.Continue;
        }

        float seconds = GroupValue<float>(player!);
        float until = Server.CurrentTime + seconds;
        _until[slot] = until;

        if (seconds <= 0f || player!.IsBot)
            return HookResult.Continue;

        player.PrintToChat($" {CC.Orchid}{Core.Localizer["chat_prefix"]}{CC.Default} {Core.Localizer["vip.spawnprot.start", (int)seconds]}");

        Core.AddTimer(seconds, () =>
        {
            var p = Utilities.GetPlayerFromSlot(slot);
            if (p == null || !p.IsValid || p.IsBot || !IsAlive(p))
                return;
            if (Math.Abs(_until[slot] - until) > 0.01f)
                return;

            p.PrintToChat($" {CC.Orchid}{Core.Localizer["chat_prefix"]}{CC.Default} {Core.Localizer["vip.spawnprot.end"]}");
        }, TimerFlags.STOP_ON_MAPCHANGE);

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
