using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class FastPlant : VipModule
{
    private class Cfg
    {
        public float Time { get; set; } = 1.0f;
        public bool ImmuneWhileBurning { get; set; } = false;
    }

    public override string Name => "FastPlant";
    public override string DisplayName => Core.Localizer["vip.module.fastplant"];

    public override void OnLoad()
    {
        FireUtil.Ensure(Core);
        Core.RegisterEventHandler<EventBombBeginplant>(OnPlant);
    }

    private HookResult OnPlant(EventBombBeginplant ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(player!) ?? new Cfg();

        if (!cfg.ImmuneWhileBurning && FireUtil.Blocked(player))
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            var weapon = player!.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
            if (weapon == null || !weapon.IsValid || weapon.DesignerName != "weapon_c4")
                return;

            var c4 = weapon.As<CC4>();
            if (c4 == null || !c4.IsValid || !c4.StartedArming)
                return;

            c4.ArmedTime = Server.CurrentTime + cfg.Time;
            Utilities.SetStateChanged(c4, "CC4", "m_fArmedTime");
        });

        return HookResult.Continue;
    }
}
