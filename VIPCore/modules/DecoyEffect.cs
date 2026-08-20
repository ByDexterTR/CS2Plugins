using System.Drawing;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class DecoyEffect : VipModule
{
    private class PoisonCfg
    {
        [JsonPropertyName("minhp")]
        public int MinHp { get; set; } = 1;
        public int Damage { get; set; } = 2;
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
        public float Tick { get; set; } = 0.1f;
        public float Radius { get; set; } = 180f;
        public bool IgnoreTeammates { get; set; } = true;
        public bool IgnoreSelf { get; set; } = true;
        public bool IgnoreEnemy { get; set; } = false;
        public int Limit { get; set; } = 0;
    }

    private class MagneticCfg
    {
        public float Strength { get; set; } = 30f;
        public float Radius { get; set; } = 180f;
        public bool IgnoreTeammates { get; set; } = true;
        public bool IgnoreSelf { get; set; } = true;
        public bool IgnoreEnemy { get; set; } = false;
        public int Limit { get; set; } = 0;
    }

    private class WallhackCfg
    {
        public float Tick { get; set; } = 0.25f;
        public string Color { get; set; } = GlowPool.DefaultColor;
        public float Radius { get; set; } = 180f;
        public bool SeeTeammates { get; set; }
        public int Limit { get; set; } = 0;
    }

    private class Cfg
    {
        public PoisonCfg? Poison { get; set; }
        public HealCfg? Heal { get; set; }
        public SlowCfg? Slow { get; set; }
        public WallhackCfg? Wallhack { get; set; }
        public MagneticCfg? Magnetic { get; set; }
    }

    private class ActiveDecoy
    {
        public required Vector Pos;
        public int OwnerSlot;
        public string Mode = "";
        public float NextTick;
        public ulong Seen;
    }

    private readonly Dictionary<int, ActiveDecoy> _decoys = new();
    private readonly HashSet<int> _slowed = new();
    private readonly HashSet<int> _slowedThisTick = new();
    private bool _glowUser;

    public override string Name => "DecoyEffect";
    public override string DisplayName => Core.Localizer["vip.module.decoyeffect"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player)
    {
        var cfg = GroupValue<Cfg>(player) ?? new Cfg();
        var options = new List<VipFeatureOption>();
        if (cfg.Poison != null)
            options.Add(new VipFeatureOption(Core.Localizer["vip.decoy.poison"], "poison"));
        if (cfg.Heal != null)
            options.Add(new VipFeatureOption(Core.Localizer["vip.decoy.heal"], "heal"));
        if (cfg.Slow != null)
            options.Add(new VipFeatureOption(Core.Localizer["vip.decoy.slow"], "slow"));
        if (cfg.Wallhack != null)
            options.Add(new VipFeatureOption(Core.Localizer["vip.decoy.wallhack"], "wallhack"));
        if (cfg.Magnetic != null)
            options.Add(new VipFeatureOption(Core.Localizer["vip.decoy.magnetic"], "magnetic"));
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

        Core.RegisterListener<OnServerPrecacheResources>(m => m.AddResource(DecoyRing.DiscModel));
        Core.RegisterEventHandler<EventDecoyStarted>(OnStarted);
        Core.RegisterEventHandler<EventDecoyDetonate>((ev, _) => { Remove(ev.Entityid); return HookResult.Continue; });
        Core.RegisterEventHandler<EventRoundStart>((_, __) => { Clear(); return HookResult.Continue; });
        Core.RegisterListener<OnMapStart>(_ => Clear());
        Core.RegisterListener<OnTick>(OnTick);
    }

    public override void OnUnload()
    {
        Clear();

        if (!_glowUser)
            return;

        _glowUser = false;
        GlowPool.Release();
    }

    private void Clear()
    {
        foreach (int id in _decoys.Keys.ToList())
            DecoyRing.Hide(id);

        _decoys.Clear();
        _slowed.Clear();
        _slowedThisTick.Clear();
    }

    private void Remove(int entityId)
    {
        if (_decoys.Remove(entityId))
            DecoyRing.Hide(entityId);
    }

    private (float radius, int limit, Color color) ModeInfo(Cfg cfg, string mode) => mode switch
    {
        "poison" when cfg.Poison != null => (cfg.Poison.Radius, cfg.Poison.Limit, Color.FromArgb(255, 255, 0, 255)),
        "heal" when cfg.Heal != null => (cfg.Heal.Radius, cfg.Heal.Limit, Color.FromArgb(255, 0, 255, 0)),
        "slow" when cfg.Slow != null => (cfg.Slow.Radius, cfg.Slow.Limit, Color.FromArgb(255, 0, 128, 255)),
        "wallhack" when cfg.Wallhack != null => (cfg.Wallhack.Radius, cfg.Wallhack.Limit, TrailBeam.Resolve(cfg.Wallhack.Color)),
        "magnetic" when cfg.Magnetic != null => (cfg.Magnetic.Radius, cfg.Magnetic.Limit, Color.FromArgb(255, 255, 128, 0)),
        _ => (0f, 0, Color.White)
    };

    private HookResult OnStarted(EventDecoyStarted ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        string mode = Setting(player!);
        var cfg = GroupValue<Cfg>(player!) ?? new Cfg();
        var (radius, limit, color) = ModeInfo(cfg, mode);
        if (radius <= 0f)
            return HookResult.Continue;

        int slot = player!.Slot;
        if (LimitReached(slot, limit))
            return HookResult.Continue;

        LimitUse(slot);

        var pos = new Vector(ev.X, ev.Y, ev.Z);
        _decoys[ev.Entityid] = new ActiveDecoy
        {
            Pos = pos,
            OwnerSlot = slot,
            Mode = mode,
            NextTick = Server.CurrentTime
        };

        DecoyRing.Show(Core, ev.Entityid, pos, radius, color);
        return HookResult.Continue;
    }

    private void OnTick()
    {
        if (_decoys.Count == 0)
        {
            ResetStaleSlows();
            return;
        }

        _slowedThisTick.Clear();
        float now = Server.CurrentTime;

        if (_glowUser)
            GlowPool.Build();

        foreach (var decoy in _decoys.Values)
        {
            var owner = Utilities.GetPlayerFromSlot(decoy.OwnerSlot);
            if (!Active(owner))
                continue;

            var cfg = GroupValue<Cfg>(owner!) ?? new Cfg();

            if (decoy.Mode == "wallhack")
            {
                ApplyWallhack(decoy, cfg.Wallhack, owner!, now);
                continue;
            }

            if (decoy.NextTick > now)
                continue;

            switch (decoy.Mode)
            {
                case "magnetic" when cfg.Magnetic != null:
                    decoy.NextTick = now + 0.05f;
                    Pull(decoy, owner!, cfg.Magnetic);
                    break;

                case "poison" when cfg.Poison != null:
                    decoy.NextTick = now + Math.Max(cfg.Poison.Tick, 0.05f);
                    Apply(decoy, owner!, cfg.Poison.Radius, cfg.Poison.IgnoreTeammates, cfg.Poison.IgnoreSelf, false, pawn =>
                    {
                        if (pawn.Health <= cfg.Poison.MinHp)
                            return;
                        pawn.Health = Math.Max(pawn.Health - cfg.Poison.Damage, cfg.Poison.MinHp);
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                        SoundUtil.EmitToPawn(pawn, "Player.DamageBody.Victim", cfg.Poison.SoundVolume);
                    });
                    break;

                case "heal" when cfg.Heal != null:
                    decoy.NextTick = now + Math.Max(cfg.Heal.Tick, 0.05f);
                    Apply(decoy, owner!, cfg.Heal.Radius, cfg.Heal.IgnoreTeammates, cfg.Heal.IgnoreSelf, cfg.Heal.IgnoreEnemy, pawn =>
                    {
                        int maxHp = pawn.MaxHealth > 0 ? pawn.MaxHealth : 100;
                        if (pawn.Health >= maxHp)
                            return;
                        pawn.Health = Math.Min(pawn.Health + cfg.Heal.Heal, maxHp);
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                        SoundUtil.EmitToPawn(pawn, "Healthshot.Success", cfg.Heal.SoundVolume);
                    });
                    break;

                case "slow" when cfg.Slow != null:
                    decoy.NextTick = now + Math.Max(cfg.Slow.Tick, 0.05f);
                    float factor = Math.Max(1f - cfg.Slow.Percent / 100f, 0f);
                    if (cfg.Slow.MinSpeed > 0)
                        factor = Math.Max(factor, cfg.Slow.MinSpeed / 250f);
                    Apply(decoy, owner!, cfg.Slow.Radius, cfg.Slow.IgnoreTeammates, cfg.Slow.IgnoreSelf, cfg.Slow.IgnoreEnemy, pawn =>
                    {
                        var controller = pawn.Controller.Value?.As<CCSPlayerController>();
                        if (controller != null && controller.IsValid)
                            _slowedThisTick.Add(controller.Slot);

                        pawn.VelocityModifier = factor;
                        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                    });
                    break;
            }
        }

        ResetStaleSlows();
    }

    private void ApplyWallhack(ActiveDecoy decoy, WallhackCfg? cfg, CCSPlayerController owner, float now)
    {
        if (cfg == null || !IsAlive(owner))
            return;

        if (decoy.NextTick <= now)
        {
            decoy.NextTick = now + Math.Max(cfg.Tick, 0.05f);
            decoy.Seen = 0;

            float radius = cfg.Radius > 0f ? cfg.Radius : 180f;
            foreach (var target in Core.Players)
            {
                if (target == null || !target.IsValid || target.Slot >= 64 || target.Slot == decoy.OwnerSlot || !IsAlive(target))
                    continue;
                if (!cfg.SeeTeammates && target.Team == owner.Team)
                    continue;

                var pawn = target.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
                    continue;

                if (TrailBeam.Distance(decoy.Pos, pawn.AbsOrigin) > radius)
                    continue;

                decoy.Seen |= 1UL << target.Slot;
            }
        }

        if (decoy.Seen == 0)
            return;

        var color = TrailBeam.Resolve(cfg.Color);

        for (int target = 0; target < 64; target++)
            if ((decoy.Seen & (1UL << target)) != 0)
                GlowPool.Show(decoy.OwnerSlot, target, color);
    }

    private void Pull(ActiveDecoy decoy, CCSPlayerController owner, MagneticCfg cfg)
    {
        if (cfg.Radius <= 0f || cfg.Strength <= 0f)
            return;

        Apply(decoy, owner, cfg.Radius, cfg.IgnoreTeammates, cfg.IgnoreSelf, cfg.IgnoreEnemy, pawn =>
        {
            if (pawn.AbsOrigin == null)
                return;

            float distance = TrailBeam.Distance(decoy.Pos, pawn.AbsOrigin);
            if (distance <= 10f)
                return;

            float dx = decoy.Pos.X - pawn.AbsOrigin.X;
            float dy = decoy.Pos.Y - pawn.AbsOrigin.Y;
            float length = MathF.Sqrt(dx * dx + dy * dy);
            if (length < 1f)
                return;

            float pull = cfg.Strength * (1f - distance / cfg.Radius);
            var velocity = pawn.AbsVelocity;
            pawn.Teleport(null, null, new Vector(velocity.X + dx / length * pull, velocity.Y + dy / length * pull, velocity.Z));
        });
    }

    private void ResetStaleSlows()
    {
        if (_slowed.Count == 0)
        {
            foreach (int slot in _slowedThisTick)
                _slowed.Add(slot);
            return;
        }

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
        foreach (int slot in _slowedThisTick)
            _slowed.Add(slot);
    }

    private void Apply(ActiveDecoy decoy, CCSPlayerController owner, float radius, bool ignoreTeammates, bool ignoreSelf, bool ignoreEnemy, Action<CCSPlayerPawn> effect)
    {
        if (radius <= 0f)
            radius = 180f;

        foreach (var target in Core.Players)
        {
            if (target == null || !target.IsValid || !IsAlive(target))
                continue;

            bool isSelf = target.Slot == decoy.OwnerSlot;
            bool isTeammate = !isSelf && target.Team == owner.Team;
            bool isEnemy = !isSelf && !isTeammate;

            if (isSelf && ignoreSelf)
                continue;
            if (isTeammate && ignoreTeammates)
                continue;
            if (isEnemy && ignoreEnemy)
                continue;

            var pawn = target.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
                continue;

            if (TrailBeam.Distance(decoy.Pos, pawn.AbsOrigin) > radius)
                continue;

            effect(pawn);
        }
    }
}
