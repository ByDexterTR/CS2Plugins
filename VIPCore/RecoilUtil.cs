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

        var pawn = PlayerCache.Pawn(player);
        var weapon = PlayerCache.ActiveWeapon(player);
        if (pawn == null || weapon == null)
            return HookResult.Continue;

        string? name = PlayerCache.ActiveWeaponDesigner(player);
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
            NetState.Changed(pawn, "CCSPlayerPawn", "m_iShotsFired");
        }

        var csWeapon = weapon.As<CCSWeaponBase>();
        if (csWeapon.FlRecoilIndex > spray.Accum)
        {
            csWeapon.FlRecoilIndex = spray.Accum;
            NetState.Changed(weapon, "CCSWeaponBase", "m_flRecoilIndex");
        }

        if (csWeapon.IRecoilIndex > shots)
        {
            csWeapon.IRecoilIndex = shots;
            NetState.Changed(weapon, "CCSWeaponBase", "m_iRecoilIndex");
        }

        if (csWeapon.AccuracyPenalty > 0f)
        {
            csWeapon.AccuracyPenalty *= keep;
            NetState.Changed(weapon, "CCSWeaponBase", "m_fAccuracyPenalty");
        }

        if (csWeapon.TurningInaccuracy > 0f)
        {
            csWeapon.TurningInaccuracy *= keep;
            NetState.Changed(weapon, "CCSWeaponBase", "m_flTurningInaccuracy");
        }

        var aim = pawn.AimPunchServices;
        if (aim != null)
        {
            bool changed = Scale(aim.PredictableBaseAngle, keep);
            changed |= Scale(aim.PredictableBaseAngleVel, keep);
            changed |= Scale(aim.UnpredictableBaseAngle, keep);
            if (changed)
                NetState.Changed(pawn, "CCSPlayerPawn", "m_pAimPunchServices");
        }

        var camera = pawn.CameraServices;
        if (camera != null && Scale(camera.CsViewPunchAngle, keep))
            NetState.Changed(pawn, "CBasePlayerPawn", "m_pCameraServices");
    }

    private static bool Scale(QAngle angle, float keep)
    {
        if (angle.X == 0f && angle.Y == 0f && angle.Z == 0f)
            return false;

        angle.X *= keep;
        angle.Y *= keep;
        angle.Z *= keep;
        return true;
    }
}
