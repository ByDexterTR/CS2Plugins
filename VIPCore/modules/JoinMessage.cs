using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class JoinMessage : VipModule
{
    private class Cfg
    {
        public string JoinMessage { get; set; } = "";
        public string LeaveMessage { get; set; } = "";
    }

    public override string Name => "JoinMessage";
    public override string DisplayName => Core.Localizer["vip.module.joinmessage"];
    public override bool ShowInMenu => false;

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerConnectFull>(OnConnect);
        Core.RegisterEventHandler<EventPlayerDisconnect>(OnDisconnect);
    }

    private HookResult OnConnect(EventPlayerConnectFull ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        Core.AddTimer(2.0f, () =>
        {
            if (player.IsValid && Granted(player))
                Announce(player, (GroupValue<Cfg>(player)?.JoinMessage) ?? "", "vip.join_message");
        });

        return HookResult.Continue;
    }

    private HookResult OnDisconnect(EventPlayerDisconnect ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (player != null && player.IsValid && !player.IsBot && Granted(player))
            Announce(player, (GroupValue<Cfg>(player)?.LeaveMessage) ?? "", "vip.leave_message");

        return HookResult.Continue;
    }

    private void Announce(CCSPlayerController player, string raw, string defaultKey)
    {
        string msg = string.IsNullOrEmpty(raw)
            ? Core.Localizer[defaultKey, player.PlayerName].Value
            : ChatColorUtil.Process(raw).Replace("{name}", player.PlayerName, StringComparison.OrdinalIgnoreCase);

        Server.PrintToChatAll($" {CC.Orchid}{Core.Localizer["chat_prefix"]}{CC.Default} {msg}");
    }
}
