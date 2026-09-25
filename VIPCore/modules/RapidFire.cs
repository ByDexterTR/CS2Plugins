using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class RapidFire : VipModule
{
    private class Cfg
    {
        public string OnlyWithWeapon { get; set; } = "";

        public int OnlyStance { get; set; } = 0;

        [JsonPropertyName("firepercent")]
        public float FirePercent { get; set; } = 2f;

        private List<string>? _allow;
        public List<string> Allow => _allow ??= WeaponUtil.ParseCsv(OnlyWithWeapon);
    }

    private static readonly Cfg DefaultCfg = new();

    private readonly uint[] _rateWeapon = new uint[64];
    private readonly int[] _ratePrimary = new int[64];
    private readonly int[] _rateSecondary = new int[64];

    public override string Name => "RapidFire";
    public override string DisplayName => Core.Localizer["vip.module.rapidfire"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerSpawn>((ev, _) =>
        {
            int slot = ev.Userid?.Slot ?? -1;
            if (slot >= 0 && slot < 64)
                _rateWeapon[slot] = 0;
            return HookResult.Continue;
        });
        Core.HookTick(OnTick);
    }

    private void OnTick()
    {
        foreach (var player in ActivePlayers())
        {
            var pawn = player.PlayerPawn.Value;
            var weapon = pawn?.WeaponServices?.ActiveWeapon.Value;
            if (pawn == null || weapon == null || !weapon.IsValid)
                continue;

            string name = weapon.DesignerName;
            if (string.IsNullOrEmpty(name) || name.Contains("knife") || name.Contains("bayonet") || name.Contains("c4"))
                continue;

            var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;
            var allow = cfg.Allow;
            if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(player)))
                continue;

            if (!StanceFilter.Matches(cfg.OnlyStance, pawn))
                continue;

            ApplyFireRate(player.Slot, weapon, Math.Clamp(cfg.FirePercent, 0.1f, 2f));
        }
    }

    private void ApplyFireRate(int slot, CBasePlayerWeapon weapon, float fire)
    {
        if (MathF.Abs(fire - 1f) < 0.001f)
            return;

        float factor = 2f - fire;
        int now = Server.TickCount;

        if (_rateWeapon[slot] != weapon.Index)
        {
            _rateWeapon[slot] = weapon.Index;
            _ratePrimary[slot] = 0;
            _rateSecondary[slot] = 0;
        }

        int next = weapon.NextPrimaryAttackTick;
        if (next > now && next != _ratePrimary[slot])
        {
            int target = Scale(now, next, factor);
            weapon.NextPrimaryAttackTick = target;
            _ratePrimary[slot] = target;
        }

        next = weapon.NextSecondaryAttackTick;
        if (next > now && next != _rateSecondary[slot])
        {
            int target = Scale(now, next, factor);
            weapon.NextSecondaryAttackTick = target;
            _rateSecondary[slot] = target;
        }
    }

    private static int Scale(int now, int next, float factor) =>
        factor <= 0f ? now : now + Math.Max((int)MathF.Round((next - now) * factor), 1);
}
