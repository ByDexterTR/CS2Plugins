using System.Text.Json.Serialization;

namespace VIPCore;

public class VipConfig
{
    [JsonPropertyName("storage")]
    public string Storage { get; set; } = "json";

    [JsonPropertyName("menu_type")]
    public string MenuType { get; set; } = "hud";

    [JsonPropertyName("admin_flag")]
    public string AdminFlag { get; set; } = "@css/root";

    [JsonPropertyName("commands")]
    public CommandNames Commands { get; set; } = new();

    [JsonPropertyName("mysql")]
    public MySqlSettings MySql { get; set; } = new();
}

public class CommandNames
{
    [JsonPropertyName("menu")] public string Menu { get; set; } = "css_vip,css_vipmenu";
    [JsonPropertyName("list_online")] public string ListOnline { get; set; } = "css_vips,css_onlinevip";
    [JsonPropertyName("list_all")] public string ListAll { get; set; } = "css_viplist";
    [JsonPropertyName("addvip")] public string AddVip { get; set; } = "css_addvip,css_vipadd";
    [JsonPropertyName("removevip")] public string RemoveVip { get; set; } = "css_removevip,css_delvip";
    [JsonPropertyName("reload")] public string Reload { get; set; } = "css_reloadvip,css_vipreload";
    [JsonPropertyName("tp")] public string Tp { get; set; } = "css_tp,css_thirdperson";
}

public class MySqlSettings
{
    [JsonPropertyName("host")] public string Host { get; set; } = "";
    [JsonPropertyName("port")] public uint Port { get; set; } = 3306;
    [JsonPropertyName("database")] public string Database { get; set; } = "";
    [JsonPropertyName("user")] public string User { get; set; } = "";
    [JsonPropertyName("password")] public string Password { get; set; } = "";
    [JsonPropertyName("table_prefix")] public string TablePrefix { get; set; } = "vip_";
}
