using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class GrenadeTrajectory : VipModule
{
    private class Cfg
    {
        public bool Molotov { get; set; } = true;
        public bool Smokegrenade { get; set; } = true;
        public bool Flashbang { get; set; } = true;
        public bool Hegrenade { get; set; } = true;
        public bool Decoy { get; set; } = true;
    }

    private const string Preview = "sv_grenade_trajectory_prac_pipreview";

    private readonly Dictionary<int, bool> _on = new();

    public override string Name => "GrenadeTrajectory";
    public override string DisplayName => Core.Localizer["vip.module.grenadetrajectory"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventItemEquip>((ev, _) => { Schedule(ev.Userid); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerSpawn>((ev, _) => { Schedule(ev.Userid); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) => { Set(ev.Userid, false); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) => { Forget(ev.Userid); return HookResult.Continue; });
        Core.HookMapStart(_ => _on.Clear());
    }

    public override void OnUnload()
    {
        foreach (var player in Core.Players)
            Set(player, false);

        _on.Clear();
    }

    public override void OnSelect(CCSPlayerController player, string value)
    {
        if (value == "off")
            Set(player, false);
        else
            Sync(player);
    }

    private void Schedule(CCSPlayerController? player) => Server.NextFrame(() => Sync(player));

    private void Sync(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        if (!IsAlive(player) || !Active(player))
        {
            Set(player, false);
            return;
        }

        var cfg = GroupValue<Cfg>(player) ?? new Cfg();
        Set(player, Wanted(cfg, ActiveWeaponName(player)));
    }

    private static bool Wanted(Cfg cfg, string? weapon) => weapon switch
    {
        "weapon_molotov" or "weapon_incgrenade" => cfg.Molotov,
        "weapon_smokegrenade" => cfg.Smokegrenade,
        "weapon_flashbang" => cfg.Flashbang,
        "weapon_hegrenade" => cfg.Hegrenade,
        "weapon_decoy" => cfg.Decoy,
        _ => false
    };

    private void Set(CCSPlayerController? player, bool on)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        int id = player.UserId ?? -1;
        if (id < 0)
            return;

        if (_on.TryGetValue(id, out bool current) && current == on)
            return;

        _on[id] = on;
        player.ReplicateConVar(Preview, on ? "1" : "0");
    }

    private void Forget(CCSPlayerController? player)
    {
        int id = player?.UserId ?? -1;
        if (id >= 0)
            _on.Remove(id);
    }
}
