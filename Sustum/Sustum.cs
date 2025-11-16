using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

public class SustumConfig : BasePluginConfig
{
    [JsonPropertyName("chat_prefix")]
    public string ChatPrefix { get; set; } = "[ByDexter]";
}

public class Sustum : BasePlugin, IPluginConfig<SustumConfig>
{
    public override string ModuleName => "Sustum";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "ByDexter";
    public override string ModuleDescription => "Sustum eklentisi";

    public SustumConfig Config { get; set; } = new SustumConfig();

    private string SustumType = "";
    private string CurrentWord = "";
    private List<string> SustumWords = new();
    private CounterStrikeSharp.API.Modules.Timers.Timer? _sustumTimer = null;
    private bool _showHud = false;
    private string _hudHtml = "";

    private HashSet<CCSPlayerController> CTSustumPlayers = new();
    private Dictionary<ulong, bool> DSustumWin = new();

    public void OnConfigParsed(SustumConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        var jsonPath = Path.Combine(ModuleDirectory, "sustum.json");
        if (File.Exists(jsonPath))
        {
            var json = File.ReadAllText(jsonPath);
            SustumWords = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        AddCommandListener("say", OnPlayerChat);
        AddCommandListener("say_team", OnPlayerChat);
        RegisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Post);
        RegisterListener<OnTick>(OnTickHud);
    }

    public HookResult OnPlayerChat(CCSPlayerController? player, CommandInfo message)
    {
        if (player == null || string.IsNullOrEmpty(CurrentWord))
            return HookResult.Continue;

        var text = message.ArgString.Trim().Trim('"');

        if (SustumType == "CTSustum" && player.Team == CsTeam.CounterTerrorist && CTSustumPlayers.Contains(player))
        {
            if (string.Equals(text, CurrentWord, StringComparison.OrdinalIgnoreCase))
            {
                CTSustumPlayers.Remove(player);
                if (CTSustumPlayers.Count == 1)
                {
                    var lastCT = CTSustumPlayers.First();
                    ShowWinnerHud(lastCT.PlayerName);
                    lastCT.ChangeTeam(CsTeam.Terrorist);
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
            var activeWeapon = player.PlayerPawn.Value!.WeaponServices!.ActiveWeapon.Get();
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
        foreach (var player in Utilities.GetPlayers())
            player.PrintToCenterHtml(" ");
    }

    private void ShowWinnerHud(string winnerName)
    {
        PrintPrefixAll(SustumType == "CTSustum" ? $"{CC.Blue}{SustumType} {CC.Default}Kaybetti: {CC.Gold}{winnerName}{CC.Default}" : $"{CC.Blue}{SustumType} {CC.Default}Kazandı: {CC.Gold}{winnerName}{CC.Default}");
        _hudHtml = SustumType == "CTSustum" ? $"<b><font color='#FF0000'>{winnerName} kaybetti!</font></b>" : $"<b><font color='#AAFF00'>{winnerName} kazandı!</font></b>";
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
                    CTSustumPlayers.Add(player);
                }
            }
            if (CTSustumPlayers.Count == 1)
            {
                var lastCT = CTSustumPlayers.First();
                ShowWinnerHud(lastCT.PlayerName);
                lastCT.ChangeTeam(CsTeam.Terrorist);
                CTSustumPlayers.Clear();
                return;
            }
        }

        int countdown = 3;
        _sustumTimer?.Kill();

        string adminName = admin?.PlayerName ?? "Birisi";

        _showHud = true;
        _hudHtml =
            $"<b><font color='#FFA500'>{adminName} {SustumType} başlattı</font></b><br>"
            + $"{countdown} saniye: kelime burada gözükecek";

        _sustumTimer = AddTimer(1.0f, () =>
            {
                if (countdown > 1)
                {
                    countdown--;
                    _hudHtml =
                        $"<b><font color='#FFA500'>{adminName} {SustumType} başlattı</font></b><br>"
                        + $"{countdown} saniye: kelime burada gözükecek";
                }
                else
                {
                    var rnd = new Random();
                    int kelimeSayisi = rnd.Next(1, 5);
                    List<string> kelimeler = new();
                    for (int i = 0; i < kelimeSayisi; i++)
                    {
                        kelimeler.Add(SustumWords[rnd.Next(SustumWords.Count)]);
                    }
                    CurrentWord = string.Join(" ", kelimeler);
                    _hudHtml =
                        $"<b><font color='#FFA500'>{adminName} {SustumType} başlattı</font></b><br>"
                        + $"<font color='#FF0000'>&raquo;</font> {CurrentWord} <font color='#FF0000'>&laquo;</font>";
                    _showHud = true;
                    _sustumTimer?.Kill();
                }
            }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
    }

    [ConsoleCommand("css_ctsustum", "css_ctsustum")]
    [RequiresPermissionsOr("@css/generic", "@jailbreak/warden")]
    public void OnCTSustumCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return;

        if (!string.IsNullOrEmpty(SustumType))
        {
            PrintPrefix(player, $" {CC.Green}{SustumType}{CC.Default} oynanıyor!");
            return;
        }
        PrintPrefixAll($"{CC.Gold}{player.PlayerName}{CC.Default}, CTSustum {CC.Green}başlattı{CC.Default}!");

        StartSustumRound("CTSustum", player);
    }

