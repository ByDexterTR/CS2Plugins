using System.Globalization;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class BulletEffect : VipModule
{
    private class PoisonCfg
    {
        public int Damage { get; set; } = 2;
        public float Tick { get; set; } = 0.5f;
        public float Duration { get; set; } = 3f;
        public bool IgnoreTeammates { get; set; } = true;
        public bool IgnoreSelf { get; set; } = true;
        public bool IgnoreEnemy { get; set; } = false;
    }

    private class SlowCfg
    {
        public int Percent { get; set; } = 20;
        public float Duration { get; set; } = 3f;
        public bool IgnoreTeammates { get; set; } = true;
        public bool IgnoreSelf { get; set; } = true;
        public bool IgnoreEnemy { get; set; } = false;
    }

    private class SizeCfg
    {
        public float Size { get; set; } = 1f;
        public float Duration { get; set; } = 5f;
        public bool IgnoreTeammates { get; set; } = true;
        public bool IgnoreSelf { get; set; } = true;
        public bool IgnoreEnemy { get; set; } = false;
    }

    private class Cfg
    {
        public PoisonCfg? Poison { get; set; }
        public SlowCfg? Slow { get; set; }
        public SizeCfg? Lower { get; set; }
        public SizeCfg? Upper { get; set; }
        public string OnlyWithWeapon { get; set; } = "";

        private List<string>? _allow;
        public List<string> Allow => _allow ??= WeaponUtil.ParseCsv(OnlyWithWeapon);
    }

    private class Effect
    {
        public string Mode = "";
        public float ExpireAt;
        public int Damage;
        public float Interval;
        public float NextTick;
        public float SlowFactor;
        public bool SizeApplied;
    }

    private readonly Effect?[] _active = new Effect?[64];

    public override string Name => "BulletEffect";
    public override string DisplayName => Core.Localizer["vip.module.bulleteffect"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player)
    {
        var cfg = GroupValue<Cfg>(player) ?? new Cfg();
        var options = new List<VipFeatureOption>();
        if (cfg.Poison != null) options.Add(new VipFeatureOption(Core.Localizer["vip.bullet.poison"], "poison"));
        if (cfg.Slow != null) options.Add(new VipFeatureOption(Core.Localizer["vip.bullet.slow"], "slow"));
        if (cfg.Lower != null) options.Add(new VipFeatureOption(Core.Localizer["vip.bullet.lower"], "lower"));
        if (cfg.Upper != null) options.Add(new VipFeatureOption(Core.Localizer["vip.bullet.upper"], "upper"));
        return options;
    }

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerHurt>(OnHurt);
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) =>
        {
            int slot = ev.Userid?.Slot ?? -1;
            if (slot >= 0 && slot < 64)
                _active[slot] = null;
            return HookResult.Continue;
        });
        Core.RegisterEventHandler<EventRoundStart>((_, __) => { ResetAll(); return HookResult.Continue; });
        Core.RegisterListener<OnMapStart>(_ => ResetAll());
        Core.RegisterListener<OnTick>(OnTick);
    }

    public override void OnUnload() => ResetAll();

    private void ResetAll()
    {
        for (int slot = 0; slot < 64; slot++)
        {
            var e = _active[slot];
            if (e != null)
                EndEffect(slot, e);
        }
    }

    private HookResult OnHurt(EventPlayerHurt ev, GameEventInfo info)
    {
        var attacker = ev.Attacker;
        if (!Active(attacker))
            return HookResult.Continue;

        var victim = ev.Userid;
        int slot = victim?.Slot ?? -1;
        if (slot < 0 || slot >= 64)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(attacker!) ?? new Cfg();

        var allow = cfg.Allow;
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(attacker!)))
            return HookResult.Continue;

        string mode = Setting(attacker!);

        bool isSelf = victim!.Slot == attacker!.Slot;
        bool isTeammate = !isSelf && victim.Team == attacker.Team;
        bool isEnemy = !isSelf && !isTeammate;

        float now = Server.CurrentTime;

        switch (mode)
        {
            case "poison" when cfg.Poison != null:
                if (Ignored(cfg.Poison.IgnoreTeammates, cfg.Poison.IgnoreSelf, cfg.Poison.IgnoreEnemy, isSelf, isTeammate, isEnemy))
                    return HookResult.Continue;
                float interval = Math.Max(cfg.Poison.Tick, 0.1f);
                float pDur = Math.Max(cfg.Poison.Duration, interval);
                var pe = _active[slot];
                if (pe != null && pe.Mode == "poison" && now < pe.ExpireAt)
                {
                    pe.ExpireAt += pDur;
                    pe.Damage = cfg.Poison.Damage;
                    pe.Interval = interval;
                }
                else
                {
                    _active[slot] = new Effect
                    {
                        Mode = "poison",
                        Damage = cfg.Poison.Damage,
                        Interval = interval,
                        NextTick = now + interval,
                        ExpireAt = now + pDur
                    };
                }
                break;

            case "slow" when cfg.Slow != null:
                if (Ignored(cfg.Slow.IgnoreTeammates, cfg.Slow.IgnoreSelf, cfg.Slow.IgnoreEnemy, isSelf, isTeammate, isEnemy))
                    return HookResult.Continue;
                float sDur = Math.Max(cfg.Slow.Duration, 0.1f);
                float factor = Math.Max(1f - cfg.Slow.Percent / 100f, 0.05f);
                var se = _active[slot];
                if (se != null && se.Mode == "slow" && now < se.ExpireAt)
                {
                    se.ExpireAt += sDur;
                    se.SlowFactor = factor;
                }
                else
                {
                    _active[slot] = new Effect
                    {
                        Mode = "slow",
                        SlowFactor = factor,
                        ExpireAt = now + sDur
                    };
                }
                break;

            case "lower" when cfg.Lower != null:
                ArmSize(victim, slot, "lower", cfg.Lower, isSelf, isTeammate, isEnemy, now);
                break;

            case "upper" when cfg.Upper != null:
                ArmSize(victim, slot, "upper", cfg.Upper, isSelf, isTeammate, isEnemy, now);
                break;
        }

        return HookResult.Continue;
    }

    private void ArmSize(CCSPlayerController victim, int slot, string mode, SizeCfg cfg, bool isSelf, bool isTeammate, bool isEnemy, float now)
    {
        if (Ignored(cfg.IgnoreTeammates, cfg.IgnoreSelf, cfg.IgnoreEnemy, isSelf, isTeammate, isEnemy))
            return;
        if (cfg.Size <= 0f)
            return;

        float dur = Math.Max(cfg.Duration, 0.1f);
        var existing = _active[slot];
        if (existing != null && existing.Mode == mode && existing.SizeApplied && now < existing.ExpireAt)
        {
            existing.ExpireAt += dur;
            return;
        }

        if (existing != null && existing.SizeApplied && existing.Mode != mode)
            EndEffect(slot, existing);

        ApplyScale(victim.PlayerPawn.Value, cfg.Size);
        _active[slot] = new Effect
        {
            Mode = mode,
            SizeApplied = true,
            ExpireAt = now + dur
        };
    }

    private static bool Ignored(bool ignoreTeammates, bool ignoreSelf, bool ignoreEnemy, bool isSelf, bool isTeammate, bool isEnemy) =>
        (isSelf && ignoreSelf) || (isTeammate && ignoreTeammates) || (isEnemy && ignoreEnemy);

    private void OnTick()
    {
        float now = Server.CurrentTime;
        for (int slot = 0; slot < 64; slot++)
        {
            var e = _active[slot];
            if (e == null)
                continue;

            if (now >= e.ExpireAt)
            {
                EndEffect(slot, e);
                continue;
            }

            var player = Utilities.GetPlayerFromSlot(slot);
            if (!IsAlive(player))
            {
                _active[slot] = null;
                continue;
            }

            var pawn = player!.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            switch (e.Mode)
            {
                case "poison":
                    if (now < e.NextTick)
                        break;
                    e.NextTick = now + e.Interval;
                    if (pawn.Health > 1)
                    {
                        pawn.Health = Math.Max(pawn.Health - e.Damage, 1);
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                        player.EmitSound("Player.DamageBody.Onlooker");
                    }
                    break;

                case "slow":
                    if (Math.Abs(pawn.VelocityModifier - e.SlowFactor) > 0.001f)
                    {
                        pawn.VelocityModifier = e.SlowFactor;
                        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                    }
                    break;
            }
        }
    }

    private void EndEffect(int slot, Effect e)
    {
        _active[slot] = null;
        var pawn = Utilities.GetPlayerFromSlot(slot)?.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        if (e.Mode == "slow" && Math.Abs(pawn.VelocityModifier - 1f) > 0.001f)
        {
            pawn.VelocityModifier = 1f;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
        }
        else if (e.SizeApplied)
            ApplyScale(pawn, 1f);
    }

    private static void ApplyScale(CCSPlayerPawn? pawn, float scale)
    {
        if (pawn == null || !pawn.IsValid)
            return;

        var skeleton = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance();
        if (skeleton != null)
            skeleton.Scale = scale;

        pawn.AcceptInput("SetScale", null, null, scale.ToString(CultureInfo.InvariantCulture));
        Server.NextFrame(() =>
        {
            if (pawn.IsValid)
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
        });
    }
}
