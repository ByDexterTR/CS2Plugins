using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class SmokeEffect : VipModule
{
    private class PoisonCfg
    {
        [JsonPropertyName("minhp")]
        public int MinHp { get; set; } = 1;
        public int Damage { get; set; } = 2;
        public float Tick { get; set; } = 0.5f;
        [JsonPropertyName("smokecolor")]
        public List<int> SmokeColor { get; set; } = new() { 255, 0, 255 };
        public float Radius { get; set; } = 180f;
        public bool IgnoreTeammates { get; set; } = true;
        public bool IgnoreSelf { get; set; } = true;
        public int Limit { get; set; } = 0;
    }

    private class HealCfg
    {
        [JsonPropertyName("maxhp")]
        public int MaxHp { get; set; } = 100;
        public int Heal { get; set; } = 2;
        public float Tick { get; set; } = 0.5f;
        [JsonPropertyName("smokecolor")]
        public List<int> SmokeColor { get; set; } = new() { 0, 255, 0 };
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
        [JsonPropertyName("smokecolor")]
        public List<int> SmokeColor { get; set; } = new() { 0, 0, 255 };
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
    }

    private class ActiveSmoke
    {
        public Vector Pos = null!;
        public int OwnerSlot;
        public string Mode = "";
        public float NextTick;
    }

    private readonly List<ActiveSmoke> _smokes = new();
    private readonly int[] _used = new int[64];
    private readonly int[] _armed = new int[64];

    public override string Name => "SmokeEffect";
    public override string DisplayName => Core.Localizer["vip.module.smokeeffect"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player)
    {
        var cfg = GroupValue<Cfg>(player) ?? new Cfg();
        var options = new List<VipFeatureOption>();
        if (cfg.Poison != null)
            options.Add(new VipFeatureOption(Core.Localizer["vip.smoke.poison"], "poison"));
        if (cfg.Heal != null)
            options.Add(new VipFeatureOption(Core.Localizer["vip.smoke.heal"], "heal"));
        if (cfg.Slow != null)
            options.Add(new VipFeatureOption(Core.Localizer["vip.smoke.slow"], "slow"));
        return options;
    }

    public override void OnLoad()
    {
        Core.RegisterListener<OnEntitySpawned>(OnEntitySpawned);
        Core.RegisterEventHandler<EventSmokegrenadeDetonate>(OnDetonate);
        Core.RegisterEventHandler<EventSmokegrenadeExpired>(OnExpired);
        Core.RegisterEventHandler<EventRoundStart>((_, __) =>
        {
            _smokes.Clear();
            Array.Clear(_used);
            Array.Clear(_armed);
            return HookResult.Continue;
        });
        Core.RegisterListener<OnTick>(OnTick);
    }

    private (List<int>? color, int limit) ModeInfo(CCSPlayerController player, string mode)
    {
        var cfg = GroupValue<Cfg>(player) ?? new Cfg();
        return mode switch
        {
            "poison" when cfg.Poison != null => (cfg.Poison.SmokeColor, cfg.Poison.Limit),
            "heal" when cfg.Heal != null => (cfg.Heal.SmokeColor, cfg.Heal.Limit),
            "slow" when cfg.Slow != null => (cfg.Slow.SmokeColor, cfg.Slow.Limit),
            _ => (null, 0)
        };
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        if (entity == null || !entity.IsValid || entity.DesignerName != "smokegrenade_projectile")
            return;

        var smoke = entity.As<CSmokeGrenadeProjectile>();
        Server.NextFrame(() =>
        {
            if (smoke == null || !smoke.IsValid)
                return;

            var player = smoke.Thrower.Value?.Controller.Value?.As<CCSPlayerController>();
            if (!Active(player))
                return;

            string mode = Setting(player!);
            var (color, limit) = ModeInfo(player!, mode);
            if (color == null)
                return;

            int slot = player!.Slot;
            if (limit > 0 && _used[slot] >= limit)
                return;

            _used[slot]++;
            _armed[slot]++;

            if (color.Count != 3)
                return;

            smoke.SmokeColor.X = color[0];
            smoke.SmokeColor.Y = color[1];
            smoke.SmokeColor.Z = color[2];
        });
    }

    private HookResult OnDetonate(EventSmokegrenadeDetonate ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        int slot = player!.Slot;
        if (_armed[slot] <= 0)
            return HookResult.Continue;

        string mode = Setting(player);
        if (ModeInfo(player, mode).color == null)
            return HookResult.Continue;

        _armed[slot]--;
        _smokes.Add(new ActiveSmoke
        {
            Pos = new Vector(ev.X, ev.Y, ev.Z),
            OwnerSlot = slot,
            Mode = mode,
            NextTick = Server.CurrentTime
        });

        return HookResult.Continue;
    }

    private HookResult OnExpired(EventSmokegrenadeExpired ev, GameEventInfo info)
    {
        _smokes.RemoveAll(s => s.Pos.X == ev.X && s.Pos.Y == ev.Y && s.Pos.Z == ev.Z);
        return HookResult.Continue;
    }

    private void OnTick()
    {
        if (_smokes.Count == 0)
            return;

        float now = Server.CurrentTime;

        foreach (var smoke in _smokes)
        {
            if (smoke.NextTick > now)
                continue;

            var owner = Utilities.GetPlayerFromSlot(smoke.OwnerSlot);
            if (!Active(owner))
                continue;

            var cfg = GroupValue<Cfg>(owner!) ?? new Cfg();

            switch (smoke.Mode)
            {
                case "poison" when cfg.Poison != null:
                    smoke.NextTick = now + Math.Max(cfg.Poison.Tick, 0.05f);
                    Apply(smoke, owner!, cfg.Poison.Radius, cfg.Poison.IgnoreTeammates, cfg.Poison.IgnoreSelf, false, pawn =>
                    {
                        if (pawn.Health <= cfg.Poison.MinHp)
                            return;
                        pawn.Health = Math.Max(pawn.Health - cfg.Poison.Damage, cfg.Poison.MinHp);
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                        pawn.Controller.Value?.As<CCSPlayerController>()?.EmitSound("Player.DamageBody.Onlooker");
                    });
                    break;

                case "heal" when cfg.Heal != null:
                    smoke.NextTick = now + Math.Max(cfg.Heal.Tick, 0.05f);
                    Apply(smoke, owner!, cfg.Heal.Radius, cfg.Heal.IgnoreTeammates, cfg.Heal.IgnoreSelf, cfg.Heal.IgnoreEnemy, pawn =>
                    {
                        if (pawn.Health >= cfg.Heal.MaxHp)
                            return;
                        pawn.Health = Math.Min(pawn.Health + cfg.Heal.Heal, cfg.Heal.MaxHp);
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                        pawn.Controller.Value?.As<CCSPlayerController>()?.EmitSound("Healthshot.Success");
                    });
                    break;

                case "slow" when cfg.Slow != null:
                    smoke.NextTick = now + 0.1f;
                    float factor = Math.Max(1f - cfg.Slow.Percent / 100f, 0f);
                    if (cfg.Slow.MinSpeed > 0)
                        factor = Math.Max(factor, cfg.Slow.MinSpeed / 250f);
                    Apply(smoke, owner!, cfg.Slow.Radius, cfg.Slow.IgnoreTeammates, cfg.Slow.IgnoreSelf, cfg.Slow.IgnoreEnemy, pawn =>
                    {
                        pawn.VelocityModifier = factor;
                        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                    });
                    break;
            }
        }
    }

    private void Apply(ActiveSmoke smoke, CCSPlayerController owner, float radius, bool ignoreTeammates, bool ignoreSelf, bool ignoreEnemy, Action<CCSPlayerPawn> effect)
    {
        if (radius <= 0)
            radius = 180f;
        foreach (var target in Utilities.GetPlayers())
        {
            if (target == null || !target.IsValid || !IsAlive(target))
                continue;

            bool isSelf = target.Slot == smoke.OwnerSlot;
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

            if (TrailBeam.Distance(smoke.Pos, pawn.AbsOrigin) > radius)
                continue;

            effect(pawn);
        }
    }
}
