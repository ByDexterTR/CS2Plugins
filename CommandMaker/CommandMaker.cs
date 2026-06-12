using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace CommandMaker;

public class CommandMakerConfig : BasePluginConfig
{
    [JsonPropertyName("ConfigPath")]
    public string ConfigPath { get; set; } = "commands.json";
}

public class CommandDefinition
{
    [JsonPropertyName("command")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public List<string> Command { get; set; } = new();

    [JsonPropertyName("type")]
    public string Type { get; set; } = "default";

    [JsonPropertyName("args")]
    public int Args { get; set; } = 0;

    [JsonPropertyName("arg1")]
    public string? Arg1 { get; set; }

    [JsonPropertyName("arg1_number_min")]
    public int? Arg1NumberMin { get; set; }

    [JsonPropertyName("arg1_number_max")]
    public int? Arg1NumberMax { get; set; }

    [JsonPropertyName("arg1_word_length")]
    public int? Arg1WordLength { get; set; }

    [JsonPropertyName("arg2")]
    public string? Arg2 { get; set; }

    [JsonPropertyName("arg2_number_min")]
    public int? Arg2NumberMin { get; set; }

    [JsonPropertyName("arg2_number_max")]
    public int? Arg2NumberMax { get; set; }

    [JsonPropertyName("arg2_word_length")]
    public int? Arg2WordLength { get; set; }

    [JsonPropertyName("arg3")]
    public string? Arg3 { get; set; }

    [JsonPropertyName("arg3_number_min")]
    public int? Arg3NumberMin { get; set; }

    [JsonPropertyName("arg3_number_max")]
    public int? Arg3NumberMax { get; set; }

    [JsonPropertyName("arg3_word_length")]
    public int? Arg3WordLength { get; set; }

    [JsonPropertyName("flag")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public List<string>? Flag { get; set; }

    [JsonPropertyName("team_filter")]
    public string? TeamFilter { get; set; }

    [JsonPropertyName("alive_filter")]
    public string? AliveFilter { get; set; }

    [JsonPropertyName("cooldown")]
    public float Cooldown { get; set; } = 0f;

    [JsonPropertyName("sethealth")]
    public string? SetHealth { get; set; }

    [JsonPropertyName("setfreeze")]
    public string? SetFreeze { get; set; }

    [JsonPropertyName("giveweapon")]
    public string? GiveWeapon { get; set; }

    [JsonPropertyName("setnoclip")]
    public string? SetNoclip { get; set; }

    [JsonPropertyName("kill")]
    public string? Kill { get; set; }

    [JsonPropertyName("setname")]
    public string? SetName { get; set; }

    [JsonPropertyName("setarmor")]
    public string? SetArmor { get; set; }

    [JsonPropertyName("setmaxhealth")]
    public string? SetMaxHealth { get; set; }

    [JsonPropertyName("setclip")]
    public string? SetClip { get; set; }

    [JsonPropertyName("setammo")]
    public string? SetAmmo { get; set; }

    [JsonPropertyName("teleport")]
    public string? Teleport { get; set; }

    [JsonPropertyName("setplayercolor")]
    public string? SetPlayerColor { get; set; }

    [JsonPropertyName("slapdamage")]
    public string? SlapDamage { get; set; }

    [JsonPropertyName("setmoney")]
    public string? SetMoney { get; set; }

    [JsonPropertyName("changeteam")]
    public string? ChangeTeam { get; set; }

    [JsonPropertyName("setspeed")]
    public string? SetSpeed { get; set; }

    [JsonPropertyName("setgravity")]
    public string? SetGravity { get; set; }

    [JsonPropertyName("respawn")]
    public string? Respawn { get; set; }

    [JsonPropertyName("sethelmet")]
    public string? SetHelmet { get; set; }

    [JsonPropertyName("setgodmode")]
    public string? SetGodmode { get; set; }

    [JsonPropertyName("setmovetype")]
    public string? SetMoveType { get; set; }

    [JsonPropertyName("stripweapons")]
    public string? StripWeapons { get; set; }

    [JsonPropertyName("setmodel")]
    public string? SetModel { get; set; }

    [JsonPropertyName("playsound")]
    public string? PlaySound { get; set; }

    [JsonPropertyName("chat")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public List<string>? Chat { get; set; }

    [JsonPropertyName("console")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public List<string>? Console { get; set; }

    [JsonPropertyName("center")]
    public string? Center { get; set; }

    [JsonPropertyName("centertime")]
    public float CenterTime { get; set; } = 5.0f;

    [JsonPropertyName("serverchat")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public List<string>? ServerChat { get; set; }

    [JsonPropertyName("servercenter")]
    public string? ServerCenter { get; set; }

    [JsonPropertyName("execute")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public List<string>? Execute { get; set; }

    [JsonPropertyName("setcvar")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public List<string>? SetCvar { get; set; }

    [JsonPropertyName("announce")]
    public bool Announce { get; set; } = false;
}

public class StringOrArrayConverter : JsonConverter<List<string>>
{
    public override List<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var single = reader.GetString();
            return string.IsNullOrEmpty(single) ? null : new List<string> { single };
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var list = new List<string>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    var item = reader.GetString();
                    if (!string.IsNullOrEmpty(item))
                        list.Add(item);
                }
            }
            return list.Count > 0 ? list : null;
        }

        reader.Skip();
        return null;
    }

    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
            writer.WriteStringValue(item);
        writer.WriteEndArray();
    }
}

public class CommandsConfig
{
    [JsonPropertyName("Commands")]
    public List<CommandDefinition> Commands { get; set; } = new();
}

public class CommandMaker : BasePlugin, IPluginConfig<CommandMakerConfig>
{
    public override string ModuleName => "CommandMaker";
    public override string ModuleVersion => "1.0.4";
    public override string ModuleAuthor => "ByDexter";
    public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

    public CommandMakerConfig Config { get; set; } = new();
    private CommandsConfig? _commandsConfig;
    private readonly Dictionary<string, CommandDefinition> _registeredCommands = new();
    private readonly Dictionary<string, CommandInfo.CommandCallback> _commandCallbacks = new();
    private readonly HashSet<ulong> _godmodePlayers = new();
    private readonly Dictionary<ulong, float> _playerSpeed = new();
    private readonly Dictionary<ulong, float> _playerGravity = new();
    private readonly Dictionary<ulong, (string Message, float EndTime)> _playerCenter = new();
    private readonly Dictionary<(string Command, ulong SteamId), float> _cooldowns = new();

    public void OnConfigParsed(CommandMakerConfig config)
    {
        Config = config;
        LoadCommands();
    }

    public override void Unload(bool hotReload)
    {
    }

    public override void Load(bool hotReload)
    {
        base.Load(hotReload);

        RegisterListener<OnTick>(OnTick);
        RegisterListener<OnEntityTakeDamagePre>(OnEntityDamage);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterListener<OnMapStart>(_ => _cooldowns.Clear());

        AddCommand("css_cm_reload", "Komutları yeniden yükle", (player, info) =>
        {
            if (player != null && !HasAnyPermission(player, new List<string> { "@css/root" }))
            {
                info.ReplyToCommand(Localizer["commandmaker.no_permission"]);
                return;
            }

            LoadCommands();
            info.ReplyToCommand(Localizer["commandmaker.commands_loaded", _registeredCommands.Count]);
        });
    }

    private void LoadCommands()
    {
        try
        {
            string configPath = Path.Combine(ModuleDirectory, Config.ConfigPath);

            if (!File.Exists(configPath))
            {
                CreateDefaultCommandsJson(configPath);
            }

            string json = File.ReadAllText(configPath);
            _commandsConfig = JsonSerializer.Deserialize<CommandsConfig>(json);

            if (_commandsConfig == null || _commandsConfig.Commands == null)
            {
                return;
            }

            foreach (var kvp in _registeredCommands.ToList())
            {
                if (_commandCallbacks.TryGetValue(kvp.Key, out var callback))
                {
                    RemoveCommand(kvp.Key, callback);
                }
            }

            _registeredCommands.Clear();
            _commandCallbacks.Clear();

            foreach (var cmd in _commandsConfig.Commands)
            {
                RegisterDynamicCommand(cmd);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CommandMaker] ERROR: {ex.Message}");
        }
    }

    private void CreateDefaultCommandsJson(string path)
    {
        var defaultConfig = new CommandsConfig
        {
            Commands = new List<CommandDefinition>
            {
                new CommandDefinition
                {
                    Command = new List<string> { "css_hp", "css_health" },
                    Type = "target",
                    Args = 1,
                    Arg1 = "number",
                    Arg1NumberMin = 1,
                    Arg1NumberMax = 500,
                    Flag = new List<string> { "@css/slay", "@css/cheats" },
                    Cooldown = 3f,
                    SetHealth = "[TARGET] [ARG1]",
                    Chat = new List<string> { "[GOLD][TARGET] [DEFAULT]adlı oyuncunun canı [GOLD][ARG1] [DEFAULT]olarak ayarlandı." },
                    Center = "<font color='green'>Can: [ARG1]</font>",
                    CenterTime = 3.0f
                },
                new CommandDefinition
                {
                    Command = new List<string> { "css_slap" },
                    Type = "target",
                    Args = 1,
                    Arg1 = "number",
                    Arg1NumberMin = 0,
                    Arg1NumberMax = 100,
                    Flag = new List<string> { "@css/slay" },
                    SlapDamage = "[TARGET] [ARG1]",
                    Announce = true
                },
                new CommandDefinition
                {
                    Command = new List<string> { "css_money", "css_setmoney" },
                    Type = "target",
                    Args = 1,
                    Arg1 = "number",
                    Arg1NumberMin = 0,
                    Arg1NumberMax = 65535,
                    Flag = new List<string> { "@css/cheats" },
                    SetMoney = "[TARGET] [ARG1]"
                },
                new CommandDefinition
                {
                    Command = new List<string> { "css_team", "css_changeteam" },
                    Type = "target",
                    Args = 1,
                    Arg1 = "number",
                    Arg1NumberMin = 0,
                    Arg1NumberMax = 3,
                    Flag = new List<string> { "@css/kick" },
                    ChangeTeam = "[TARGET] [ARG1]"
                },
                new CommandDefinition
                {
                    Command = new List<string> { "css_site" },
                    Type = "default",
                    Chat = new List<string>
                    {
                        "[GOLD]Web Sitemiz: [DEFAULT]https://bydexter.net/",
                        "[GOLD]Discord: [DEFAULT]discord.gg/bydexter"
                    }
                },
                new CommandDefinition
                {
                    Command = new List<string> { "css_serverinfo" },
                    Type = "default",
                    Chat = new List<string>
                    {
                        "[ORCHID]Sunucu: [GOLD][HOSTNAME] [DEFAULT]| IP: [GOLD][SERVERIP]:[SERVERPORT] [DEFAULT]| Harita: [GOLD][MAPNAME] [DEFAULT]| Saat: [GOLD][TIME]",
                        "[ORCHID]Raunt: [GOLD][ROUND] [DEFAULT]| Skor: [GOLD]CT [CTSCORE] [DEFAULT]- [GOLD][TSCORE] T",
                        "[ORCHID]Oyuncu: [GOLD][PLAYERCOUNT] [DEFAULT](T: [GOLD][TCOUNT] [DEFAULT]CT: [GOLD][CTCOUNT] [DEFAULT]İzleyici: [GOLD][SPECCOUNT][DEFAULT]) | Canlı: [GOLD][ALIVECOUNT] [DEFAULT](T: [GOLD][ALIVET] [DEFAULT]CT: [GOLD][ALIVECT][DEFAULT])"
                    }
                },
                new CommandDefinition
                {
                    Command = new List<string> { "css_my" },
                    Type = "default",
                    Chat = new List<string> { "[GOLD][PLAYER] [DEFAULT]| Can: [GOLD][PLAYERHEALTH] [DEFAULT]Zırh: [GOLD][PLAYERARMOR] [DEFAULT]Para: [GOLD][PLAYERMONEY] [DEFAULT]Takım: [GOLD][PLAYERTEAM] [DEFAULT]Silah: [GOLD][PLAYERWEAPON]" },
                    Console = new List<string>
                    {
                        "SteamID64: [PLAYERSTEAMID]",
                        "Sunucu: [HOSTNAME] | Harita: [MAPNAME]"
                    }
                },
                new CommandDefinition
                {
                    Command = new List<string> { "css_target" },
                    Type = "target",
                    Flag = new List<string> { "@css/generic" },
                    Chat = new List<string> { "[GOLD][TARGET] [DEFAULT]| Can: [GOLD][TARGETHEALTH] [DEFAULT]Zırh: [GOLD][TARGETARMOR] [DEFAULT]Para: [GOLD][TARGETMONEY] [DEFAULT]Takım: [GOLD][TARGETTEAM] [DEFAULT]Silah: [GOLD][TARGETWEAPON]" },
                    Console = new List<string> { "Hedef SteamID64: [TARGETSTEAMID]" }
                },
                new CommandDefinition
                {
                    Command = new List<string> { "css_rastgele" },
                    Type = "default",
                    Flag = new List<string> { "@css/chat" },
                    Cooldown = 10f,
                    Chat = new List<string>
                    {
                        "[ORCHID]Rastgele: [GOLD][RANDOMPLAYER] [DEFAULT]| T: [GOLD][RANDOMT] [DEFAULT]| CT: [GOLD][RANDOMCT]",
                        "[ORCHID]Canlı: [GOLD][RANDOMALIVE] [DEFAULT]| Ölü: [GOLD][RANDOMDEAD]",
                        "[ORCHID]Canlı T: [GOLD][RANDOMTALIVE] [DEFAULT]| Ölü T: [GOLD][RANDOMTDEAD]",
                        "[ORCHID]Canlı CT: [GOLD][RANDOMCTALIVE] [DEFAULT]| Ölü CT: [GOLD][RANDOMCTDEAD]"
                    }
                },
                new CommandDefinition
                {
                    Command = new List<string> { "css_warmup" },
                    Type = "default",
                    Flag = new List<string> { "@css/root" },
                    SetCvar = new List<string>
                    {
                        "mp_warmuptime 60",
                        "mp_warmup_pausetimer 0"
                    },
                    Execute = new List<string> { "mp_warmup_start" },
                    ServerChat = new List<string> { "[GOLD][PLAYER] [DEFAULT]ısınma turunu başlattı." }
                },
                new CommandDefinition
                {
                    Command = new List<string> { "css_can" },
                    Type = "playertarget",
                    TeamFilter = "T",
                    AliveFilter = "alive",
                    Cooldown = 30f,
                    SetHealth = "[TARGET] 100",
                    Chat = new List<string> { "[GREEN]Canın yenilendi. [DEFAULT](30 sn'de bir kullanılabilir)" }
                }
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        string jsonContent = JsonSerializer.Serialize(defaultConfig, options);
        File.WriteAllText(path, jsonContent);
    }

    private void RegisterDynamicCommand(CommandDefinition cmd)
    {
        foreach (var entry in cmd.Command)
        {
            foreach (var commandName in entry.Split(';'))
            {
                var trimmedName = commandName.Trim();
                if (string.IsNullOrEmpty(trimmedName)) continue;

                _registeredCommands[trimmedName] = cmd;

                CommandInfo.CommandCallback callback = (player, info) =>
                {
                    HandleDynamicCommand(player, info, cmd);
                };

                _commandCallbacks[trimmedName] = callback;
                AddCommand(trimmedName, $"Dynamic command: {trimmedName}", callback);
            }
        }
    }

    private void HandleDynamicCommand(CCSPlayerController? player, CommandInfo info, CommandDefinition cmd)
    {
        if (player != null)
        {
            if (cmd.Flag is { Count: > 0 } && !HasAnyPermission(player, cmd.Flag))
            {
                info.ReplyToCommand(Localizer["commandmaker.no_permission"]);
                return;
            }

            if (!string.IsNullOrEmpty(cmd.TeamFilter))
            {
                var required = cmd.TeamFilter.Equals("CT", StringComparison.OrdinalIgnoreCase)
                    ? CsTeam.CounterTerrorist
                    : CsTeam.Terrorist;

                if (player.Team != required)
                {
                    info.ReplyToCommand(Localizer["commandmaker.team_only", cmd.TeamFilter.ToUpper()]);
                    return;
                }
            }

            if (!string.IsNullOrEmpty(cmd.AliveFilter))
            {
                bool mustBeAlive = cmd.AliveFilter.Equals("alive", StringComparison.OrdinalIgnoreCase);
                if (player.PawnIsAlive != mustBeAlive)
                {
                    info.ReplyToCommand(mustBeAlive
                        ? Localizer["commandmaker.alive_only"]
                        : Localizer["commandmaker.dead_only"]);
                    return;
                }
            }

            if (cmd.Cooldown > 0f)
            {
                var key = (info.GetArg(0).ToLower(), player.SteamID);
                float now = Server.CurrentTime;

                if (_cooldowns.TryGetValue(key, out float lastUse) && now < lastUse + cmd.Cooldown)
                {
                    int remain = (int)MathF.Ceiling(lastUse + cmd.Cooldown - now);
                    info.ReplyToCommand(Localizer["commandmaker.cooldown_wait", remain]);
                    return;
                }

                _cooldowns[key] = now;
            }
        }

        switch (cmd.Type.ToLower())
        {
            case "default":
                HandleDefaultCommand(player, info, cmd);
                break;
            case "target":
                HandleTargetCommand(player, info, cmd);
                break;
            case "playertarget":
                HandlePlayerTargetCommand(player, info, cmd);
                break;
            case "execute":
                HandleExecuteCommand(player, info, cmd);
                break;
            default:
                info.ReplyToCommand(Localizer["commandmaker.unknown_type", cmd.Type]);
                break;
        }
    }

    private void HandleDefaultCommand(CCSPlayerController? player, CommandInfo info, CommandDefinition cmd)
    {
        SendCommandMessages(player, null, cmd, null, null, null, null);
        RunServerLines(cmd.Execute, player, null, null, null, null, null);
        RunServerLines(cmd.SetCvar, player, null, null, null, null, null);
    }

    private void HandleTargetCommand(CCSPlayerController? player, CommandInfo info, CommandDefinition cmd)
    {
        if (info.ArgCount < 2)
        {
            info.ReplyToCommand($"{Localizer["commandmaker.usage_player", info.GetArg(0)]} {(cmd.Args > 0 ? "<arg1>" : "")} {(cmd.Args > 1 ? "<arg2>" : "")} {(cmd.Args > 2 ? "<arg3>" : "")}".TrimEnd());
            return;
        }

        var targetName = info.GetArg(1);
        var targets = FindTargets(targetName, player, out var groupLabel);

        if (targets.Count == 0)
        {
            info.ReplyToCommand(Localizer["commandmaker.player_not_found", targetName]);
            return;
        }

        string? arg1 = null;
        string? arg2 = null;
        string? arg3 = null;

        if (cmd.Args >= 1)
        {
            if (info.ArgCount < 3)
            {
                info.ReplyToCommand($"{Localizer["commandmaker.usage_player", info.GetArg(0)]} <arg1> {(cmd.Args > 1 ? "<arg2>" : "")} {(cmd.Args > 2 ? "<arg3>" : "")}".TrimEnd());
                return;
            }
            arg1 = info.GetArg(2);

            if (!ValidateArgument(arg1, cmd.Arg1, cmd.Arg1NumberMin, cmd.Arg1NumberMax, cmd.Arg1WordLength, out string errorMsg))
            {
                info.ReplyToCommand(errorMsg);
                return;
            }
        }

        if (cmd.Args >= 2)
        {
            if (info.ArgCount < 4)
            {
                info.ReplyToCommand($"{Localizer["commandmaker.usage_player", info.GetArg(0)]} <arg1> <arg2> {(cmd.Args > 2 ? "<arg3>" : "")}".TrimEnd());
                return;
            }
            arg2 = info.GetArg(3);

            if (!ValidateArgument(arg2, cmd.Arg2, cmd.Arg2NumberMin, cmd.Arg2NumberMax, cmd.Arg2WordLength, out string errorMsg))
            {
                info.ReplyToCommand(errorMsg);
                return;
            }
        }

        if (cmd.Args >= 3)
        {
            if (info.ArgCount < 5)
            {
                info.ReplyToCommand($"{Localizer["commandmaker.usage_player", info.GetArg(0)]} <arg1> <arg2> <arg3>");
                return;
            }
            arg3 = info.GetArg(4);

            if (!ValidateArgument(arg3, cmd.Arg3, cmd.Arg3NumberMin, cmd.Arg3NumberMax, cmd.Arg3WordLength, out string errorMsg))
            {
                info.ReplyToCommand(errorMsg);
                return;
            }
        }

        foreach (var target in targets)
            ApplyCommandActions(player, target, cmd, arg1, arg2, arg3);

        SendCommandMessages(player, targets[0], cmd, arg1, arg2, arg3, targets.Count == 1 ? null : groupLabel);
    }

    private void HandlePlayerTargetCommand(CCSPlayerController? player, CommandInfo info, CommandDefinition cmd)
    {
        List<CCSPlayerController> targets;
        string? groupLabel = null;
        string? arg1 = null;
        string? arg2 = null;
        string? arg3 = null;

        if (info.ArgCount >= 2)
        {
            var targetName = info.GetArg(1);
            targets = FindTargets(targetName, player, out groupLabel);

            if (targets.Count == 0)
            {
                info.ReplyToCommand(Localizer["commandmaker.player_not_found", targetName]);
                return;
            }

            if (info.ArgCount >= 3) arg1 = info.GetArg(2);
            if (info.ArgCount >= 4) arg2 = info.GetArg(3);
            if (info.ArgCount >= 5) arg3 = info.GetArg(4);
        }
        else
        {
            if (player == null)
            {
                info.ReplyToCommand(Localizer["commandmaker.player_only"]);
                return;
            }

            targets = new List<CCSPlayerController> { player };
        }

        foreach (var target in targets)
            ApplyCommandActions(player, target, cmd, arg1, arg2, arg3);

        SendCommandMessages(player, targets[0], cmd, arg1, arg2, arg3, targets.Count == 1 ? null : groupLabel);
    }

    private bool HasAnyPermission(CCSPlayerController player, List<string> flags)
    {
        bool anyFlag = false;

        foreach (var entry in flags)
        {
            foreach (var flag in entry.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                anyFlag = true;
                if (AdminManager.PlayerHasPermissions(player, flag))
                    return true;
            }
        }

        return !anyFlag;
    }

    private void HandleExecuteCommand(CCSPlayerController? player, CommandInfo info, CommandDefinition cmd)
    {
        if (cmd.Execute is not { Count: > 0 } && cmd.SetCvar is not { Count: > 0 })
        {
            info.ReplyToCommand(Localizer["commandmaker.execute_undefined"]);
            return;
        }

        string? arg1 = cmd.Args >= 1 && info.ArgCount >= 2 ? info.GetArg(1) : null;
        string? arg2 = cmd.Args >= 2 && info.ArgCount >= 3 ? info.GetArg(2) : null;
        string? arg3 = cmd.Args >= 3 && info.ArgCount >= 4 ? info.GetArg(3) : null;

        RunServerLines(cmd.Execute, player, null, arg1, arg2, arg3, null);
        RunServerLines(cmd.SetCvar, player, null, arg1, arg2, arg3, null);
        SendCommandMessages(player, null, cmd, arg1, arg2, arg3, null);
    }

    private void SendCommandMessages(CCSPlayerController? player, CCSPlayerController? target, CommandDefinition cmd, string? arg1, string? arg2, string? arg3, string? targetLabel)
    {
        if (cmd.Announce && player != null)
        {
            var commandName = cmd.Command.FirstOrDefault()?.Split(';')[0] ?? "";
            Server.PrintToChatAll($" {Localizer["commandmaker.announce", $"{CC.Orchid}{player.PlayerName}{CC.Default}", $"{CC.Gold}{commandName}{CC.Default}"]}");
        }

        if (cmd.Chat is { Count: > 0 } && player != null)
        {
            foreach (var line in cmd.Chat)
            {
                var chatMessage = FormatMessage(line, player, target, arg1, arg2, arg3, targetLabel);
                if (chatMessage.Length > 0 && chatMessage[0] != ' ')
                    chatMessage = " " + chatMessage;
                player.PrintToChat(chatMessage);
            }
        }

        if (cmd.Console is { Count: > 0 } && player != null)
        {
            foreach (var line in cmd.Console)
                player.PrintToConsole(FormatMessage(line, player, target, arg1, arg2, arg3, targetLabel));
        }

        if (!string.IsNullOrEmpty(cmd.Center) && player != null)
        {
            var centerMessage = FormatMessage(cmd.Center, player, target, arg1, arg2, arg3, targetLabel);
            _playerCenter[player.SteamID] = (centerMessage, Server.CurrentTime + cmd.CenterTime);
        }

        if (cmd.ServerChat is { Count: > 0 })
        {
            foreach (var line in cmd.ServerChat)
            {
                var serverChatMessage = FormatMessage(line, player, target, arg1, arg2, arg3, targetLabel);
                if (serverChatMessage.Length > 0 && serverChatMessage[0] != ' ')
                    serverChatMessage = " " + serverChatMessage;
                Server.PrintToChatAll(serverChatMessage);
            }
        }

        if (!string.IsNullOrEmpty(cmd.ServerCenter))
        {
            var message = FormatMessage(cmd.ServerCenter, player, target, arg1, arg2, arg3, targetLabel);
            foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid))
            {
                p.PrintToCenterHtml(message);
            }
        }
    }

    private void RunServerLines(List<string>? lines, CCSPlayerController? player, CCSPlayerController? target, string? arg1, string? arg2, string? arg3, string? targetLabel)
    {
        if (lines is not { Count: > 0 })
            return;

        foreach (var line in lines)
            Server.ExecuteCommand(FormatMessage(line, player, target, arg1, arg2, arg3, targetLabel));
    }

    private void ApplyCommandActions(CCSPlayerController? player, CCSPlayerController target, CommandDefinition cmd, string? arg1, string? arg2, string? arg3)
    {
        if (!string.IsNullOrEmpty(cmd.SetHealth))
        {
            string healthCmd = FormatMessage(cmd.SetHealth, player, target, arg1, arg2, arg3);
            var parts = healthCmd.Split(' ');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int health) && target.PlayerPawn.Value != null)
            {
                target.PlayerPawn.Value.Health = health;
                Utilities.SetStateChanged(target.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
            }
        }

        if (!string.IsNullOrEmpty(cmd.SetFreeze))
        {
            string freezeCmd = FormatMessage(cmd.SetFreeze, player, target, arg1, arg2, arg3);
            var parts = freezeCmd.Split(' ');
            if (parts.Length >= 2)
            {
                bool freeze = parts[1].ToLower() == "true" || parts[1] == "1";
                if (target.PlayerPawn.Value != null)
                {
                    var pawn = target.PlayerPawn.Value;
                    if (freeze)
                    {
                        pawn.MoveType = MoveType_t.MOVETYPE_INVALID;
                        Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 11);
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
                    }
                    else
                    {
                        pawn.MoveType = MoveType_t.MOVETYPE_WALK;
                        Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 2);
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(cmd.GiveWeapon))
        {
            string weaponCmd = FormatMessage(cmd.GiveWeapon, player, target, arg1, arg2, arg3);
            var parts = weaponCmd.Split(' ');
            if (parts.Length >= 2)
            {
                string weaponName = parts[1];
                if (!weaponName.StartsWith("weapon_"))
                {
                    weaponName = "weapon_" + weaponName;
                }
                target.GiveNamedItem(weaponName);
            }
        }

        if (!string.IsNullOrEmpty(cmd.SetNoclip))
        {
            string noclipCmd = FormatMessage(cmd.SetNoclip, player, target, arg1, arg2, arg3);
            if (target.PlayerPawn.Value != null)
            {
                var pawn = target.PlayerPawn.Value;
                if (pawn.MoveType == MoveType_t.MOVETYPE_NOCLIP)
                {
                    pawn.MoveType = MoveType_t.MOVETYPE_WALK;
                    Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 2);
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
                }
                else
                {
                    pawn.MoveType = MoveType_t.MOVETYPE_NOCLIP;
                    Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 8);
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
                }
            }
        }

        if (!string.IsNullOrEmpty(cmd.Kill))
        {
            if (target.PlayerPawn.Value != null)
            {
                target.PlayerPawn.Value.CommitSuicide(false, true);
            }
        }

        if (!string.IsNullOrEmpty(cmd.SetName))
        {
            string nameCmd = FormatMessage(cmd.SetName, player, target, arg1, arg2, arg3);
            var parts = nameCmd.Split(' ', 2);
            if (parts.Length >= 2)
            {
                target.PlayerName = parts[1];
                Utilities.SetStateChanged(target, "CBasePlayerController", "m_iszPlayerName");
            }
        }

        if (!string.IsNullOrEmpty(cmd.SetArmor))
        {
            string armorCmd = FormatMessage(cmd.SetArmor, player, target, arg1, arg2, arg3);
            var parts = armorCmd.Split(' ');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int armor))
            {
                var pawn = target.PlayerPawn.Value as CCSPlayerPawn;
                if (pawn != null)
                {
                    pawn.ArmorValue = armor;
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
                }
            }
        }

        if (!string.IsNullOrEmpty(cmd.SetMaxHealth))
        {
            string maxHealthCmd = FormatMessage(cmd.SetMaxHealth, player, target, arg1, arg2, arg3);
            var parts = maxHealthCmd.Split(' ');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int maxHealth))
            {
                if (target.PlayerPawn.Value != null)
                {
                    target.PlayerPawn.Value.MaxHealth = maxHealth;
                    Utilities.SetStateChanged(target.PlayerPawn.Value, "CBaseEntity", "m_iMaxHealth");
                }
            }
        }

        if (!string.IsNullOrEmpty(cmd.SetClip))
        {
            string clipCmd = FormatMessage(cmd.SetClip, player, target, arg1, arg2, arg3);
            var parts = clipCmd.Split(' ');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int clip))
            {
                var pawn = target.PlayerPawn.Value as CCSPlayerPawn;
                if (pawn?.WeaponServices?.ActiveWeapon?.Value != null)
                {
                    var weapon = pawn.WeaponServices.ActiveWeapon.Value as CBasePlayerWeapon;
                    if (weapon != null)
                    {
                        weapon.Clip1 = clip;
                        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(cmd.SetAmmo))
        {
            string ammoCmd = FormatMessage(cmd.SetAmmo, player, target, arg1, arg2, arg3);
            var parts = ammoCmd.Split(' ');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int ammo))
            {
                var pawn = target.PlayerPawn.Value as CCSPlayerPawn;
                if (pawn?.WeaponServices?.ActiveWeapon?.Value != null)
                {
                    var weapon = pawn.WeaponServices.ActiveWeapon.Value as CBasePlayerWeapon;
                    if (weapon != null)
                    {
                        weapon.ReserveAmmo[0] = ammo;
                        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(cmd.Teleport))
        {
            string tpCmd = FormatMessage(cmd.Teleport, player, target, arg1, arg2, arg3);
            var parts = tpCmd.Split(' ');
            if (parts.Length >= 4 &&
                float.TryParse(parts[1], out float x) &&
                float.TryParse(parts[2], out float y) &&
                float.TryParse(parts[3], out float z))
            {
                if (target.PlayerPawn.Value != null)
                {
                    target.PlayerPawn.Value.Teleport(new Vector(x, y, z), target.PlayerPawn.Value.EyeAngles, Vector.Zero);
                }
            }
        }

        if (!string.IsNullOrEmpty(cmd.SetPlayerColor))
        {
            string colorCmd = FormatMessage(cmd.SetPlayerColor, player, target, arg1, arg2, arg3);
            var parts = colorCmd.Split(' ');
            if (parts.Length >= 4 &&
                int.TryParse(parts[1], out int r) &&
                int.TryParse(parts[2], out int g) &&
                int.TryParse(parts[3], out int b))
            {
                if (target.PlayerPawn.Value != null)
                {
                    target.PlayerPawn.Value.Render = System.Drawing.Color.FromArgb(255, r, g, b);
                    Utilities.SetStateChanged(target.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
                }
            }
        }

        if (!string.IsNullOrEmpty(cmd.SlapDamage))
        {
            string slapCmd = FormatMessage(cmd.SlapDamage, player, target, arg1, arg2, arg3);
            var parts = slapCmd.Split(' ');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int damage))
            {
                if (target.PlayerPawn.Value != null)
                {
                    PerformSlap(target.PlayerPawn.Value, damage);
                }
            }
        }

        if (!string.IsNullOrEmpty(cmd.SetMoney))
        {
            string moneyCmd = FormatMessage(cmd.SetMoney, player, target, arg1, arg2, arg3);
            var parts = moneyCmd.Split(' ');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int money))
            {
                SetMoney(target, money);
            }
        }

        if (!string.IsNullOrEmpty(cmd.ChangeTeam))
        {
            string teamCmd = FormatMessage(cmd.ChangeTeam, player, target, arg1, arg2, arg3);
            var parts = teamCmd.Split(' ');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int team))
            {
                if (team >= 0 && team <= 3)
                {
                    target.ChangeTeam((CsTeam)team);
                }
            }
        }

        if (!string.IsNullOrEmpty(cmd.SetSpeed))
        {
            string speedCmd = FormatMessage(cmd.SetSpeed, player, target, arg1, arg2, arg3);
            var parts = speedCmd.Split(' ');
            if (parts.Length >= 2 && float.TryParse(parts[1], out float speed))
            {
                _playerSpeed[target.SteamID] = speed;
            }
        }

        if (!string.IsNullOrEmpty(cmd.SetGravity))
        {
            string gravityCmd = FormatMessage(cmd.SetGravity, player, target, arg1, arg2, arg3);
            var parts = gravityCmd.Split(' ');
            if (parts.Length >= 2 && float.TryParse(parts[1], out float gravity))
            {
                _playerGravity[target.SteamID] = gravity;
            }
        }

        if (!string.IsNullOrEmpty(cmd.Respawn))
        {
            if (target.PlayerPawn.Value != null &&
                target.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            {
                target.Respawn();
            }
        }

        if (!string.IsNullOrEmpty(cmd.SetHelmet))
        {
            target.GiveNamedItem("item_assaultsuit");
        }

        if (!string.IsNullOrEmpty(cmd.SetGodmode))
        {
            string godmodeCmd = FormatMessage(cmd.SetGodmode, player, target, arg1, arg2, arg3);
            var parts = godmodeCmd.Split(' ');
            if (parts.Length >= 2)
            {
                bool godmode = parts[1].ToLower() == "true" || parts[1] == "1";
                if (godmode)
                {
                    _godmodePlayers.Add(target.SteamID);
                }
                else
                {
                    _godmodePlayers.Remove(target.SteamID);
                }
            }
        }

        if (!string.IsNullOrEmpty(cmd.SetMoveType))
        {
            string moveTypeCmd = FormatMessage(cmd.SetMoveType, player, target, arg1, arg2, arg3);
            var parts = moveTypeCmd.Split(' ');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int moveTypeValue))
            {
                var pawn = target.PlayerPawn.Value;
                if (pawn != null)
                {
                    SetMoveTypeHelper(pawn, (MoveType_t)moveTypeValue);
                }
            }
        }

        if (!string.IsNullOrEmpty(cmd.StripWeapons))
        {
            var pawn = target.PlayerPawn.Value as CCSPlayerPawn;
            if (pawn?.WeaponServices?.MyWeapons != null)
            {
                foreach (var weaponOpt in pawn.WeaponServices.MyWeapons)
                {
                    if (weaponOpt?.Value == null)
                        continue;

                    pawn.RemovePlayerItem(weaponOpt.Value);
                }
            }
        }

        if (!string.IsNullOrEmpty(cmd.SetModel))
        {
            string modelCmd = FormatMessage(cmd.SetModel, player, target, arg1, arg2, arg3);
            var parts = modelCmd.Split(' ', 2);
            if (parts.Length >= 2)
            {
                var pawn = target.PlayerPawn.Value;
                if (pawn != null)
                {
                    Server.NextFrame(() =>
                    {
                        pawn.SetModel(parts[1]);
                    });
                }
            }
        }

        if (!string.IsNullOrEmpty(cmd.PlaySound))
        {
            string soundCmd = FormatMessage(cmd.PlaySound, player, target, arg1, arg2, arg3);
            var parts = soundCmd.Split(' ', 2);
            if (parts.Length >= 2)
            {
                target.ExecuteClientCommand($"play {parts[1]}");
            }
        }
    }

    private static void PerformSlap(CBasePlayerPawn pawn, int damage = 0)
    {
        if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return;

        var vel = new Vector(pawn.AbsVelocity.X, pawn.AbsVelocity.Y, pawn.AbsVelocity.Z);

        vel.X += (Random.Shared.Next(180) + 50) * (Random.Shared.Next(2) == 1 ? -1 : 1);
        vel.Y += (Random.Shared.Next(180) + 50) * (Random.Shared.Next(2) == 1 ? -1 : 1);
        vel.Z += Random.Shared.Next(200) + 100;

        pawn.AbsVelocity.X = vel.X;
        pawn.AbsVelocity.Y = vel.Y;
        pawn.AbsVelocity.Z = vel.Z;

        if (damage <= 0)
            return;

        pawn.Health -= damage;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        if (pawn.Health <= 0)
            pawn.CommitSuicide(true, true);
    }

    private static void SetMoney(CCSPlayerController? controller, int money)
    {
        var moneyServices = controller?.InGameMoneyServices;
        if (moneyServices == null) return;

        moneyServices.Account = money;

        if (controller != null)
            Utilities.SetStateChanged(controller, "CCSPlayerController", "m_pInGameMoneyServices");
    }

    private static void SetMoveTypeHelper(CBasePlayerPawn pawn, MoveType_t moveType)
    {
        pawn.MoveType = moveType;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
        Schema.GetRef<MoveType_t>(pawn.Handle, "CBaseEntity", "m_nActualMoveType") = moveType;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player != null)
        {
            var steamId = player.SteamID;
            _godmodePlayers.Remove(steamId);
            _playerSpeed.Remove(steamId);
            _playerGravity.Remove(steamId);
            _playerCenter.Remove(steamId);

            foreach (var key in _cooldowns.Keys.Where(k => k.SteamId == steamId).ToList())
                _cooldowns.Remove(key);
        }
        return HookResult.Continue;
    }

    private void OnTick()
    {
        if (_playerSpeed.Count == 0 && _playerGravity.Count == 0 && _playerCenter.Count == 0)
            return;

        var currentTime = Server.CurrentTime;
        var toRemove = new List<ulong>();

        foreach (var player in Utilities.GetPlayers())
        {
            if (player?.IsValid != true || player.PlayerPawn.Value == null)
                continue;

            var steamId = player.SteamID;

            if (_playerSpeed.TryGetValue(steamId, out float speed))
            {
                player.PlayerPawn.Value.VelocityModifier = speed / 250f;
            }

            if (_playerGravity.TryGetValue(steamId, out float gravity))
            {
                player.PlayerPawn.Value.GravityScale = gravity;
            }

            if (_playerCenter.TryGetValue(steamId, out var centerData))
            {
                if (currentTime < centerData.EndTime)
                {
                    player.PrintToCenterHtml(centerData.Message);
                }
                else
                {
                    toRemove.Add(steamId);
                }
            }
        }

        foreach (var steamId in toRemove)
        {
            _playerCenter.Remove(steamId);
        }
    }

    private HookResult OnEntityDamage(CEntityInstance victimEnt, CTakeDamageInfo info)
    {
        if (_godmodePlayers.Count == 0)
            return HookResult.Continue;

        if (victimEnt == null)
            return HookResult.Continue;

        var victimPawn = victimEnt as CCSPlayerPawn ?? new CCSPlayerPawn(victimEnt.Handle);
        var victimController = victimPawn?.OriginalController.Value;

        if (victimController == null)
            return HookResult.Continue;

        if (_godmodePlayers.Contains(victimController.SteamID))
        {
            info.Damage = 0f;
        }

        return HookResult.Continue;
    }

    private bool ValidateArgument(string arg, string? argType, int? min, int? max, int? maxLength, out string errorMsg)
    {
        errorMsg = "";

        if (argType == "number")
        {
            if (!int.TryParse(arg, out int value))
            {
                errorMsg = Localizer["commandmaker.arg_number"];
                return false;
            }

            if (min.HasValue && value < min.Value)
            {
                errorMsg = Localizer["commandmaker.arg_min", min.Value];
                return false;
            }

            if (max.HasValue && value > max.Value)
            {
                errorMsg = Localizer["commandmaker.arg_max", max.Value];
                return false;
            }
        }
        else if (argType == "word")
        {
            if (maxLength.HasValue && arg.Length > maxLength.Value)
            {
                errorMsg = Localizer["commandmaker.arg_word_length", maxLength.Value];
                return false;
            }
        }

        return true;
    }

    private List<CCSPlayerController> FindTargets(string search, CCSPlayerController? caller, out string? groupLabel)
    {
        groupLabel = null;
        var players = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsHLTV).ToList();
        var s = search.ToLower();

        switch (s)
        {
            case "@all":
                groupLabel = Localizer["commandmaker.group_all"];
                return players;
            case "@ct":
                groupLabel = Localizer["commandmaker.group_ct"];
                return players.Where(p => p.Team == CsTeam.CounterTerrorist).ToList();
            case "@t":
                groupLabel = Localizer["commandmaker.group_t"];
                return players.Where(p => p.Team == CsTeam.Terrorist).ToList();
            case "@alive":
                groupLabel = Localizer["commandmaker.group_alive"];
                return players.Where(p => p.PawnIsAlive).ToList();
            case "@dead":
                groupLabel = Localizer["commandmaker.group_dead"];
                return players.Where(p => !p.PawnIsAlive).ToList();
            case "@me":
                return caller != null
                    ? new List<CCSPlayerController> { caller }
                    : new List<CCSPlayerController>();
            case "@random":
                {
                    var pool = players.Where(p => !p.IsBot).ToList();
                    return pool.Count == 0
                        ? new List<CCSPlayerController>()
                        : new List<CCSPlayerController> { pool[Random.Shared.Next(pool.Count)] };
                }
        }

        if (s.StartsWith("#") && int.TryParse(s[1..], out int userid))
        {
            var byId = Utilities.GetPlayerFromUserid(userid);
            return byId != null && byId.IsValid
                ? new List<CCSPlayerController> { byId }
                : new List<CCSPlayerController>();
        }

        var byName = players.FirstOrDefault(p => p.PlayerName.ToLower() == s)
                  ?? players.FirstOrDefault(p => p.PlayerName.ToLower().Contains(s));
        return byName != null
            ? new List<CCSPlayerController> { byName }
            : new List<CCSPlayerController>();
    }

    private static string TeamName(CsTeam team) => team switch
    {
        CsTeam.Terrorist => "T",
        CsTeam.CounterTerrorist => "CT",
        CsTeam.Spectator => "SPEC",
        _ => "NONE"
    };

    private static string ActiveWeaponName(CCSPlayerController p)
    {
        var name = p.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value?.DesignerName;
        if (string.IsNullOrEmpty(name)) return "-";
        return name.StartsWith("weapon_") ? name[7..] : name;
    }

    private static string PickRandomName(IEnumerable<CCSPlayerController> pool)
    {
        var list = pool.ToList();
        return list.Count == 0 ? "-" : list[Random.Shared.Next(list.Count)].PlayerName;
    }

    private string FormatMessage(string message, CCSPlayerController? player, CCSPlayerController? target, string? arg1, string? arg2, string? arg3, string? targetLabel = null)
    {
        if (!message.Contains('['))
            return message;

        if (player != null)
        {
            if (message.Contains("[PLAYERNAME]") || message.Contains("[PLAYER]"))
            {
                var playerName = player.PlayerName;
                message = message.Replace("[PLAYERNAME]", playerName);
                message = message.Replace("[PLAYER]", playerName);
            }

            var playerPawn = player.PlayerPawn.Value;

            if (message.Contains("[PLAYERHEALTH]"))
                message = message.Replace("[PLAYERHEALTH]", (playerPawn?.Health ?? 0).ToString());

            if (message.Contains("[PLAYERARMOR]"))
                message = message.Replace("[PLAYERARMOR]", (playerPawn?.ArmorValue ?? 0).ToString());

            if (message.Contains("[PLAYERMONEY]"))
                message = message.Replace("[PLAYERMONEY]", (player.InGameMoneyServices?.Account ?? 0).ToString());

            if (message.Contains("[PLAYERSTEAMID]"))
                message = message.Replace("[PLAYERSTEAMID]", player.SteamID.ToString());

            if (message.Contains("[PLAYERTEAM]"))
                message = message.Replace("[PLAYERTEAM]", TeamName(player.Team));

            if (message.Contains("[PLAYERWEAPON]"))
                message = message.Replace("[PLAYERWEAPON]", ActiveWeaponName(player));

            if (message.Contains("[PLAYERCOORDINATE]") && playerPawn?.AbsOrigin != null)
            {
                var pos = playerPawn.AbsOrigin;
                message = message.Replace("[PLAYERCOORDINATE]", $"{pos.X} {pos.Y} {pos.Z}");
            }
        }

        if (target != null)
        {
            if (message.Contains("[TARGET]") || message.Contains("[PLAYER/TARGET]"))
            {
                var targetName = targetLabel ?? target.PlayerName;
                message = message.Replace("[PLAYER/TARGET]", targetName);
                message = message.Replace("[TARGET]", targetName);
            }

            var targetPawn = target.PlayerPawn.Value;

            if (targetPawn != null)
            {
                if (message.Contains("[TARGETHEALTH]"))
                    message = message.Replace("[TARGETHEALTH]", targetPawn.Health.ToString());

                if (message.Contains("[TARGETARMOR]"))
                    message = message.Replace("[TARGETARMOR]", targetPawn.ArmorValue.ToString());

                if (message.Contains("[TARGETCOORDINATE]") && targetPawn.AbsOrigin != null)
                {
                    var pos = targetPawn.AbsOrigin;
                    message = message.Replace("[TARGETCOORDINATE]", $"{pos.X} {pos.Y} {pos.Z}");
                }
            }

            if (message.Contains("[TARGETMONEY]"))
                message = message.Replace("[TARGETMONEY]", (target.InGameMoneyServices?.Account ?? 0).ToString());

            if (message.Contains("[TARGETSTEAMID]"))
                message = message.Replace("[TARGETSTEAMID]", target.SteamID.ToString());

            if (message.Contains("[TARGETWEAPON]"))
                message = message.Replace("[TARGETWEAPON]", ActiveWeaponName(target));

            if (message.Contains("[TARGETTEAM]"))
                message = message.Replace("[TARGETTEAM]", TeamName(target.Team));
        }

        if (arg1 != null)
        {
            message = message.Replace("[ARG1]", arg1);
        }

        if (arg2 != null)
        {
            message = message.Replace("[ARG2]", arg2);
        }

        if (arg3 != null)
        {
            message = message.Replace("[ARG3]", arg3);
        }

        bool needsRandom = message.Contains("[RANDOM");
        bool needsCounts = message.Contains("[PLAYERCOUNT]") || message.Contains("[ALIVECOUNT]") ||
                           message.Contains("[TCOUNT]") || message.Contains("[CTCOUNT]") || message.Contains("[SPECCOUNT]") ||
                           message.Contains("[ALIVET]") || message.Contains("[ALIVECT]");

        if (needsRandom || needsCounts)
        {
            var all = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsHLTV).ToList();
            var humans = all.Where(p => !p.IsBot).ToList();

            if (needsCounts)
            {
                message = message.Replace("[PLAYERCOUNT]", humans.Count.ToString());
                message = message.Replace("[ALIVECOUNT]", all.Count(p => p.PawnIsAlive).ToString());
                message = message.Replace("[TCOUNT]", all.Count(p => p.Team == CsTeam.Terrorist).ToString());
                message = message.Replace("[CTCOUNT]", all.Count(p => p.Team == CsTeam.CounterTerrorist).ToString());
                message = message.Replace("[SPECCOUNT]", all.Count(p => p.Team == CsTeam.Spectator).ToString());
                message = message.Replace("[ALIVET]", all.Count(p => p.Team == CsTeam.Terrorist && p.PawnIsAlive).ToString());
                message = message.Replace("[ALIVECT]", all.Count(p => p.Team == CsTeam.CounterTerrorist && p.PawnIsAlive).ToString());
            }

            if (needsRandom)
            {
                if (message.Contains("[RANDOMPLAYER]"))
                    message = message.Replace("[RANDOMPLAYER]", PickRandomName(humans.Where(p => p.PawnIsAlive)));
                if (message.Contains("[RANDOMT]"))
                    message = message.Replace("[RANDOMT]", PickRandomName(humans.Where(p => p.Team == CsTeam.Terrorist)));
                if (message.Contains("[RANDOMCT]"))
                    message = message.Replace("[RANDOMCT]", PickRandomName(humans.Where(p => p.Team == CsTeam.CounterTerrorist)));
                if (message.Contains("[RANDOMALIVE]"))
                    message = message.Replace("[RANDOMALIVE]", PickRandomName(humans.Where(p => p.PawnIsAlive)));
                if (message.Contains("[RANDOMDEAD]"))
                    message = message.Replace("[RANDOMDEAD]", PickRandomName(humans.Where(p => !p.PawnIsAlive)));
                if (message.Contains("[RANDOMTALIVE]"))
                    message = message.Replace("[RANDOMTALIVE]", PickRandomName(humans.Where(p => p.Team == CsTeam.Terrorist && p.PawnIsAlive)));
                if (message.Contains("[RANDOMTDEAD]"))
                    message = message.Replace("[RANDOMTDEAD]", PickRandomName(humans.Where(p => p.Team == CsTeam.Terrorist && !p.PawnIsAlive)));
                if (message.Contains("[RANDOMCTALIVE]"))
                    message = message.Replace("[RANDOMCTALIVE]", PickRandomName(humans.Where(p => p.Team == CsTeam.CounterTerrorist && p.PawnIsAlive)));
                if (message.Contains("[RANDOMCTDEAD]"))
                    message = message.Replace("[RANDOMCTDEAD]", PickRandomName(humans.Where(p => p.Team == CsTeam.CounterTerrorist && !p.PawnIsAlive)));
            }
        }

        if (message.Contains("[ROUND]"))
        {
            var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;
            message = message.Replace("[ROUND]", ((gameRules?.TotalRoundsPlayed ?? 0) + 1).ToString());
        }

        if (message.Contains("[CTSCORE]") || message.Contains("[TSCORE]"))
        {
            int ctScore = 0, tScore = 0;
            foreach (var team in Utilities.FindAllEntitiesByDesignerName<CTeam>("cs_team_manager"))
            {
                if (team.TeamNum == 3) ctScore = team.Score;
                else if (team.TeamNum == 2) tScore = team.Score;
            }
            message = message.Replace("[CTSCORE]", ctScore.ToString());
            message = message.Replace("[TSCORE]", tScore.ToString());
        }

        if (message.Contains("[SERVERIP]"))
            message = message.Replace("[SERVERIP]", ConVar.Find("ip")?.StringValue ?? "unknown");

        if (message.Contains("[SERVERPORT]"))
            message = message.Replace("[SERVERPORT]", ConVar.Find("hostport")?.GetPrimitiveValue<int>().ToString() ?? "27015");

        if (message.Contains("[HOSTNAME]"))
            message = message.Replace("[HOSTNAME]", ConVar.Find("hostname")?.StringValue ?? "unknown");

        if (message.Contains("[MAPNAME]"))
            message = message.Replace("[MAPNAME]", Server.MapName);

        if (message.Contains("[TIME]"))
            message = message.Replace("[TIME]", DateTime.Now.ToString("HH:mm:ss"));

        message = message.Replace("[DEFAULT]", $"{CC.Default}");
        message = message.Replace("[RED]", $"{CC.Red}");
        message = message.Replace("[LIGHTRED]", $"{CC.LightRed}");
        message = message.Replace("[DARKRED]", $"{CC.DarkRed}");
        message = message.Replace("[BLUEGREY]", $"{CC.BlueGrey}");
        message = message.Replace("[BLUE]", $"{CC.Blue}");
        message = message.Replace("[DARKBLUE]", $"{CC.DarkBlue}");
        message = message.Replace("[PURPLE]", $"{CC.Purple}");
        message = message.Replace("[ORCHID]", $"{CC.Orchid}");
        message = message.Replace("[YELLOW]", $"{CC.Yellow}");
        message = message.Replace("[GOLD]", $"{CC.Gold}");
        message = message.Replace("[LIGHTGREEN]", $"{CC.LightGreen}");
        message = message.Replace("[GREEN]", $"{CC.Green}");
        message = message.Replace("[LIME]", $"{CC.Lime}");
        message = message.Replace("[GREY]", $"{CC.Grey}");
        message = message.Replace("[GREY2]", $"{CC.Grey2}");

        return message;
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
