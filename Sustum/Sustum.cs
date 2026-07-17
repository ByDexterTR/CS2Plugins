using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

public class SustumConfig : BasePluginConfig
{
    [JsonPropertyName("stop_cmd")]
    public string StopCommands { get; set; } = "css_ctsustum0,css_dsustum0,css_tsustum0,css_olusustum0,css_sustum0";

    [JsonPropertyName("stop_flag")]
    public string StopFlag { get; set; } = "@jailbreak/warden,@css/generic";

    [JsonPropertyName("ctsustum_cmd")]
    public string CtSustumCommands { get; set; } = "css_ctsustum";

    [JsonPropertyName("ctsustum_flag")]
    public string CtSustumFlag { get; set; } = "@jailbreak/warden,@css/generic";

    [JsonPropertyName("tsustum_cmd")]
    public string TSustumCommands { get; set; } = "css_tsustum";

    [JsonPropertyName("tsustum_flag")]
    public string TSustumFlag { get; set; } = "@jailbreak/warden,@css/generic";

    [JsonPropertyName("dsustum_cmd")]
    public string DSustumCommands { get; set; } = "css_dsustum";

    [JsonPropertyName("dsustum_flag")]
    public string DSustumFlag { get; set; } = "@jailbreak/warden,@css/generic";

    [JsonPropertyName("olusustum_cmd")]
    public string OluSustumCommands { get; set; } = "css_olusustum";

    [JsonPropertyName("olusustum_flag")]
    public string OluSustumFlag { get; set; } = "@jailbreak/warden,@css/generic";
}

public class Sustum : BasePlugin, IPluginConfig<SustumConfig>
{
    public override string ModuleName => "Sustum";
    public override string ModuleVersion => "1.0.7";
    public override string ModuleAuthor => "ByDexter";
    public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

    private string SustumType = "";
    private string CurrentWord = "";
    private List<string> SustumWords = new();
    private CounterStrikeSharp.API.Modules.Timers.Timer? _sustumTimer = null;
    private bool _showHud = false;
    private string _hudHtml = "";

    private HashSet<ulong> CTSustumPlayers = new();
    private Dictionary<ulong, bool> DSustumWin = new();

    public SustumConfig Config { get; set; } = new();