    [ConsoleCommand("css_tsustum", "css_tsustum")]
    [RequiresPermissionsOr("@css/generic", "@jailbreak/warden")]
    public void OnTSustumCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return;

        if (!string.IsNullOrEmpty(SustumType))
        {
            PrintPrefix(player, $"{CC.Green}{SustumType}{CC.Default} oynanıyor!");
            return;
        }
        PrintPrefixAll($"{CC.Gold}{player.PlayerName}{CC.Default}, TSustum {CC.Green}başlattı{CC.Default}!");
        StartSustumRound("TSustum", player);
    }

    [ConsoleCommand("css_dsustum", "css_dsustum")]
    [RequiresPermissionsOr("@css/generic", "@jailbreak/warden")]
    public void OnDSustumCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return;

        if (!string.IsNullOrEmpty(SustumType))
        {
            PrintPrefix(player, $"{CC.Green}{SustumType}{CC.Default} oynanıyor!");
            return;
        }
        PrintPrefixAll($"{CC.Gold}{player.PlayerName}{CC.Default}, DSustum {CC.Green}başlattı{CC.Default}!");
        StartSustumRound("DSustum", player);
    }

    [ConsoleCommand("css_olusustum", "css_olusustum")]
    [RequiresPermissionsOr("@css/generic", "@jailbreak/warden")]
    public void OnOluSustumCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return;

        if (!string.IsNullOrEmpty(SustumType))
        {
            PrintPrefix(player, $"{CC.Green}{SustumType}{CC.Default} oynanıyor!");
            return;
        }
        PrintPrefixAll($"{CC.Gold}{player.PlayerName}{CC.Default}, ÖlüSustum {CC.Green}başlattı{CC.Default}!");
        StartSustumRound("ÖlüSustum", player);
    }

    [ConsoleCommand("css_ctsustum0", "css_ctsustum0")]
    [ConsoleCommand("css_dsustum0", "css_dsustum0")]
    [ConsoleCommand("css_tsustum0", "css_tsustum0")]
    [ConsoleCommand("css_olusustum0", "css_olusustum0")]
    [ConsoleCommand("css_sustum0", "css_sustum0")]
    [RequiresPermissionsOr("@css/generic", "@jailbreak/warden")]
    public void OnSustumCancelCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return;

        if (string.IsNullOrEmpty(SustumType))
        {
            PrintPrefix(player, "Aktif bir sustum oyunu yok!");
            return;
        }

        HideHud();
        PrintPrefixAll($"{CC.Gold}{player.PlayerName}{CC.Default} sustum oyununu {CC.Red}iptal etti{CC.Default}!");
    }

    private void PrintPrefix(CCSPlayerController? player, string message)
    {
        if (player == null || !player.IsValid)
            return;

        player.PrintToChat($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {message}");
    }

    private void PrintPrefixAll(string message)
    {
        Server.PrintToChatAll($" {CC.Orchid}{Config.ChatPrefix}{CC.Default} {message}");
    }

    static bool IsAlive(CCSPlayerController? player)
    {
        return player?.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
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
