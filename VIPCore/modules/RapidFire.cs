using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class RapidFire : VipModule
{
    private class Cfg
    {
        public string OnlyWithWeapon { get; set; } = "";

        [JsonPropertyName("norecoil")]
        public bool NoRecoil { get; set; } = true;

        private List<string>? _allow;
        public List<string> Allow => _allow ??= WeaponUtil.ParseCsv(OnlyWithWeapon);
    }

    private static readonly Cfg DefaultCfg = new();

    public override string Name => "RapidFire";
    public override string DisplayName => Core.Localizer["vip.module.rapidfire"];

    public override void OnLoad() => Core.RegisterListener<OnTick>(OnTick);

    private void OnTick()
    {
        foreach (var player in Core.Players)
        {
            if (player == null || !player.IsValid || player.IsBot || !IsAlive(player) || !Active(player))
                continue;

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

            if (cfg.NoRecoil)
            {
                if (pawn.AimPunchServices != null)
                {
                    pawn.AimPunchServices.PredictableBaseTick = 0;
                    pawn.AimPunchServices.PredictableBaseTickInterpAmount = 0;
                    pawn.AimPunchServices.UnpredictableBaseTick = 0;
                }

                if (pawn.CameraServices != null)
                {
                    pawn.CameraServices.CsViewPunchAngleTick = 0;
                    pawn.CameraServices.CsViewPunchAngleTickRatio = 0f;
                }
            }

            Schema.SetSchemaValue(weapon.Handle, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick", Server.TickCount);
            Schema.SetSchemaValue(weapon.Handle, "CBasePlayerWeapon", "m_nNextSecondaryAttackTick", Server.TickCount);
        }
    }
}
