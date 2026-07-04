using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;

namespace VIPCore;

public class AdminFlags : VipModule
{
    private readonly Dictionary<ulong, string[]> _granted = new();

    public override string Name => "AdminFlags";
    public override string DisplayName => Core.Localizer["vip.module.adminflags"];
    public override bool ShowInMenu => false;

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerConnectFull>((ev, _) => { Apply(ev.Userid); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerSpawn>((ev, _) => { Apply(ev.Userid); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) => { Revoke(ev.Userid); return HookResult.Continue; });
    }

    public override void OnUnload()
    {
        foreach (var (steamId, flags) in _granted)
            AdminManager.RemovePlayerPermissions(new SteamID(steamId), flags);
        _granted.Clear();
    }

    private void Apply(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        ulong steamId = player.SteamID;
        bool has = _granted.ContainsKey(steamId);

        if (!Granted(player))
        {
            if (has)
                Revoke(player);
            return;
        }

        if (has)
            return;

        var flags = (GroupValue<List<string>>(player) ?? new())
            .Where(f => !string.IsNullOrWhiteSpace(f) && f.StartsWith('@') && !AdminManager.PlayerHasPermissions(player, f))
            .ToArray();

        if (flags.Length == 0)
            return;

        AdminManager.AddPlayerPermissions(player, flags);
        _granted[steamId] = flags;
    }

    private void Revoke(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        if (_granted.Remove(player.SteamID, out var flags))
            AdminManager.RemovePlayerPermissions(player, flags);
    }
}
