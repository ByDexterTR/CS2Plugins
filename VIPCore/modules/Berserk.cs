using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class Berserk : VipModule
{
    private class Cfg
    {
        public float Dpk { get; set; } = 0.2f;
        [JsonPropertyName("maxdpk")]
        public float MaxDpk { get; set; } = 5.0f;
    }

    private readonly float[] _bonus = new float[64];

    public override string Name => "Berserk";
    public override string DisplayName => Core.Localizer["vip.module.berserk"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerDeath>(OnDeath);
        Core.RegisterEventHandler<EventPlayerSpawn>((ev, _) =>
        {
            int slot = ev.Userid?.Slot ?? -1;
            if (slot >= 0 && slot < 64)
                _bonus[slot] = 0f;
            return HookResult.Continue;
        });
        Core.RegisterListener<OnEntityTakeDamagePre>(OnDamage);
    }

    private HookResult OnDeath(EventPlayerDeath ev, GameEventInfo info)
    {
        var attacker = ev.Attacker;
        var victim = ev.Userid;
        if (attacker == null || !attacker.IsValid || attacker.IsBot || victim == null
            || attacker.Slot == victim.Slot || !Active(attacker))
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(attacker) ?? new Cfg();
        int slot = attacker.Slot;
        _bonus[slot] = Math.Min(_bonus[slot] + cfg.Dpk, Math.Max(cfg.MaxDpk, 0f));
        return HookResult.Continue;
    }

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        if (info.Attacker?.Value == null)
            return HookResult.Continue;

        var attacker = PawnController(info.Attacker.Value);
        if (attacker == null || !Active(attacker))
            return HookResult.Continue;

        float bonus = _bonus[attacker.Slot];
        if (bonus <= 0)
            return HookResult.Continue;

        info.Damage *= 1f + bonus;
        return HookResult.Changed;
    }
}
