using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class Knockback : VipModule
{
    private class Cfg
    {
        public float Force { get; set; } = 120f;
        [JsonPropertyName("max_speed")]
        public float MaxSpeed { get; set; } = 1200f;
        [JsonPropertyName("only_in_air")]
        public bool OnlyInAir { get; set; } = true;
        [JsonPropertyName("only_with_weapon")]
        public string OnlyWithWeapon { get; set; } = "";

        private List<string>? _allow;
        [JsonIgnore]
        public List<string> Allow => _allow ??= WeaponUtil.ParseCsv(OnlyWithWeapon);
    }

    private static readonly HashSet<string> BulletWeapons = new(StringComparer.Ordinal)
    {
        "deagle", "revolver", "glock", "usp_silencer", "cz75a",
        "fiveseven", "p250", "tec9", "elite", "hkp2000",
        "mp9", "mac10", "bizon", "mp7", "ump45", "p90", "mp5sd",
        "famas", "galilar", "m4a1", "m4a1_silencer", "ak47", "aug", "sg556",
        "ssg08", "awp", "scar20", "g3sg1",
        "nova", "xm1014", "mag7", "sawedoff",
        "m249", "negev"
    };

    public override string Name => "Knockback";
    public override string DisplayName => Core.Localizer["vip.module.knockback"];

    public override void OnLoad() => Core.RegisterEventHandler<EventWeaponFire>(OnFire);

    private HookResult OnFire(EventWeaponFire ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (player == null || !player.IsValid || player.IsBot || !Active(player))
            return HookResult.Continue;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(player) ?? new Cfg();
        if (cfg.Force <= 0f)
            return HookResult.Continue;

        if (cfg.OnlyInAir && (pawn.Flags & 1u) != 0)
            return HookResult.Continue;

        string? weapon = ActiveWeaponName(player);
        if (!FiresBullets(weapon))
            return HookResult.Continue;

        if (cfg.Allow.Count > 0 && !WeaponUtil.MatchesAny(cfg.Allow, weapon))
            return HookResult.Continue;

        var forward = new Vector();
        NativeAPI.AngleVectors(pawn.EyeAngles.Handle, forward.Handle, 0, 0);

        pawn.AbsVelocity.X -= forward.X * cfg.Force;
        pawn.AbsVelocity.Y -= forward.Y * cfg.Force;
        pawn.AbsVelocity.Z -= forward.Z * cfg.Force;

        Clamp(pawn, cfg.MaxSpeed);
        return HookResult.Continue;
    }

    private static void Clamp(CCSPlayerPawn pawn, float maxSpeed)
    {
        if (maxSpeed <= 0f)
            return;

        var velocity = pawn.AbsVelocity;
        float speed = MathF.Sqrt(velocity.X * velocity.X + velocity.Y * velocity.Y + velocity.Z * velocity.Z);
        if (speed <= maxSpeed || speed <= 0f)
            return;

        float scale = maxSpeed / speed;
        velocity.X *= scale;
        velocity.Y *= scale;
        velocity.Z *= scale;
    }

    private static bool FiresBullets(string? weapon)
    {
        if (string.IsNullOrEmpty(weapon))
            return false;

        if (weapon.StartsWith("weapon_", StringComparison.Ordinal))
            weapon = weapon["weapon_".Length..];

        return BulletWeapons.Contains(weapon);
    }
}
