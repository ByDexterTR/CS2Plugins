using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;

namespace VIPCore;

public class VIPChat : VipModule
{
    public override string Name => "VIPChat";
    public override string DisplayName => Core.Localizer["vip.module.vipchat"];
    public override bool ShowInMenu => false;

    public override void OnLoad() => Core.HookUserMessage(118, OnChat, HookMode.Pre);

    private HookResult OnChat(UserMessage um)
    {
        var author = Utilities.GetPlayerFromIndex(um.ReadInt("entityindex"));
        if (author == null || !author.IsValid || author.IsBot)
            return HookResult.Continue;

        string message = um.ReadString("param2");
        if (!message.StartsWith('#') || !Granted(author))
            return HookResult.Continue;

        string text = message[1..].Trim();
        if (text.Length == 0)
            return HookResult.Stop;

        string name = $" {ChatColorUtil.Char("purple")}[VIP] {ChatColorUtil.Char("gold")}{author.PlayerName}";
        string body = $"{ChatColorUtil.Char("green")}{text}";

        um.SetString("param1", name);
        um.SetString("param2", body);
        um.SetString("messagename", $"{name}\x01: {body}");

        for (int i = um.Recipients.Count - 1; i >= 0; i--)
        {
            var recipient = um.Recipients[i];
            if (recipient == null || !Core.IsClientVip(recipient))
                um.Recipients.RemoveAt(i);
        }

        return HookResult.Changed;
    }
}
