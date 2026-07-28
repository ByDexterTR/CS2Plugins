using System.Globalization;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.UserMessages;

namespace AntiCapsLock;

public class AntiCapsLockConfig : BasePluginConfig
{
    [JsonPropertyName("mode_capslock")]
    public int ModeCapslock { get; set; } = 1;

    [JsonPropertyName("threshold_capslock")]
    public float ThresholdCapslock { get; set; } = 0.5f;

    [JsonPropertyName("minlength_capslock")]
    public int MinLengthCapslock { get; set; } = 4;

    [JsonPropertyName("lowercase_culture")]
    public string LowercaseCulture { get; set; } = "tr-TR";

    [JsonPropertyName("capsignore_flag")]
    public string CapsignoreFlag { get; set; } = "";
}

public class AntiCapsLock : BasePlugin, IPluginConfig<AntiCapsLockConfig>
{
    public override string ModuleName => "AntiCapsLock";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "ByDexter";
    public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

    public AntiCapsLockConfig Config { get; set; } = new();

    private string ChatPrefix => Localizer["chat_prefix"];
    private CultureInfo _culture = CultureInfo.InvariantCulture;

    public void OnConfigParsed(AntiCapsLockConfig config)
    {
        if (config.ModeCapslock is not (1 or 2)) config.ModeCapslock = 1;
        if (config.ThresholdCapslock < 0f) config.ThresholdCapslock = 0f;
        if (config.ThresholdCapslock > 1f) config.ThresholdCapslock = 1f;
        if (config.MinLengthCapslock < 1) config.MinLengthCapslock = 1;

        try
        {
            _culture = config.LowercaseCulture.Length > 0
                ? CultureInfo.GetCultureInfo(config.LowercaseCulture)
                : CultureInfo.InvariantCulture;
        }
        catch (CultureNotFoundException)
        {
            _culture = CultureInfo.InvariantCulture;
        }

        Config = config;
    }

    public override void Load(bool hotReload)
    {
        HookUserMessage(118, OnChatMessage, HookMode.Pre);
        AddCommandListener("say", OnSay, HookMode.Pre);
        AddCommandListener("say_team", OnSay, HookMode.Pre);
    }

    private HookResult OnChatMessage(UserMessage um)
    {
        if (Config.ModeCapslock != 1)
            return HookResult.Continue;

        if (!IsAffected(Utilities.GetPlayerFromIndex(um.ReadInt("entityindex"))))
            return HookResult.Continue;

        if (!um.ReadString("messagename").StartsWith("Cstrike_Chat"))
            return HookResult.Continue;

        string message = um.ReadString("param2");
        if (!IsCapsLock(message))
            return HookResult.Continue;

        um.SetString("param2", message.ToLower(_culture));
        return HookResult.Changed;
    }

    private HookResult OnSay(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (Config.ModeCapslock != 2 || !IsAffected(player))
            return HookResult.Continue;

        if (!IsCapsLock(commandInfo.GetArg(1)))
            return HookResult.Continue;

        player!.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["capslock.warn"]}");
        return HookResult.Handled;
    }

    private bool IsAffected(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return false;

        return Config.CapsignoreFlag.Length == 0
            || !AdminManager.PlayerHasPermissions(player, Config.CapsignoreFlag);
    }

    private bool IsCapsLock(string message)
    {
        if (message.Length == 0 || message[0] is '!' or '/' or '#')
            return false;

        int letters = 0, upper = 0;
        foreach (char c in message)
        {
            if (!char.IsLetter(c))
                continue;

            letters++;
            if (char.IsUpper(c))
                upper++;
        }

        return letters >= Config.MinLengthCapslock && (float)upper / letters >= Config.ThresholdCapslock;
    }
}
