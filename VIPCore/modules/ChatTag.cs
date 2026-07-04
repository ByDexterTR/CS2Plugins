using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;

namespace VIPCore;

public class ChatTag : VipModule
{
    private class Cfg
    {
        public string Tag { get; set; } = "";
        public string NameColor { get; set; } = "gold";
        public string ChatColor { get; set; } = "default";
    }

    public override string Name => "ChatTag";
    public override string DisplayName => Core.Localizer["vip.module.chattag"];

    public override void OnLoad() => Core.HookUserMessage(118, OnChat, HookMode.Pre);

    private HookResult OnChat(UserMessage um)
    {
        var author = Utilities.GetPlayerFromIndex(um.ReadInt("entityindex"));
        if (author == null || !author.IsValid || author.IsBot || !Active(author))
            return HookResult.Continue;

        string token = um.ReadString("messagename");
        if (!token.StartsWith("Cstrike_Chat"))
            return HookResult.Continue;

        string message = um.ReadString("param2");
        if (message.Length == 0 || message.StartsWith('#'))
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(author) ?? new Cfg();
        string tag = ChatColorUtil.Process(cfg.Tag);
        string tagPart = tag.Length > 0 ? tag + " " : "";
        string name = $"{ChatColorUtil.Char(cfg.NameColor)}{author.PlayerName}";
        string body = $"{ChatColorUtil.Char(cfg.ChatColor)}{message}";

        string line = $" {Prefix(token)}{tagPart}{name}\x01{Suffix(token)}: {body}";

        um.SetString("param1", name);
        um.SetString("param2", body);
        um.SetString("messagename", line);
        return HookResult.Changed;
    }

    private string Prefix(string token)
    {
        if (token.Contains("_All"))
            return $"[{Core.Localizer["vip.chat.all"]}] ";
        if (token.Contains("_CT"))
            return $"[{Core.Localizer["vip.chat.ct"]}] ";
        if (token.Contains("_T"))
            return $"[{Core.Localizer["vip.chat.t"]}] ";
        return "";
    }

    private string Suffix(string token)
    {
        if (token.Contains("Dead"))
            return $" [{Core.Localizer["vip.chat.dead"]}]";
        if (token.Contains("Spec"))
            return $" [{Core.Localizer["vip.chat.spec"]}]";
        return "";
    }
}
