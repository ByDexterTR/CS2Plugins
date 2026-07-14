using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;

namespace AdminList;

public class AdminGroup
{
  [JsonPropertyName("tag")]
  public string Tag { get; set; } = "";

  [JsonPropertyName("tag_color")]
  public string TagColor { get; set; } = "default";

  [JsonPropertyName("name_color")]
  public string NameColor { get; set; } = "default";

  [JsonPropertyName("flag")]
  public string Flag { get; set; } = "";
}

public class AdminListConfig : BasePluginConfig
{
  [JsonPropertyName("admins_cmd")]
  public string Commands { get; set; } = "css_admins";

  [JsonPropertyName("reload_cmd")]
  public string ReloadCommands { get; set; } = "css_adminsreload,css_reloadadmins";

  [JsonPropertyName("reload_flag")]
  public string ReloadFlag { get; set; } = "@css/root";

  [JsonPropertyName("groups")]
  public List<AdminGroup> Groups { get; set; } = new()
  {
    new AdminGroup { Tag = "OWNER", TagColor = "darkred", NameColor = "gold", Flag = "@css/owner" },
    new AdminGroup { Tag = "DEV", TagColor = "purple", NameColor = "lightred", Flag = "@css/dev" },
    new AdminGroup { Tag = "MOD", TagColor = "blue", NameColor = "bluegrey", Flag = "@css/mod" },
    new AdminGroup { Tag = "VIP", TagColor = "gold", NameColor = "yellow", Flag = "@css/vip" }
  };
}

public class AdminList : BasePlugin, IPluginConfig<AdminListConfig>
{
  public override string ModuleName => "AdminList";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public AdminListConfig Config { get; set; } = new();

  private static readonly JsonSerializerOptions JsonOpts = new()
  {
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true
  };

  public void OnConfigParsed(AdminListConfig config)
  {
    config.Groups.RemoveAll(g => string.IsNullOrWhiteSpace(g.Tag) || string.IsNullOrWhiteSpace(g.Flag));
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    foreach (var name in Split(Config.Commands))
      AddCommand(name, "Cevrimici yetkilileri listeler", OnAdminsCommand);

    foreach (var name in Split(Config.ReloadCommands))
      AddCommand(name, "AdminList configini yeniden yukler", OnReloadCommand);
  }

  private static string[] Split(string names) =>
    names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

  private void OnAdminsCommand(CCSPlayerController? player, CommandInfo info)
  {
    var players = Utilities.GetPlayers()
      .Where(p => p != null && p.IsValid && !p.IsBot && !p.IsHLTV)
      .ToList();

    var seen = new HashSet<int>();
    var lines = new List<string>();

    foreach (var group in Config.Groups)
    {
      char tagColor = ChatColor.Get(group.TagColor);
      char nameColor = ChatColor.Get(group.NameColor);

      foreach (var p in players)
      {
        if (seen.Contains(p.Slot) || !AdminManager.PlayerHasPermissions(p, group.Flag))
          continue;

        seen.Add(p.Slot);
        lines.Add($" {tagColor}[{group.Tag}]{CC.Default} {nameColor}{p.PlayerName}");
      }
    }

    if (lines.Count == 0)
    {
      info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["admins.none"]}");
      return;
    }

    info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["admins.title"]}");
    foreach (var line in lines)
      info.ReplyToCommand(line);
  }

  private void OnReloadCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player != null && !AdminManager.PlayerHasPermissions(player, Config.ReloadFlag))
    {
      info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["admins.no_permission"]}");
      return;
    }

    try
    {
      string path = Path.GetFullPath(Path.Combine(
        ModuleDirectory, "..", "..", "configs", "plugins", "AdminList", "AdminList.json"));

      if (!File.Exists(path))
      {
        info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["admins.reload_failed"]}");
        return;
      }

      var config = JsonSerializer.Deserialize<AdminListConfig>(File.ReadAllText(path), JsonOpts);
      if (config == null)
      {
        info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["admins.reload_failed"]}");
        return;
      }

      OnConfigParsed(config);
      info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["admins.reloaded"]}");
    }
    catch (Exception ex)
    {
      Logger.LogError("AdminList: config yeniden yuklenemedi. {0}", ex.Message);
      info.ReplyToCommand($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["admins.reload_failed"]}");
    }
  }
}

public static class ChatColor
{
  private static readonly Dictionary<string, char> Colors = new(StringComparer.OrdinalIgnoreCase)
  {
    ["default"] = '\x01', ["white"] = '\x01', ["darkred"] = '\x02', ["green"] = '\x04',
    ["lightgreen"] = '\x05', ["lime"] = '\x06', ["red"] = '\x07', ["grey"] = '\x08',
    ["yellow"] = '\x09', ["bluegrey"] = '\x0A', ["blue"] = '\x0B', ["darkblue"] = '\x0C',
    ["purple"] = '\x0E', ["orchid"] = '\x0E', ["lightred"] = '\x0F', ["gold"] = '\x10'
  };

  public static char Get(string? name) =>
    name != null && Colors.TryGetValue(name.Trim(), out var c) ? c : '\x01';
}

public static class CC
{
  public static char Default => '\x01';
  public static char Orchid => '\x0E';
}
