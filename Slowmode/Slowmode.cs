using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace Slowmode;

public class SlowmodeConfig : BasePluginConfig
{
    [JsonPropertyName("slowignore_flag")]
    public string SlowignoreFlag { get; set; } = "@css/chat";

    [JsonPropertyName("slow_min")]
    public int SlowMin { get; set; } = 1;

    [JsonPropertyName("slow_max")]
    public int SlowMax { get; set; } = 300;
}

public class Slowmode : BasePlugin, IPluginConfig<SlowmodeConfig>
{
    public override string ModuleName => "Slowmode";
    public override string ModuleVersion => "1.0.1";
    public override string ModuleAuthor => "ByDexter";
    public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

    public SlowmodeConfig Config { get; set; } = new();

    private string ChatPrefix => Localizer["chat_prefix"];
    private int _interval;
    private readonly Dictionary<ulong, double> _lastMessage = new();

    public void OnConfigParsed(SlowmodeConfig config)
    {
        if (config.SlowMin < 1) config.SlowMin = 1;
        if (config.SlowMax < config.SlowMin) config.SlowMax = config.SlowMin;
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        AddCommandListener("say", OnSay, HookMode.Pre);
        AddCommandListener("say_team", OnSay, HookMode.Pre);
        RegisterListener<Listeners.OnMapStart>(_ => _lastMessage.Clear());
    }

    [ConsoleCommand("css_slowmode", "css_slowmode <saniye|off>")]
    [RequiresPermissions("@css/chat")]
    public void OnSlowmodeCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (commandInfo.ArgCount < 2)
        {
            commandInfo.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["slowmode.usage", Config.SlowMin, Config.SlowMax]}");
            return;
        }

        string arg = commandInfo.GetArg(1);
        if (arg.Equals("off", StringComparison.OrdinalIgnoreCase) || arg == "0")
        {
            _interval = 0;
            _lastMessage.Clear();
            Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["slowmode.disabled"]}");
            return;
        }

        if (!int.TryParse(arg, out int seconds) || seconds < Config.SlowMin || seconds > Config.SlowMax)
        {
            commandInfo.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["slowmode.usage", Config.SlowMin, Config.SlowMax]}");
            return;
        }

        _interval = seconds;
        _lastMessage.Clear();
        Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["slowmode.enabled", seconds]}");
    }

    private HookResult OnSay(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (_interval <= 0 || player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        string message = commandInfo.GetArg(1);
        if (message.Length == 0)
            return HookResult.Continue;

        if (Config.SlowignoreFlag.Length > 0 && AdminManager.PlayerHasPermissions(player, Config.SlowignoreFlag))
            return HookResult.Continue;

        double now = Server.EngineTime;
        if (_lastMessage.TryGetValue(player.SteamID, out double last))
        {
            double remaining = _interval - (now - last);
            if (remaining > 0)
            {
                player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["slowmode.wait", (int)Math.Ceiling(remaining)]}");
                return HookResult.Handled;
            }
        }

        _lastMessage[player.SteamID] = now;
        return HookResult.Continue;
    }
}