    public void OnConfigParsed(SustumConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        var jsonPath = Path.Combine(ModuleDirectory, "Sustum.json");
        if (File.Exists(jsonPath))
        {
            var json = File.ReadAllText(jsonPath);
            SustumWords = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        AddCommandListener("say", OnPlayerChat);
        AddCommandListener("say_team", OnPlayerChat);
        RegisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Post);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterListener<OnTick>(OnTickHud);

        foreach (var name in Util.Split(Config.CtSustumCommands))
            AddCommand(name, "CTSustum baslatir", OnCTSustumCommand);
        foreach (var name in Util.Split(Config.TSustumCommands))
            AddCommand(name, "TSustum baslatir", OnTSustumCommand);
        foreach (var name in Util.Split(Config.DSustumCommands))
            AddCommand(name, "DSustum baslatir", OnDSustumCommand);
        foreach (var name in Util.Split(Config.OluSustumCommands))
            AddCommand(name, "OluSustum baslatir", OnOluSustumCommand);
        foreach (var name in Util.Split(Config.StopCommands))
            AddCommand(name, "Sustum oyununu durdurur", OnSustumCancelCommand);
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player != null)
        {
            DSustumWin.Remove(player.SteamID);
            CTSustumPlayers.Remove(player.SteamID);
        }
        return HookResult.Continue;
    }

    public HookResult OnPlayerChat(CCSPlayerController? player, CommandInfo message)
    {
        if (player == null || string.IsNullOrEmpty(CurrentWord))
            return HookResult.Continue;

        var text = message.ArgString.Trim().Trim('"');

        if (SustumType == "CTSustum" && player.Team == CsTeam.CounterTerrorist && CTSustumPlayers.Contains(player.SteamID))
        {
            if (string.Equals(text, CurrentWord, StringComparison.OrdinalIgnoreCase))
            {
                CTSustumPlayers.Remove(player.SteamID);
                if (CTSustumPlayers.Count == 1)
                {
                    var lastSteamId = CTSustumPlayers.First();
                    var lastCT = Utilities.GetPlayerFromSteamId(lastSteamId);
                    if (lastCT != null && lastCT.IsValid)
                    {
                        ShowWinnerHud(lastCT.PlayerName);
                        lastCT.ChangeTeam(CsTeam.Terrorist);
                    }
                    CTSustumPlayers.Clear();
                }
            }
        }
        else if (SustumType == "TSustum" && player.Team == CsTeam.Terrorist)
        {
            if (string.Equals(text, CurrentWord, StringComparison.OrdinalIgnoreCase))
            {
                ShowWinnerHud(player.PlayerName);
                player.ChangeTeam(CsTeam.CounterTerrorist);
            }
        }
        else if (SustumType == "DSustum" && player.Team == CsTeam.Terrorist && IsAlive(player))
        {
            if (string.Equals(text, CurrentWord, StringComparison.OrdinalIgnoreCase))
            {
                ShowWinnerHud(player.PlayerName);
                player.GiveNamedItem("weapon_deagle");
                DSustumWin[player.SteamID] = true;
                var playerPawn = player!.PlayerPawn.Value;
                if (playerPawn != null)
                {
                    playerPawn.Render = Color.FromArgb(255, 255, 165, 0);
                    Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");
                }
            }
        }
        else if (SustumType == "ÖlüSustum" && player.Team == CsTeam.Terrorist && !IsAlive(player))
        {
            if (string.Equals(text, CurrentWord, StringComparison.OrdinalIgnoreCase))
            {
                ShowWinnerHud(player.PlayerName);
                player.Respawn();
            }
        }
        return HookResult.Continue;
    }

    public HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (DSustumWin.TryGetValue(player.SteamID, out bool win) && win)
        {
            var activeWeapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Get();
            if (activeWeapon != null && activeWeapon.DesignerName == "weapon_deagle")
            {
                activeWeapon.Remove();
                DSustumWin[player.SteamID] = false;
                var playerPawn = player!.PlayerPawn.Value;
                if (playerPawn != null)
                {
                    playerPawn.Render = Color.FromArgb(255, 255, 255, 255);
                    Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");
                }
            }
        }
        return HookResult.Continue;
    }

    private void OnTickHud()
    {
        if (_showHud)
        {
            foreach (var player in Utilities.GetPlayers())
            {
                player.PrintToCenterHtml(_hudHtml);
            }
        }
    }

    private void HideHud()
    {
        _sustumTimer?.Kill();
        SustumType = "";
        CurrentWord = "";
        _showHud = false;
    }

    private void ShowWinnerHud(string winnerName)
    {
        PrintPrefixAll(SustumType == "CTSustum" ? Localizer["sustum.loser", winnerName].ToString() : Localizer["sustum.winner", winnerName].ToString());
        _hudHtml = SustumType == "CTSustum" ? $"<font color='#FF0000'>{Localizer["sustum.hud_lost", winnerName]}</font>" : $"<font color='#AAFF00'>{Localizer["sustum.hud_won", winnerName]}</font>";
        _showHud = true;

        AddTimer(2.0f, HideHud);
    }

    private void StartSustumRound(string type, CCSPlayerController? admin = null)
    {
        if (SustumWords.Count == 0 || !string.IsNullOrEmpty(SustumType))
            return;

        SustumType = type;
        if (SustumType == "CTSustum")
        {
            CTSustumPlayers.Clear();
            CCSPlayerController? warden = null;
            foreach (var player in Utilities.GetPlayers())
            {
                if (player.Team == CsTeam.CounterTerrorist && AdminManager.PlayerHasPermissions(player, "@jailbreak/warden"))
                {
                    warden = player;
                    break;
                }
            }
            foreach (var player in Utilities.GetPlayers())
            {
                if (player.Team == CsTeam.CounterTerrorist && player != warden)
                {
                    CTSustumPlayers.Add(player.SteamID);
                }
            }
            if (CTSustumPlayers.Count == 1)
            {
                var lastSteamId = CTSustumPlayers.First();
                var lastCT = Utilities.GetPlayerFromSteamId(lastSteamId);
                if (lastCT != null && lastCT.IsValid)
                {
                    ShowWinnerHud(lastCT.PlayerName);
                    lastCT.ChangeTeam(CsTeam.Terrorist);
                }
                CTSustumPlayers.Clear();
                return;
            }
        }

        int countdown = 3;
        _sustumTimer?.Kill();

        string adminName = admin?.PlayerName ?? Localizer["sustum.hud_someone"];
        string startedLine = $"<font color='#FFA500'>{Localizer["sustum.hud_started", adminName, SustumType]}</font><br>";

        _showHud = true;
        _hudHtml = startedLine + Localizer["sustum.hud_countdown", countdown];

        _sustumTimer = AddTimer(1.0f, () =>
            {
                if (countdown > 1)
                {
                    countdown--;
                    _hudHtml = startedLine + Localizer["sustum.hud_countdown", countdown];
                }
                else
                {
                    int kelimeSayisi = Random.Shared.Next(1, 5);
                    List<string> kelimeler = new();
                    for (int i = 0; i < kelimeSayisi; i++)
                    {
                        kelimeler.Add(SustumWords[Random.Shared.Next(SustumWords.Count)]);
                    }
                    CurrentWord = string.Join(" ", kelimeler);
                    _hudHtml = startedLine + $"<font class='fontSize-l'>{CurrentWord}</font>";
                    _showHud = true;
                    _sustumTimer?.Kill();
                }
            }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
    }

    public void OnCTSustumCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || !Util.HasAccess(player, Config.CtSustumFlag))
            return;

        if (!string.IsNullOrEmpty(SustumType))
        {
            PrintPrefix(player, Localizer["sustum.in_progress", SustumType].ToString());
            return;
        }
        PrintPrefixAll(Localizer["sustum.started", player.PlayerName, "CTSustum"].ToString());

        StartSustumRound("CTSustum", player);
    }

    public void OnTSustumCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || !Util.HasAccess(player, Config.TSustumFlag))
            return;

        if (!string.IsNullOrEmpty(SustumType))
        {
            PrintPrefix(player, Localizer["sustum.in_progress", SustumType].ToString());
            return;
        }
        PrintPrefixAll(Localizer["sustum.started", player.PlayerName, "TSustum"].ToString());
        StartSustumRound("TSustum", player);
    }

    public void OnDSustumCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || !Util.HasAccess(player, Config.DSustumFlag))
            return;

        if (!string.IsNullOrEmpty(SustumType))
        {
            PrintPrefix(player, Localizer["sustum.in_progress", SustumType].ToString());
            return;
        }
        PrintPrefixAll(Localizer["sustum.started", player.PlayerName, "DSustum"].ToString());
        StartSustumRound("DSustum", player);
    }

    public void OnOluSustumCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || !Util.HasAccess(player, Config.OluSustumFlag))
            return;

        if (!string.IsNullOrEmpty(SustumType))
        {
            PrintPrefix(player, Localizer["sustum.in_progress", SustumType].ToString());
            return;
        }
        PrintPrefixAll(Localizer["sustum.started", player.PlayerName, "ÖlüSustum"].ToString());
        StartSustumRound("ÖlüSustum", player);
    }

    public void OnSustumCancelCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || !Util.HasAccess(player, Config.StopFlag))
            return;

        if (string.IsNullOrEmpty(SustumType))
        {
            PrintPrefix(player, Localizer["sustum.no_active"].ToString());
            return;
        }

        HideHud();
        PrintPrefixAll(Localizer["sustum.cancelled", player.PlayerName].ToString());
    }

    private void PrintPrefix(CCSPlayerController? player, string message)
    {
        if (player == null || !player.IsValid)
            return;

        player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {message}");
    }

    private void PrintPrefixAll(string message)
    {
        Server.PrintToChatAll($" {CC.Orchid}{ChatPrefix}{CC.Default} {message}");
    }

    static bool IsAlive(CCSPlayerController? player)
    {
        return player?.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
    }
}
