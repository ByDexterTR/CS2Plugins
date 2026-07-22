using System.Text.Json.Serialization;
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class Aura : VipModule
{
    private class HealCfg
    {
        public int Heal { get; set; } = 2;
        public float Tick { get; set; } = 0.5f;
        public float Radius { get; set; } = 180f;
        [JsonPropertyName("beamcolor")]
        public string BeamColor { get; set; } = "0 255 0";
        public float DurationOn { get; set; } = 1f;
        public float DurationOff { get; set; } = 0f;
        public bool IgnoreTeammates { get; set; } = false;
        public bool IgnoreSelf { get; set; } = false;
        public bool IgnoreEnemy { get; set; } = true;
    }

    private class PoisonCfg
    {
        public int Damage { get; set; } = 2;
        public float Tick { get; set; } = 0.5f;
        public float Radius { get; set; } = 180f;
        [JsonPropertyName("beamcolor")]
        public string BeamColor { get; set; } = "255 0 255";
        public float DurationOn { get; set; } = 1f;
        public float DurationOff { get; set; } = 0f;
        public bool IgnoreTeammates { get; set; } = true;
        public bool IgnoreSelf { get; set; } = true;
        public bool IgnoreEnemy { get; set; } = false;
    }

    private class SlowCfg
    {
        public int Percent { get; set; } = 30;
        [JsonPropertyName("minspeed")]
        public float MinSpeed { get; set; } = 100f;
        public float Radius { get; set; } = 180f;
        [JsonPropertyName("beamcolor")]
        public string BeamColor { get; set; } = "0 0 255";
        public float DurationOn { get; set; } = 1f;
        public float DurationOff { get; set; } = 0f;
        public bool IgnoreTeammates { get; set; } = true;
        public bool IgnoreSelf { get; set; } = true;
        public bool IgnoreEnemy { get; set; } = false;
    }

    private class SpeedCfg
    {
        public int Percent { get; set; } = 30;
        [JsonPropertyName("maxspeed")]
        public float MaxSpeed { get; set; } = 400f;
        public float Radius { get; set; } = 180f;
        [JsonPropertyName("beamcolor")]
        public string BeamColor { get; set; } = "255 255 0";
        public float DurationOn { get; set; } = 1f;
        public float DurationOff { get; set; } = 0f;
        public bool IgnoreTeammates { get; set; } = false;
        public bool IgnoreSelf { get; set; } = false;
        public bool IgnoreEnemy { get; set; } = true;
    }

    private class Cfg
    {
        public HealCfg? Heal { get; set; }
        public PoisonCfg? Poison { get; set; }
        public SlowCfg? Slow { get; set; }
        public SpeedCfg? Speed { get; set; }
    }

    private const int Segments = 24;

    private readonly Dictionary<int, List<CBeam>> _rings = new();
    private readonly float[] _nextEffectTick = new float[64];
    private readonly string?[] _colorStr = new string?[64];
    private readonly Color[] _colorVal = new Color[64];
    private readonly HashSet<int> _affected = new();
    private readonly HashSet<int> _affectedThisTick = new();

    public override string Name => "Aura";
    public override string DisplayName => Core.Localizer["vip.module.aura"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player)
    {
        var cfg = GroupValue<Cfg>(player) ?? new Cfg();
        var options = new List<VipFeatureOption>();
        if (cfg.Heal != null) options.Add(new VipFeatureOption(Core.Localizer["vip.aura.heal"], "heal"));
        if (cfg.Poison != null) options.Add(new VipFeatureOption(Core.Localizer["vip.aura.poison"], "poison"));
        if (cfg.Slow != null) options.Add(new VipFeatureOption(Core.Localizer["vip.aura.slow"], "slow"));
        if (cfg.Speed != null) options.Add(new VipFeatureOption(Core.Localizer["vip.aura.speed"], "speed"));
        return options;
    }

    public override void OnLoad() => Core.RegisterListener<OnTick>(OnTick);

    public override void OnUnload()
    {
        foreach (var beams in _rings.Values)
            foreach (var beam in beams)
                if (beam != null && beam.IsValid)
                    beam.Remove();
        _rings.Clear();
    }

    private void OnTick()
    {
        _affectedThisTick.Clear();

        foreach (var player in Core.Players)
        {
            if (player == null || !player.IsValid || player.IsBot)
                continue;

            int slot = player.Slot;

            if (!IsAlive(player) || !Active(player))
            {
                RemoveRing(slot);
                continue;
            }

            string mode = Setting(player);
            var cfg = GroupValue<Cfg>(player) ?? new Cfg();

            var (radius, color, durationOn, durationOff) = mode switch
            {
                "heal" when cfg.Heal != null => (cfg.Heal.Radius, cfg.Heal.BeamColor, cfg.Heal.DurationOn, cfg.Heal.DurationOff),
                "poison" when cfg.Poison != null => (cfg.Poison.Radius, cfg.Poison.BeamColor, cfg.Poison.DurationOn, cfg.Poison.DurationOff),
                "slow" when cfg.Slow != null => (cfg.Slow.Radius, cfg.Slow.BeamColor, cfg.Slow.DurationOn, cfg.Slow.DurationOff),
                "speed" when cfg.Speed != null => (cfg.Speed.Radius, cfg.Speed.BeamColor, cfg.Speed.DurationOn, cfg.Speed.DurationOff),
                _ => (0f, "", 0f, 0f)
            };

            if (radius <= 0)
            {
                RemoveRing(slot);
                continue;
            }

            bool ringVisible = true;
            if (durationOff > 0)
            {
                float on = Math.Max(durationOn, 1f);
                ringVisible = Server.CurrentTime % (on + durationOff) < on;
            }

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
                continue;

            if (ringVisible)
            {
                if (_colorStr[slot] != color || color.Length == 0 || color.Equals("rainbow", StringComparison.OrdinalIgnoreCase))
                {
                    _colorStr[slot] = color;
                    _colorVal[slot] = TrailBeam.Resolve(color);
                }
                UpdateRing(slot, pawn.AbsOrigin, radius, _colorVal[slot]);
            }
            else
                RemoveRing(slot);

            if (!ringVisible || Server.CurrentTime < _nextEffectTick[slot])
                continue;

            switch (mode)
            {
                case "heal" when cfg.Heal != null:
                    _nextEffectTick[slot] = Server.CurrentTime + Math.Max(cfg.Heal.Tick, 0.05f);
                    Apply(player, pawn.AbsOrigin, radius, cfg.Heal.IgnoreTeammates, cfg.Heal.IgnoreSelf, cfg.Heal.IgnoreEnemy, target =>
                    {
                        int maxHp = target.MaxHealth > 0 ? target.MaxHealth : 100;
                        if (target.Health >= maxHp)
                            return;
                        target.Health = Math.Min(target.Health + cfg.Heal.Heal, maxHp);
                        Utilities.SetStateChanged(target, "CBaseEntity", "m_iHealth");
                    });
                    break;

                case "poison" when cfg.Poison != null:
                    _nextEffectTick[slot] = Server.CurrentTime + Math.Max(cfg.Poison.Tick, 0.05f);
                    Apply(player, pawn.AbsOrigin, radius, cfg.Poison.IgnoreTeammates, cfg.Poison.IgnoreSelf, cfg.Poison.IgnoreEnemy, target =>
                    {
                        if (target.Health <= 1)
                            return;
                        target.Health = Math.Max(target.Health - cfg.Poison.Damage, 1);
                        Utilities.SetStateChanged(target, "CBaseEntity", "m_iHealth");
                    });
                    break;

                case "slow" when cfg.Slow != null:
                    Apply(player, pawn.AbsOrigin, radius, cfg.Slow.IgnoreTeammates, cfg.Slow.IgnoreSelf, cfg.Slow.IgnoreEnemy, target =>
                    {
                        float factor = Math.Max(1f - cfg.Slow.Percent / 100f, 0f);
                        if (cfg.Slow.MinSpeed > 0)
                            factor = Math.Max(factor, cfg.Slow.MinSpeed / 250f);
                        SetVelocity(target, factor);
                    });
                    break;

                case "speed" when cfg.Speed != null:
                    Apply(player, pawn.AbsOrigin, radius, cfg.Speed.IgnoreTeammates, cfg.Speed.IgnoreSelf, cfg.Speed.IgnoreEnemy, target =>
                    {
                        float factor = 1f + cfg.Speed.Percent / 100f;
                        if (cfg.Speed.MaxSpeed > 0)
                            factor = Math.Min(factor, cfg.Speed.MaxSpeed / 250f);
                        SetVelocity(target, factor);
                    });
                    break;
            }
        }

        foreach (int slot in _affected)
        {
            if (_affectedThisTick.Contains(slot))
                continue;

            var pawn = Utilities.GetPlayerFromSlot(slot)?.PlayerPawn.Value;
            if (pawn != null && pawn.IsValid && Math.Abs(pawn.VelocityModifier - 1f) > 0.001f)
            {
                pawn.VelocityModifier = 1f;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }

        _affected.Clear();
        foreach (int slot in _affectedThisTick)
            _affected.Add(slot);
    }

    private void SetVelocity(CCSPlayerPawn pawn, float factor)
    {
        var controller = pawn.Controller.Value?.As<CCSPlayerController>();
        if (controller != null && controller.IsValid)
            _affectedThisTick.Add(controller.Slot);

        pawn.VelocityModifier = factor;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
    }

    private void Apply(CCSPlayerController owner, Vector center, float radius, bool ignoreTeammates, bool ignoreSelf, bool ignoreEnemy, Action<CCSPlayerPawn> effect)
    {
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

    private void UpdateRing(int slot, Vector center, float radius, Color color)
    {
        if (!_rings.TryGetValue(slot, out var beams))
        {
            beams = new List<CBeam>();
            for (int i = 0; i < Segments; i++)
            {
                var created = Utilities.CreateEntityByName<CBeam>("beam");
                if (created == null || !created.IsValid)
                    continue;

                created.Width = 2f;
                created.Render = color;
                created.DispatchSpawn();
                beams.Add(created);
            }

            if (beams.Count == 0)
                return;

            _rings[slot] = beams;
        }

        float z = center.Z + 5f;
        double step = 2 * Math.PI / beams.Count;

        for (int i = 0; i < beams.Count; i++)
        {
            var beam = beams[i];
            if (beam == null || !beam.IsValid)
                continue;

            double a1 = step * i;
            double a2 = step * (i + 1);

            var start = new Vector(
                center.X + radius * (float)Math.Cos(a1),
                center.Y + radius * (float)Math.Sin(a1),
                z);

            if (beam.Render != color)
            {
                beam.Render = color;
                Utilities.SetStateChanged(beam, "CBaseModelEntity", "m_clrRender");
            }

            beam.Teleport(start, new QAngle(), new Vector());

            beam.EndPos.X = center.X + radius * (float)Math.Cos(a2);
            beam.EndPos.Y = center.Y + radius * (float)Math.Sin(a2);
            beam.EndPos.Z = z;
            Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");
        }
    }

    private void RemoveRing(int slot)
    {
        if (!_rings.Remove(slot, out var beams))
            return;

        foreach (var beam in beams)
            if (beam != null && beam.IsValid)
                beam.Remove();
    }
}
