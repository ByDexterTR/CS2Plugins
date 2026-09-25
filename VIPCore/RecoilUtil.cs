using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public delegate float RecoilProvider(CCSPlayerController player, CCSPlayerPawn pawn);

public static class RecoilUtil
{
    private const int SprayResetTicks = 32;

    private static VIPCore? _owner;
    private static readonly List<RecoilProvider> _providers = new();
    private static readonly Dictionary<int, (float Accum, int Tick)> _spray = new();

    public static void Ensure(VIPCore core)
    {
        if (ReferenceEquals(_owner, core))
            return;

        _owner = core;
        _providers.Clear();
        _spray.Clear();

        core.RegisterEventHandler<EventWeaponFire>(OnFire);
        core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) => { Forget(ev.Userid); return HookResult.Continue; });
        core.HookMapStart(_ => _spray.Clear());
    }

    public static void Provide(RecoilProvider provider)
    {
        if (!_providers.Contains(provider))
            _providers.Add(provider);
    }

    public static void Withdraw(RecoilProvider provider) => _providers.Remove(provider);

    private static void Forget(CCSPlayerController? player)
    {
        int id = player?.UserId ?? -1;
        if (id >= 0)
            _spray.Remove(id);
    }

    private static HookResult OnFire(EventWeaponFire ev, GameEventInfo info)
    {
        if (_providers.Count == 0)
            return HookResult.Continue;

        var player = ev.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        var pawn = player.PlayerPawn.Value;
        var weapon = pawn?.WeaponServices?.ActiveWeapon.Value;
        if (pawn == null || !pawn.IsValid || weapon == null || !weapon.IsValid)
            return HookResult.Continue;

        string name = weapon.DesignerName;
        if (string.IsNullOrEmpty(name) || name.Contains("knife") || name.Contains("bayonet") || name.Contains("c4"))
            return HookResult.Continue;

        float keep = 1f;
        foreach (var provider in _providers)
            keep = MathF.Min(keep, provider(player, pawn));

        if (keep >= 1f)
            return HookResult.Continue;

        Apply(player, pawn, weapon, Math.Clamp(keep, 0f, 1f));
        return HookResult.Continue;
    }

    private static void Apply(CCSPlayerController player, CCSPlayerPawn pawn, CBasePlayerWeapon weapon, float keep)
    {
        int id = player.UserId ?? -1;
        if (id < 0)
            return;

        int now = Server.TickCount;
        _spray.TryGetValue(id, out var spray);
        if (now - spray.Tick > SprayResetTicks)
            spray.Accum = 0f;

        spray.Tick = now;
        spray.Accum += keep;
        _spray[id] = spray;

        int shots = (int)spray.Accum;
        if (pawn.ShotsFired != shots)
        {
            pawn.ShotsFired = shots;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_iShotsFired");
        }

        var csWeapon = weapon.As<CCSWeaponBase>();
        if (csWeapon.FlRecoilIndex > spray.Accum)
        {
            csWeapon.FlRecoilIndex = spray.Accum;
            Utilities.SetStateChanged(weapon, "CCSWeaponBase", "m_flRecoilIndex");
        }

        if (csWeapon.IRecoilIndex > shots)
        {
            csWeapon.IRecoilIndex = shots;
            Utilities.SetStateChanged(weapon, "CCSWeaponBase", "m_iRecoilIndex");
        }

        if (csWeapon.AccuracyPenalty > 0f)
        {
            csWeapon.AccuracyPenalty *= keep;
            Utilities.SetStateChanged(weapon, "CCSWeaponBase", "m_fAccuracyPenalty");
        }

        if (csWeapon.TurningInaccuracy > 0f)
        {
            csWeapon.TurningInaccuracy *= keep;
            Utilities.SetStateChanged(weapon, "CCSWeaponBase", "m_flTurningInaccuracy");
        }

        var aim = pawn.AimPunchServices;
        if (aim != null)
        {
            Scale(aim.PredictableBaseAngle, keep);
            Scale(aim.PredictableBaseAngleVel, keep);
            Scale(aim.UnpredictableBaseAngle, keep);
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_pAimPunchServices");
        }

        var camera = pawn.CameraServices;
        if (camera != null)
        {
            Scale(camera.CsViewPunchAngle, keep);
            Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");
        }
    }

    private static void Scale(QAngle angle, float keep)
    {
        angle.X *= keep;
        angle.Y *= keep;
        angle.Z *= keep;
    }
}
