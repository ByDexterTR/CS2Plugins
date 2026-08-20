using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class HealthshotBoost : VipModule
{
    private class Cfg
    {
        public float Duration { get; set; } = 5f;
        public float SpeedMultiplier { get; set; } = 1.3f;
        public float DamageMultiplier { get; set; } = 1.25f;
        public int Limit { get; set; } = 0;
    }

    private static readonly Cfg DefaultCfg = new();

    private readonly float[] _until = new float[64];
    private readonly float[] _lastBoost = new float[64];
    private readonly float[] _speed = new float[64];
    private readonly float[] _damage = new float[64];
    private readonly bool[] _slowApplied = new bool[64];

    public override string Name => "HealthshotBoost";
    public override string DisplayName => Core.Localizer["vip.module.healthshotboost"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) => { Reset(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        Core.RegisterListener<OnTick>(OnTick);
        Core.RegisterListener<OnEntityTakeDamagePre>(OnDamage);
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        Reset(ev.Userid?.Slot ?? -1);
        return HookResult.Continue;
    }

    private void Reset(int slot)
    {
        if (slot < 0 || slot >= 64)
            return;

        _until[slot] = 0f;
        _lastBoost[slot] = 0f;
        _speed[slot] = 1f;
        _damage[slot] = 1f;
        _slowApplied[slot] = false;
    }

    private void OnTick()
    {
        float now = Server.CurrentTime;

        foreach (var player in Core.Players)
        {
            if (player == null || !player.IsValid || player.IsBot || player.Slot >= 64)
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            int slot = player.Slot;
            float boost = pawn.HealthShotBoostExpirationTime;

            if (boost > now && boost > _lastBoost[slot] + 0.05f && Active(player))
            {
                var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;
                if (!LimitReached(slot, cfg.Limit))
                {
                    LimitUse(slot);
                    _until[slot] = now + Math.Max(cfg.Duration, 0.1f);
                    _speed[slot] = Math.Max(cfg.SpeedMultiplier, 1f);
                    _damage[slot] = Math.Max(cfg.DamageMultiplier, 0f);
                }
            }

            _lastBoost[slot] = boost;

            if (_until[slot] <= 0f)
                continue;

            if (now >= _until[slot] || !IsAlive(player))
            {
                _until[slot] = 0f;
                if (_slowApplied[slot])
                {
                    _slowApplied[slot] = false;
                    if (pawn.IsValid && Math.Abs(pawn.VelocityModifier - 1f) > 0.001f)
                    {
                        pawn.VelocityModifier = 1f;
                        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                    }
                }
                continue;
            }

            if (_speed[slot] > 1f && pawn.VelocityModifier < _speed[slot] - 0.001f)
            {
                _slowApplied[slot] = true;
                pawn.VelocityModifier = _speed[slot];
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }
    }

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        if (info.Attacker?.Value == null)
            return HookResult.Continue;

        var attacker = PawnController(info.Attacker.Value);
        if (attacker == null || attacker.Slot >= 64 || _until[attacker.Slot] <= Server.CurrentTime)
            return HookResult.Continue;

        float scale = _damage[attacker.Slot];
        if (Math.Abs(scale - 1f) < 0.001f)
            return HookResult.Continue;

        var victim = PawnController(entity);
        if (victim == null || victim.Slot == attacker.Slot || victim.Team == attacker.Team)
            return HookResult.Continue;

        info.Damage = MathF.Max(info.Damage * scale, 0f);
        return HookResult.Changed;
    }
}
