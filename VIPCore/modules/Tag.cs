using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;

namespace VIPCore;

public class Tag : VipModule
{
    private class Cfg
    {
        [JsonPropertyName("tag")]
        public string TagText { get; set; } = "";
        public string NameColor { get; set; } = "gold";
        public string ChatColor { get; set; } = "default";
        public string Tab { get; set; } = "";
    }

    private readonly bool[] _tabApplied = new bool[64];

    public override string Name => "Tag";
    public override string DisplayName => Core.Localizer["vip.module.tag"];

    public override void OnLoad()
    {
        Core.HookUserMessage(118, OnChat, HookMode.Pre);
        Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
    }

    public override void OnUnload()
    {
        for (int slot = 0; slot < 64; slot++)
            if (_tabApplied[slot])
                ClearTab(Utilities.GetPlayerFromSlot(slot));
    }

    public override void OnSelect(CCSPlayerController player, string value)
    {
        if (value == "on")
            ApplyTab(player);
        else
            ClearTab(player);
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        if (Active(player))
            ApplyTab(player);
        else if (_tabApplied[player.Slot])
            ClearTab(player);

        return HookResult.Continue;
    }

    private void ApplyTab(CCSPlayerController player)
    {
        string tab = (GroupValue<Cfg>(player) ?? new Cfg()).Tab;
        if (tab.Length == 0)
            return;

        if (player.Clan != tab)
        {
            player.Clan = tab;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
            new EventNextlevelChanged(false).FireEvent(false);
        }
        _tabApplied[player.Slot] = true;
    }

    private void ClearTab(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid)
            return;

        _tabApplied[player.Slot] = false;
        if (player.Clan.Length == 0)
            return;

        player.Clan = "";
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
        new EventNextlevelChanged(false).FireEvent(false);
    }

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
        string tag = ChatColorUtil.Process(cfg.TagText);
        string tagPart = tag.Length > 0 ? tag + " " : "";
        string name = $"{ChatColorUtil.Char(cfg.NameColor)}{author.PlayerName}";
        string body = $"{ChatColorUtil.Char(cfg.ChatColor)}{message}";

        string line = $" \x01{Prefix(token)}{tagPart}{name}\x01{Suffix(token)}\x01: {body}";

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
