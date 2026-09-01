using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class HealthshotEffect : VipModule
{
    private class PoisonCfg
    {
        [JsonPropertyName("minhp")]
        public int MinHp { get; set; } = 1;
        public int Damage { get; set; } = 2;
        public float Time { get; set; } = 5f;
        public float Tick { get; set; } = 0.5f;
        public float SoundVolume { get; set; } = 0.3f;
        public float Radius { get; set; } = 180f;
        public bool IgnoreTeammates { get; set; } = true;
        public bool IgnoreSelf { get; set; } = true;
        public int Limit { get; set; } = 0;
    }

    private class HealCfg
    {
        public int Heal { get; set; } = 2;
        public float Time { get; set; } = 5f;
        public float Tick { get; set; } = 0.5f;
        public float SoundVolume { get; set; } = 0.5f;
        public float Radius { get; set; } = 180f;
        public bool IgnoreTeammates { get; set; } = false;
        public bool IgnoreSelf { get; set; } = false;
        public bool IgnoreEnemy { get; set; } = true;
        public int Limit { get; set; } = 0;
    }

    private class SlowCfg
    {
        public int Percent { get; set; } = 30;
        [JsonPropertyName("minspeed")]
        public float MinSpeed { get; set; } = 100f;
        public float Time { get; set; } = 5f;
        public float Radius { get; set; } = 180f;
        public bool IgnoreTeammates { get; set; } = true;
        public bool IgnoreSelf { get; set; } = true;
        public bool IgnoreEnemy { get; set; } = false;
        public int Limit { get; set; } = 0;
    }

    private class SpeedCfg
    {
        public float SpeedMultiplier { get; set; } = 1.3f;
        public float Time { get; set; } = 5f;
        public int Limit { get; set; } = 0;
    }

    private class StrengthCfg
    {
        public float DamageMultiplier { get; set; } = 1.5f;
        public float Time { get; set; } = 5f;
        public float Radius { get; set; } = 0f;
        public bool IgnoreTeammates { get; set; } = false;
        public bool IgnoreSelf { get; set; } = false;
        public bool IgnoreEnemy { get; set; } = true;
        public int Limit { get; set; } = 0;
    }

    private class WallhackCfg
    {
        public float Time { get; set; } = 5f;
        public float Tick { get; set; } = 0.25f;
        public string Color { get; set; } = GlowPool.DefaultColor;
        public float Radius { get; set; } = 0f;
        public bool SeeTeammates { get; set; }
        public int OnlyMode { get; set; } = 0;
        public int Limit { get; set; } = 0;
    }

    private class RadarhackCfg
    {
        public float Time { get; set; } = 5f;
        public float Tick { get; set; } = 0.25f;
        public float Radius { get; set; } = 0f;
        public bool SeeTeammates { get; set; }
        public int OnlyMode { get; set; } = 0;
        public int Limit { get; set; } = 0;
    }

    private class MagneticCfg
    {
        public float Strength { get; set; } = 30f;
        public float Time { get; set; } = 5f;
        public float Radius { get; set; } = 180f;
        public bool IgnoreTeammates { get; set; } = true;
        public bool IgnoreSelf { get; set; } = true;
        public bool IgnoreEnemy { get; set; } = false;
        public int Limit { get; set; } = 0;
    }

    private class Cfg
    {
        public PoisonCfg? Poison { get; set; }
        public HealCfg? Heal { get; set; }
        public SlowCfg? Slow { get; set; }
        public SpeedCfg? Speed { get; set; }
        public StrengthCfg? Strength { get; set; }
        public WallhackCfg? Wallhack { get; set; }
        public RadarhackCfg? Radarhack { get; set; }
        public MagneticCfg? Magnetic { get; set; }
    }

    private class Running
    {
        public required string Mode;
        public required float ExpireAt;
        public float NextTick;
        public ulong Seen;
    }

    private static readonly Cfg DefaultCfg = new();

    private readonly Running?[] _active = new Running?[64];
    private readonly float[] _lastBoost = new float[64];
    private readonly int[] _strengthTick = new int[64];
    private readonly float[] _strength = new float[64];
    private readonly float[] _speed = new float[64];
    private readonly HashSet<int> _slowed = new();
    private readonly HashSet<int> _slowedThisTick = new();
    private bool _glowUser;

    public override string Name => "HealthshotEffect";
    public override string DisplayName => Core.Localizer["vip.module.healthshoteffect"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player)
    {
        var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;
        var options = new List<VipFeatureOption>();
        if (cfg.Poison != null) options.Add(new VipFeatureOption(Core.Localizer["vip.decoy.poison"], "poison"));
        if (cfg.Heal != null) options.Add(new VipFeatureOption(Core.Localizer["vip.decoy.heal"], "heal"));
        if (cfg.Slow != null) options.Add(new VipFeatureOption(Core.Localizer["vip.decoy.slow"], "slow"));
        if (cfg.Speed != null) options.Add(new VipFeatureOption(Core.Localizer["vip.decoy.speed"], "speed"));
        if (cfg.Strength != null) options.Add(new VipFeatureOption(Core.Localizer["vip.decoy.strength"], "strength"));
        if (cfg.Wallhack != null) options.Add(new VipFeatureOption(Core.Localizer["vip.decoy.wallhack"], "wallhack"));
        if (cfg.Radarhack != null) options.Add(new VipFeatureOption(Core.Localizer["vip.decoy.radarhack"], "radarhack"));
        if (cfg.Magnetic != null) options.Add(new VipFeatureOption(Core.Localizer["vip.decoy.magnetic"], "magnetic"));
        return options;
    }

    public override void OnLoad()
    {
        if (Core.GetAllGroupValues<Cfg>(Name).Any(c => c?.Wallhack != null))
        {
            GlowPool.Ensure(Core);
            GlowPool.Acquire();
            _glowUser = true;
        }

        ActivityFilter.Ensure(Core);

        Core.HookTick(OnTick);
        Core.HookDamage(OnDamage);
        Core.RegisterEventHandler<EventPlayerSpawn>((ev, _) => { Reset(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) => { Reset(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        Core.RegisterEventHandler<EventRoundStart>((_, _) => { ResetAll(); return HookResult.Continue; });
    }

    public override void OnUnload()
    {
        ResetAll();

        if (!_glowUser)
            return;

        _glowUser = false;
        GlowPool.Release();
    }

    private void Reset(int slot)
    {
        if (slot < 0 || slot >= 64)
            return;

        _active[slot] = null;
        _lastBoost[slot] = 0f;
        _strengthTick[slot] = 0;
        _strength[slot] = 1f;
        _speed[slot] = 1f;
    }

    private void ResetAll()
    {
        for (int slot = 0; slot < 64; slot++)
            Reset(slot);

        _slowed.Clear();
        _slowedThisTick.Clear();
    }

    private void OnTick()
    {
        float now = Server.CurrentTime;
        _slowedThisTick.Clear();

        bool anyGlow = false;

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
                Start(player, now);

            _lastBoost[slot] = boost;

            var state = _active[slot];
            if (state == null)
                continue;

            if (now >= state.ExpireAt || !IsAlive(player) || !Active(player))
            {
                Stop(player, slot);
                continue;
            }

            if (state.Mode == "wallhack" || state.Mode == "radarhack")
                anyGlow = true;

            Apply(player, pawn, state, now);
        }

        if (anyGlow && _glowUser)
            GlowPool.Build();

        RestoreStaleSlows();
    }

    private void Start(CCSPlayerController player, float now)
    {
        int slot = player.Slot;
        string mode = Setting(player);
        var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;

        var (time, limit) = mode switch
        {
            "poison" when cfg.Poison != null => (cfg.Poison.Time, cfg.Poison.Limit),
            "heal" when cfg.Heal != null => (cfg.Heal.Time, cfg.Heal.Limit),
            "slow" when cfg.Slow != null => (cfg.Slow.Time, cfg.Slow.Limit),
            "speed" when cfg.Speed != null => (cfg.Speed.Time, cfg.Speed.Limit),
            "strength" when cfg.Strength != null => (cfg.Strength.Time, cfg.Strength.Limit),
            "wallhack" when cfg.Wallhack != null => (cfg.Wallhack.Time, cfg.Wallhack.Limit),
            "radarhack" when cfg.Radarhack != null => (cfg.Radarhack.Time, cfg.Radarhack.Limit),
            "magnetic" when cfg.Magnetic != null => (cfg.Magnetic.Time, cfg.Magnetic.Limit),
            _ => (0f, 0)
        };

        if (time <= 0f || LimitReached(slot, limit))
            return;

        LimitUse(slot);
        _active[slot] = new Running { Mode = mode, ExpireAt = now + time, NextTick = now };
    }

    private void Stop(CCSPlayerController player, int slot)
    {
        _active[slot] = null;
        _strength[slot] = 1f;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        if (_speed[slot] > 1f && Math.Abs(pawn.VelocityModifier - 1f) > 0.001f)
        {
            pawn.VelocityModifier = 1f;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
        }

        _speed[slot] = 1f;
    }

    private void Apply(CCSPlayerController player, CCSPlayerPawn pawn, Running state, float now)
    {
        var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;
        int slot = player.Slot;

        switch (state.Mode)
        {
            case "speed" when cfg.Speed != null:
                _speed[slot] = Math.Max(cfg.Speed.SpeedMultiplier, 1f);
                if (pawn.VelocityModifier < _speed[slot] - 0.001f)
                {
                    pawn.VelocityModifier = _speed[slot];
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                }
                return;

            case "strength" when cfg.Strength != null:
                MarkStrength(player, pawn, cfg.Strength);
                return;

            case "wallhack" when cfg.Wallhack != null:
                Wallhack(player, pawn, cfg.Wallhack, state, now);
                return;

            case "radarhack" when cfg.Radarhack != null:
                Radarhack(player, pawn, cfg.Radarhack, state, now);
                return;

            case "magnetic" when cfg.Magnetic != null:
                Magnetic(player, pawn, cfg.Magnetic);
                return;
        }

        if (state.NextTick > now)
            return;

        switch (state.Mode)
        {
            case "poison" when cfg.Poison != null:
                state.NextTick = now + Math.Max(cfg.Poison.Tick, 0.05f);
                Around(player, pawn, cfg.Poison.Radius, cfg.Poison.IgnoreTeammates, cfg.Poison.IgnoreSelf, false, target =>
                {
                    if (target.Health <= cfg.Poison.MinHp)
                        return;
                    target.Health = Math.Max(target.Health - cfg.Poison.Damage, cfg.Poison.MinHp);
                    Utilities.SetStateChanged(target, "CBaseEntity", "m_iHealth");
                    SoundUtil.EmitToPawn(target, "Player.DamageBody.Victim", cfg.Poison.SoundVolume);
                });
                break;

            case "heal" when cfg.Heal != null:
                state.NextTick = now + Math.Max(cfg.Heal.Tick, 0.05f);
                Around(player, pawn, cfg.Heal.Radius, cfg.Heal.IgnoreTeammates, cfg.Heal.IgnoreSelf, cfg.Heal.IgnoreEnemy, target =>
                {
                    int maxHp = target.MaxHealth > 0 ? target.MaxHealth : 100;
                    if (target.Health >= maxHp)
                        return;
                    target.Health = Math.Min(target.Health + cfg.Heal.Heal, maxHp);
                    Utilities.SetStateChanged(target, "CBaseEntity", "m_iHealth");
                    SoundUtil.EmitToPawn(target, "Healthshot.Success", cfg.Heal.SoundVolume);
                });
                break;

            case "slow" when cfg.Slow != null:
                state.NextTick = now + 0.1f;
                float factor = Math.Max(1f - cfg.Slow.Percent / 100f, 0f);
                if (cfg.Slow.MinSpeed > 0)
                    factor = Math.Max(factor, cfg.Slow.MinSpeed / 250f);
                Around(player, pawn, cfg.Slow.Radius, cfg.Slow.IgnoreTeammates, cfg.Slow.IgnoreSelf, cfg.Slow.IgnoreEnemy, target =>
                {
                    var controller = target.Controller.Value?.As<CCSPlayerController>();
                    if (controller != null && controller.IsValid)
                        _slowedThisTick.Add(controller.Slot);

                    target.VelocityModifier = factor;
                    Utilities.SetStateChanged(target, "CCSPlayerPawn", "m_flVelocityModifier");
                });
                break;
        }
    }

    private void MarkStrength(CCSPlayerController player, CCSPlayerPawn pawn, StrengthCfg cfg)
    {
        _strengthTick[player.Slot] = Server.TickCount;
        _strength[player.Slot] = cfg.DamageMultiplier;

        if (cfg.Radius <= 0f)
            return;

        Around(player, pawn, cfg.Radius, cfg.IgnoreTeammates, cfg.IgnoreSelf, cfg.IgnoreEnemy, target =>
        {
            var controller = target.Controller.Value?.As<CCSPlayerController>();
            if (controller == null || !controller.IsValid || controller.Slot >= 64)
                return;

            _strengthTick[controller.Slot] = Server.TickCount;
            _strength[controller.Slot] = cfg.DamageMultiplier;
        });
    }

    private void Magnetic(CCSPlayerController player, CCSPlayerPawn pawn, MagneticCfg cfg)
    {
        if (cfg.Radius <= 0f || cfg.Strength <= 0f || pawn.AbsOrigin == null)
            return;

        var center = pawn.AbsOrigin;

        Around(player, pawn, cfg.Radius, cfg.IgnoreTeammates, cfg.IgnoreSelf, cfg.IgnoreEnemy, target =>
        {
            if (target.AbsOrigin == null)
                return;

            float distance = TrailBeam.Distance(center, target.AbsOrigin);
            if (distance <= 10f)
                return;

            float dx = center.X - target.AbsOrigin.X;
            float dy = center.Y - target.AbsOrigin.Y;
            float length = MathF.Sqrt(dx * dx + dy * dy);
            if (length < 1f)
                return;

            float pull = cfg.Strength * (1f - distance / cfg.Radius);
            var velocity = target.AbsVelocity;
            target.Teleport(null, null, new Vector(velocity.X + dx / length * pull, velocity.Y + dy / length * pull, velocity.Z));
        });
    }

    private void Wallhack(CCSPlayerController player, CCSPlayerPawn pawn, WallhackCfg cfg, Running state, float now)
    {
        if (state.NextTick <= now)
        {
            state.NextTick = now + Math.Max(cfg.Tick, 0.05f);
            state.Seen = Scan(player, pawn, cfg.Radius, cfg.SeeTeammates, cfg.OnlyMode);
        }

        if (state.Seen == 0)
            return;

        var color = TrailBeam.Resolve(cfg.Color);
        for (int target = 0; target < 64; target++)
            if ((state.Seen & (1UL << target)) != 0)
                GlowPool.Show(player.Slot, target, color);
    }

    private void Radarhack(CCSPlayerController player, CCSPlayerPawn pawn, RadarhackCfg cfg, Running state, float now)
    {
        if (state.NextTick <= now)
        {
            state.NextTick = now + Math.Max(cfg.Tick, 0.05f);
            state.Seen = Scan(player, pawn, cfg.Radius, cfg.SeeTeammates, cfg.OnlyMode);
        }

        if (state.Seen == 0)
            return;

        int slot = player.Slot;
        for (int index = 0; index < 64; index++)
        {
            if ((state.Seen & (1UL << index)) == 0)
                continue;

            var targetPawn = Utilities.GetPlayerFromSlot(index)?.PlayerPawn.Value;
            if (targetPawn != null && targetPawn.IsValid)
                targetPawn.EntitySpottedState.SpottedByMask[slot / 32] |= 1u << (slot % 32);
        }
    }

    private ulong Scan(CCSPlayerController player, CCSPlayerPawn pawn, float radius, bool seeTeammates, int onlyMode)
    {
        ulong seen = 0;
        var center = pawn.AbsOrigin;

        foreach (var target in Core.Players)
        {
            if (target == null || !target.IsValid || target.Slot >= 64 || target.Slot == player.Slot || !IsAlive(target))
                continue;
            if (!seeTeammates && target.Team == player.Team)
                continue;

            var targetPawn = target.PlayerPawn.Value;
            if (targetPawn == null || !targetPawn.IsValid || targetPawn.AbsOrigin == null)
                continue;

            if (radius > 0f && (center == null || TrailBeam.Distance(center, targetPawn.AbsOrigin) > radius))
                continue;
            if (!ActivityFilter.Matches(onlyMode, target, targetPawn))
                continue;

            seen |= 1UL << target.Slot;
        }

        return seen;
    }

    private void Around(CCSPlayerController owner, CCSPlayerPawn ownerPawn, float radius, bool ignoreTeammates, bool ignoreSelf, bool ignoreEnemy, Action<CCSPlayerPawn> effect)
    {
        var center = ownerPawn.AbsOrigin;
        if (center == null)
            return;

        if (radius <= 0f)
            radius = 180f;

        foreach (var target in Core.Players)
        {
            if (target == null || !target.IsValid || !IsAlive(target))
                continue;

            bool isSelf = target.Slot == owner.Slot;
            bool isTeammate = !isSelf && target.Team == owner.Team;
            bool isEnemy = !isSelf && !isTeammate;

            if (isSelf && ignoreSelf) continue;
            if (isTeammate && ignoreTeammates) continue;
            if (isEnemy && ignoreEnemy) continue;

            var pawn = target.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
                continue;

            if (TrailBeam.Distance(center, pawn.AbsOrigin) > radius)
                continue;

            effect(pawn);
        }
    }

    private void RestoreStaleSlows()
    {
        if (_slowed.Count > 0)
        {
            foreach (int slot in _slowed)
            {
                if (_slowedThisTick.Contains(slot))
                    continue;

                var pawn = Utilities.GetPlayerFromSlot(slot)?.PlayerPawn.Value;
                if (pawn != null && pawn.IsValid && Math.Abs(pawn.VelocityModifier - 1f) > 0.001f)
                {
                    pawn.VelocityModifier = 1f;
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }

            _slowed.Clear();
        }

        foreach (int slot in _slowedThisTick)
            _slowed.Add(slot);
    }

    private HookResult OnDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        if (info.Attacker?.Value == null)
            return HookResult.Continue;

        var attacker = PawnController(info.Attacker.Value);
        if (attacker == null || attacker.Slot >= 64)
            return HookResult.Continue;

        if (Server.TickCount - _strengthTick[attacker.Slot] > 1)
            return HookResult.Continue;

        float scale = _strength[attacker.Slot];
        if (Math.Abs(scale - 1f) < 0.001f)
            return HookResult.Continue;

        var victim = PawnController(entity);
        if (victim != null && victim.Slot == attacker.Slot)
            return HookResult.Continue;

        info.Damage = MathF.Max(info.Damage * scale, 0f);
        return HookResult.Changed;
    }
}
