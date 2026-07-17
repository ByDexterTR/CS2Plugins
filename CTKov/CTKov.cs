using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace CTKov;

public class CTKovConfig : BasePluginConfig
{
    [JsonPropertyName("ctkov_cmd")]
    public string CtkovCommands { get; set; } = "css_ctkov,css_kovct";

    [JsonPropertyName("ctkov_flag")]
    public string CtkovFlag { get; set; } = "@jailbreak/warden,@css/generic";
}

public class CTKov : BasePlugin, IPluginConfig<CTKovConfig>
{
    public override string ModuleName => "CTKov";
    public override string ModuleVersion => "1.0.3";
    public override string ModuleAuthor => "ByDexter";
    public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

    private string ChatPrefix => Localizer["chat_prefix"];

    public CTKovConfig Config { get; set; } = new();

    public void OnConfigParsed(CTKovConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        foreach (var name in Util.Split(Config.CtkovCommands))
            AddCommand(name, "Warden olmayan CTleri T takimina tasir", OnCTKovCommand);
    }

    public void OnCTKovCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid)
            return;

        if (!Util.HasAccess(player, Config.CtkovFlag))
            return;

        int kickedCount = 0;
        var playersToMove = new List<CCSPlayerController>();

        foreach (var target in Utilities.GetPlayers())
        {
            if (target == null || !target.IsValid || target.IsBot)
                continue;

            if (target.Team != CsTeam.CounterTerrorist)
                continue;

            if (AdminManager.PlayerHasPermissions(target, "@jailbreak/warden"))
                continue;

            playersToMove.Add(target);
        }

        foreach (var target in playersToMove)
        {
            if (target.IsValid)
            {
                target.ChangeTeam(CsTeam.Terrorist);
                kickedCount++;
            }
        }

        if (kickedCount > 0)
        {
            Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctkov.guards_moved", player.PlayerName, kickedCount]}");
        }
        else
        {
            player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["ctkov.no_guards"]}");
        }
    }
}
