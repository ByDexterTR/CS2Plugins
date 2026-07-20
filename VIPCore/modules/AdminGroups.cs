using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;

namespace VIPCore;

public class AdminGroups : VipModule
{
    private readonly Dictionary<ulong, string[]> _granted = new();

    public override string Name => "AdminGroups";
    public override string DisplayName => Core.Localizer["vip.module.admingroups"];
    public override bool ShowInMenu => false;

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerConnectFull>((ev, _) => { Apply(ev.Userid); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerSpawn>((ev, _) => { Apply(ev.Userid); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) => { Revoke(ev.Userid); return HookResult.Continue; });
    }

    public override void OnUnload()
    {
        foreach (var (steamId, groups) in _granted)
            AdminManager.RemovePlayerFromGroup(new SteamID(steamId), true, groups);
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

        var groups = (GroupValue<List<string>>(player) ?? new())
            .Where(g => !string.IsNullOrWhiteSpace(g) && g.StartsWith('#') && !AdminManager.PlayerInGroup(player, g))
            .ToArray();

        if (groups.Length == 0)
            return;

        AdminManager.AddPlayerToGroup(player, groups);
        _granted[steamId] = groups;
    }

    private void Revoke(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        if (_granted.Remove(player.SteamID, out var groups))
            AdminManager.RemovePlayerFromGroup(player, true, groups);
    }
}
