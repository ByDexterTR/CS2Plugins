using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class PoisonBullet : VipModule
{
    private class Cfg
    {
        [JsonPropertyName("minhp")]
        public int MinHp { get; set; } = 1;
        public int Damage { get; set; } = 1;
        [JsonPropertyName("damagetick")]
        public float DamageTick { get; set; } = 1f;
        public string OnlyWithWeapon { get; set; } = "";
        public bool IgnoreTeammates { get; set; } = true;

        private List<string>? _allow;
        public List<string> Allow => _allow ??= WeaponUtil.ParseCsv(OnlyWithWeapon);
    }

    private static readonly Cfg DefaultCfg = new();

    private class Poisoned
    {
        public int MinHp;
        public int Damage;
        public float Interval;
        public float NextTick;
    }

    private readonly Poisoned?[] _poisoned = new Poisoned?[64];

    public override string Name => "PoisonBullet";
    public override string DisplayName => Core.Localizer["vip.module.poisonbullet"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerHurt>(OnHurt);
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) =>
        {
            int slot = ev.Userid?.Slot ?? -1;
            if (slot >= 0 && slot < 64)
                _poisoned[slot] = null;
            return HookResult.Continue;
        });
        Core.RegisterEventHandler<EventRoundStart>((_, __) => { Array.Clear(_poisoned); return HookResult.Continue; });
        Core.RegisterListener<OnTick>(OnTick);
    }

    private HookResult OnHurt(EventPlayerHurt ev, GameEventInfo info)
    {
        var attacker = ev.Attacker;
        if (!Active(attacker))
            return HookResult.Continue;

        var victim = ev.Userid;
        int slot = victim?.Slot ?? -1;
        if (slot < 0 || slot >= 64 || victim!.Slot == attacker!.Slot)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(attacker) ?? DefaultCfg;
        if (cfg.IgnoreTeammates && victim.Team == attacker.Team)
            return HookResult.Continue;

        var allow = cfg.Allow;
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(attacker!)))
            return HookResult.Continue;

        float interval = Math.Max(cfg.DamageTick, 0.1f);
        _poisoned[slot] = new Poisoned
        {
            MinHp = cfg.MinHp,
            Damage = cfg.Damage,
            Interval = interval,
            NextTick = Server.CurrentTime + interval
        };

        return HookResult.Continue;
    }

    private void OnTick()
    {
        float now = Server.CurrentTime;
        for (int slot = 0; slot < 64; slot++)
        {
            var poison = _poisoned[slot];
            if (poison == null || now < poison.NextTick)
                continue;

            poison.NextTick = now + poison.Interval;

            var player = Utilities.GetPlayerFromSlot(slot);
            if (!IsAlive(player))
            {
                _poisoned[slot] = null;
                continue;
            }

            var pawn = player!.PlayerPawn.Value!;
            if (pawn.Health <= poison.MinHp)
                continue;

            pawn.Health = Math.Max(pawn.Health - poison.Damage, poison.MinHp);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            player.EmitSound("Player.DamageBody.Onlooker");
        }
    }
}
