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

    [JsonPropertyName("buy_commands")]
    public Dictionary<string, string> BuyCommands { get; set; } = new()
    {
        ["galil"] = "css_galil",
        ["ak47"] = "css_ak47,css_ak",
        ["sg553"] = "css_sg553,css_sg",
        ["glock"] = "css_glock",
        ["mac10"] = "css_mac10,css_mac",
        ["tec9"] = "css_tec9,css_tec",
        ["sawedoff"] = "css_sawedoff",
        ["g3sg1"] = "css_g3sg1",
        ["p2000"] = "css_p2000",
        ["mag7"] = "css_mag7",
        ["fiveseven"] = "css_fiveseven",
        ["famas"] = "css_famas",
        ["m4a1"] = "css_m4a1,css_m4a1s",
        ["m4a4"] = "css_m4a4",
        ["aug"] = "css_aug",
        ["scar20"] = "css_scar20",
        ["mp9"] = "css_mp9",
        ["usp"] = "css_usp,css_usps"
    };

    [JsonPropertyName("hide")]
    public Dictionary<string, string> Hide { get; set; } = new()
    {
        ["BulletTrail"] = "all",
        ["C4Effect"] = "all",
        ["KillEffect"] = "all",
        ["PlayerTrail"] = "all",
        ["PlayerGlow"] = "self",
        ["GrenadeTrail"] = "all",
        ["SaySound"] = "all"
    };

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
    [JsonPropertyName("updateuser")] public string UpdateUser { get; set; } = "css_updatevip,css_vipupdate";
    [JsonPropertyName("hidevip")] public string HideVip { get; set; } = "css_hidevip,css_hidefx";
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
