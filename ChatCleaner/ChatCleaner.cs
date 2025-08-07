using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace ChatCleaner;

public class ChatCleanerConfig : BasePluginConfig
{
    [JsonPropertyName("chat_prefix")]
    public string ChatPrefix { get; set; } = "[ByDexter]";
}

public class ChatCleaner : BasePlugin, IPluginConfig<ChatCleanerConfig>
{
    public override string ModuleName => "ChatCleaner";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "ByDexter";
    public override string ModuleDescription => "Deletes all chat messages";
    public ChatCleanerConfig Config { get; set; } = new ChatCleanerConfig();

    public void OnConfigParsed(ChatCleanerConfig config)
    {
        Config = config;
    }

    [ConsoleCommand("css_selfcc", "css_selfcc")]
    public void OnSelfChatCleanCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null) return;

        for (int i = 0; i < 500; i++)
        {
            player.PrintToChat(" ");
        }

        player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {Localizer["chatcleaner.selfcleaned"]}");
    }

    [ConsoleCommand("css_cc", "css_cc")]
    [RequiresPermissions("@css/chat")]
    public void OnAdminChatCleanCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null) return;

        for (int i = 0; i < 500; i++)
        {
            Server.PrintToChatAll(" ");
        }

        Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {Localizer["chatcleaner.admincleaned"]} {CC.Gold}{player.PlayerName}");
    }
}

public static class CC
{
    public static char Default => '\x01';
    public static char Red => '\x07';
    public static char LightRed => '\x0F';
    public static char DarkRed => '\x02';
    public static char BlueGrey => '\x0A';
    public static char Blue => '\x0B';
    public static char DarkBlue => '\x0C';
    public static char Purple => '\x0C';
    public static char Orchid => '\x0E';
    public static char Yellow => '\x09';
    public static char Gold => '\x10';
    public static char LightGreen => '\x05';
    public static char Green => '\x04';
    public static char Lime => '\x06';
    public static char Grey => '\x08';
    public static char Grey2 => '\x0D';
}