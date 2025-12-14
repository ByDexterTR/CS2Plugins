using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
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
    public string Command { get; set; } = "";

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
    public string? Flag { get; set; }

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
    public string? Chat { get; set; }

    [JsonPropertyName("center")]
    public string? Center { get; set; }

    [JsonPropertyName("centertime")]
    public float CenterTime { get; set; } = 5.0f;

    [JsonPropertyName("serverchat")]
    public string? ServerChat { get; set; }

    [JsonPropertyName("servercenter")]
    public string? ServerCenter { get; set; }

    [JsonPropertyName("execute")]
    public string? Execute { get; set; }

    [JsonPropertyName("announce")]
    public bool Announce { get; set; } = false;
}

public class CommandsConfig
{
    [JsonPropertyName("Commands")]
    public List<CommandDefinition> Commands { get; set; } = new();
}

public class CommandMaker : BasePlugin, IPluginConfig<CommandMakerConfig>
{
    public override string ModuleName => "CommandMaker";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "ByDexter";
    public override string ModuleDescription => "Komut oluşturucu";

    public CommandMakerConfig Config { get; set; } = new();
    private CommandsConfig? _commandsConfig;
    private readonly Dictionary<string, CommandDefinition> _registeredCommands = new();
    private readonly Dictionary<string, CommandInfo.CommandCallback> _commandCallbacks = new();
    private readonly HashSet<ulong> _godmodePlayers = new();
    private readonly Dictionary<ulong, float> _playerSpeed = new();
    private readonly Dictionary<ulong, float> _playerGravity = new();
    private readonly Dictionary<ulong, (string Message, float EndTime)> _playerCenter = new();

    public void OnConfigParsed(CommandMakerConfig config)
    {
        Config = config;
        LoadCommands();
    }

    public override void Unload(bool hotReload)
    {
        try { VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre); } catch { }
    }

    public override void Load(bool hotReload)
    {
        base.Load(hotReload);

        RegisterListener<OnTick>(OnTick);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);

        AddCommand("css_cm_reload", "Komutları yeniden yükle", (player, info) =>
        {
            if (player != null && !HasAnyPermission(player, "@css/root"))
            {
                info.ReplyToCommand("Bu komutu kullanmak için yetkiniz yok.");
                return;
            }

            LoadCommands();
            info.ReplyToCommand($"CommandMaker: {_registeredCommands.Count} komut yüklendi.");
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
                    Command = "css_hp;css_health",
                    Type = "target",
                    Args = 1,
                    Arg1 = "number",
                    Arg1NumberMin = 1,
                    Arg1NumberMax = 500,
                    Flag = "@css/slay;@css/cheats",
                    SetHealth = "[TARGET] [ARG1]",
                    Chat = "[GOLD][TARGET] [DEFAULT]adlı oyuncunun canı [GOLD][ARG1] [DEFAULT]olarak ayarlandı.",
                    Center = "<font color='green'>Can: [ARG1]</font>",
                    CenterTime = 3.0f,
                    Announce = false
                },
                new CommandDefinition
                {
                    Command = "css_slap",
                    Type = "target",
                    Args = 1,
                    Arg1 = "number",
                    Arg1NumberMin = 0,
                    Arg1NumberMax = 100,
                    Flag = "@css/slay",
                    SlapDamage = "[TARGET] [ARG1]",
                    Announce = true
                },
                new CommandDefinition
                {
                    Command = "css_money;css_setmoney",
                    Type = "target",
                    Args = 1,
                    Arg1 = "number",
                    Arg1NumberMin = 0,
                    Arg1NumberMax = 65535,
                    Flag = "@css/cheats",
                    SetMoney = "[TARGET] [ARG1]"
                },
                new CommandDefinition
                {
                    Command = "css_team;css_changeteam",
                    Type = "target",
                    Args = 1,
                    Arg1 = "number",
                    Arg1NumberMin = 0,
                    Arg1NumberMax = 3,
                    Flag = "@css/kick",
                    ChangeTeam = "[TARGET] [ARG1]"
                },
                new CommandDefinition
                {
                    Command = "css_site",
                    Type = "default",
                    Flag = "",
                    Chat = "[GOLD]Web Sitemiz: [DEFAULT]https://bydexter.net/"
                },
                new CommandDefinition
                {
                    Command = "css_serverinfo",
                    Type = "default",
                    Flag = "",
                    Chat = "[ORCHID]Sunucu: [GOLD][HOSTNAME] [DEFAULT]| IP: [GOLD][SERVERIP]:[SERVERPORT] [DEFAULT]| Harita: [GOLD][MAPNAME] [DEFAULT]| Oyuncu: [GOLD][PLAYERCOUNT]"
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
        var commandNames = cmd.Command.Split(';');

        foreach (var commandName in commandNames)
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

    private void HandleDynamicCommand(CCSPlayerController? player, CommandInfo info, CommandDefinition cmd)
    {
        if (!string.IsNullOrEmpty(cmd.Flag) && player != null)
        {
            if (!HasAnyPermission(player, cmd.Flag))
            {
                info.ReplyToCommand("Bu komutu kullanmak için yetkiniz yok.");
                return;
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
                info.ReplyToCommand($"Bilinmeyen komut tipi: {cmd.Type}");
                break;
        }
    }

    private void HandleDefaultCommand(CCSPlayerController? player, CommandInfo info, CommandDefinition cmd)
    {
        if (!string.IsNullOrEmpty(cmd.Chat))
        {
            var chatMessage = FormatMessage(cmd.Chat, player, null, null, null, null);
            if (chatMessage.Length > 0 && chatMessage[0] != ' ')
            {
                chatMessage = " " + chatMessage;
            }
            if (player != null)
            {
                player.PrintToChat(chatMessage);
            }
        }

        if (!string.IsNullOrEmpty(cmd.Center) && player != null)
        {
            var centerMessage = FormatMessage(cmd.Center, player, null, null, null, null);
            _playerCenter[player.SteamID] = (centerMessage, Server.CurrentTime + cmd.CenterTime);
        }

        if (!string.IsNullOrEmpty(cmd.ServerChat))
        {
            var serverChatMessage = FormatMessage(cmd.ServerChat, player, null, null, null, null);
            if (serverChatMessage.Length > 0 && serverChatMessage[0] != ' ')
            {
                serverChatMessage = " " + serverChatMessage;
            }
            Server.PrintToChatAll(serverChatMessage);
        }

        if (!string.IsNullOrEmpty(cmd.ServerCenter))
        {
            var message = FormatMessage(cmd.ServerCenter, player, null, null, null, null);
            foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid))
            {
                p.PrintToCenterHtml(message);
            }
        }
    }

    private void HandleTargetCommand(CCSPlayerController? player, CommandInfo info, CommandDefinition cmd)
    {
        if (info.ArgCount < 2)
        {
            info.ReplyToCommand($"Kullanım: {info.GetArg(0)} <oyuncu> {(cmd.Args > 0 ? "<arg1>" : "")} {(cmd.Args > 1 ? "<arg2>" : "")} {(cmd.Args > 2 ? "<arg3>" : "")}");
            return;
        }

        var targetName = info.GetArg(1);
        var target = FindTarget(targetName);

        if (target == null)
        {
            info.ReplyToCommand($"Oyuncu bulunamadı: {targetName}");
            return;
        }

        string? arg1 = null;
        string? arg2 = null;
        string? arg3 = null;

        if (cmd.Args >= 1)
        {
            if (info.ArgCount < 3)
            {
                info.ReplyToCommand($"Kullanım: {info.GetArg(0)} <oyuncu> <arg1> {(cmd.Args > 1 ? "<arg2>" : "")} {(cmd.Args > 2 ? "<arg3>" : "")}");
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
                info.ReplyToCommand($"Kullanım: {info.GetArg(0)} <oyuncu> <arg1> <arg2> {(cmd.Args > 2 ? "<arg3>" : "")}");
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
                info.ReplyToCommand($"Kullanım: {info.GetArg(0)} <oyuncu> <arg1> <arg2> <arg3>");
                return;
            }
            arg3 = info.GetArg(4);

            if (!ValidateArgument(arg3, cmd.Arg3, cmd.Arg3NumberMin, cmd.Arg3NumberMax, cmd.Arg3WordLength, out string errorMsg))
            {
                info.ReplyToCommand(errorMsg);
                return;
            }
        }

        ExecuteCommandActions(player, target, cmd, arg1, arg2, arg3);
    }
    private void HandlePlayerTargetCommand(CCSPlayerController? player, CommandInfo info, CommandDefinition cmd)
    {
        CCSPlayerController? target = player;
        string? arg1 = null;
        string? arg2 = null;
        string? arg3 = null;

        if (info.ArgCount >= 2)
        {
            var targetName = info.GetArg(1);
            target = FindTarget(targetName);

            if (target == null)
            {
                info.ReplyToCommand($"Oyuncu bulunamadı: {targetName}");
                return;
            }

            if (info.ArgCount >= 3) arg1 = info.GetArg(2);
            if (info.ArgCount >= 4) arg2 = info.GetArg(3);
            if (info.ArgCount >= 5) arg3 = info.GetArg(4);
        }
        else if (info.ArgCount >= 2 && cmd.Arg1 != "optional")
        {
            arg1 = info.GetArg(1);
            if (info.ArgCount >= 3) arg2 = info.GetArg(2);
            if (info.ArgCount >= 4) arg3 = info.GetArg(3);
        }

        if (target == null)
        {
            info.ReplyToCommand("Bu komut sadece oyuncular tarafından kullanılabilir.");
            return;
        }

        ExecuteCommandActions(player, target, cmd, arg1, arg2, arg3);
    }

    private bool HasAnyPermission(CCSPlayerController player, string flags)
    {
        if (string.IsNullOrEmpty(flags)) return true;

        var flagList = flags.Split(';').Select(f => f.Trim()).Where(f => !string.IsNullOrEmpty(f)).ToList();

        if (flagList.Count == 0) return true;

        foreach (var flag in flagList)
        {
            if (AdminManager.PlayerHasPermissions(player, flag))
                return true;
        }

        return false;
    }

    private void HandleExecuteCommand(CCSPlayerController? player, CommandInfo info, CommandDefinition cmd)
    {
        if (string.IsNullOrEmpty(cmd.Execute))
        {
            info.ReplyToCommand("Çalıştırılacak komut tanımlanmamış.");
            return;
        }

        string? arg1 = cmd.Args >= 1 && info.ArgCount >= 2 ? info.GetArg(1) : null;
        string? arg2 = cmd.Args >= 2 && info.ArgCount >= 3 ? info.GetArg(2) : null;
        string? arg3 = cmd.Args >= 3 && info.ArgCount >= 4 ? info.GetArg(3) : null;

        string executeCmd = FormatMessage(cmd.Execute, player, null, arg1, arg2, arg3);
        Server.ExecuteCommand(executeCmd);
    }

    private void ExecuteCommandActions(CCSPlayerController? player, CCSPlayerController target, CommandDefinition cmd, string? arg1, string? arg2, string? arg3)
    {
        if (cmd.Announce && player != null)
        {
            var commandName = cmd.Command.Split(';')[0];
            Server.PrintToChatAll($" {CC.Orchid}{player.PlayerName}{CC.Default}, {CC.Gold}{commandName}{CC.Default} komutunu kullandı.");
        }

        if (!string.IsNullOrEmpty(cmd.Chat))
        {
            var chatMessage = FormatMessage(cmd.Chat, player, target, arg1, arg2, arg3);
            if (chatMessage.Length > 0 && chatMessage[0] != ' ')
            {
                chatMessage = " " + chatMessage;
            }
            if (player != null)
            {
                player.PrintToChat(chatMessage);
            }
        }

        if (!string.IsNullOrEmpty(cmd.Center) && player != null)
        {
            var centerMessage = FormatMessage(cmd.Center, player, target, arg1, arg2, arg3);
            _playerCenter[player.SteamID] = (centerMessage, Server.CurrentTime + cmd.CenterTime);
        }

        if (!string.IsNullOrEmpty(cmd.ServerChat))
        {
            var serverChatMessage = FormatMessage(cmd.ServerChat, player, target, arg1, arg2, arg3);
            if (serverChatMessage.Length > 0 && serverChatMessage[0] != ' ')
            {
                serverChatMessage = " " + serverChatMessage;
            }
            Server.PrintToChatAll(serverChatMessage);
        }

        if (!string.IsNullOrEmpty(cmd.ServerCenter))
        {
            var message = FormatMessage(cmd.ServerCenter, player, target, arg1, arg2, arg3);
            foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid))
            {
                p.PrintToCenterHtml(message);
            }
        }

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

        var controller = pawn.Controller.Value?.As<CCSPlayerController>();

        var random = new Random();
        var vel = new Vector(pawn.AbsVelocity.X, pawn.AbsVelocity.Y, pawn.AbsVelocity.Z);

        vel.X += (random.Next(180) + 50) * (random.Next(2) == 1 ? -1 : 1);
        vel.Y += (random.Next(180) + 50) * (random.Next(2) == 1 ? -1 : 1);
        vel.Z += random.Next(200) + 100;

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

    private HookResult OnTakeDamage(DynamicHook h)
    {
        if (_godmodePlayers.Count == 0)
            return HookResult.Continue;

        var victimEnt = h.GetParam<CEntityInstance>(0);
        if (victimEnt == null)
            return HookResult.Continue;

        var victimPawn = victimEnt as CCSPlayerPawn ?? new CCSPlayerPawn(victimEnt.Handle);
        var victimController = victimPawn?.OriginalController.Value;

        if (victimController == null)
            return HookResult.Continue;

        if (_godmodePlayers.Contains(victimController.SteamID))
        {
            var info = h.GetParam<CTakeDamageInfo>(1);
            if (info != null)
            {
                info.Damage = 0f;
                return HookResult.Changed;
            }
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
                errorMsg = "Argüman bir sayı olmalıdır.";
                return false;
            }

            if (min.HasValue && value < min.Value)
            {
                errorMsg = $"Sayı en az {min.Value} olmalıdır.";
                return false;
            }

            if (max.HasValue && value > max.Value)
            {
                errorMsg = $"Sayı en fazla {max.Value} olabilir.";
                return false;
            }
        }
        else if (argType == "word")
        {
            if (maxLength.HasValue && arg.Length > maxLength.Value)
            {
                errorMsg = $"Kelime uzunluğu en fazla {maxLength.Value} karakter olabilir.";
                return false;
            }
        }

        return true;
    }

    private CCSPlayerController? FindTarget(string search)
    {
        search = search.ToLower();

        if (search.StartsWith("#"))
        {
            if (int.TryParse(search.Substring(1), out int userid))
            {
                return Utilities.GetPlayerFromUserid(userid);
            }
        }

        var players = Utilities.GetPlayers().Where(p => p.IsValid).ToList();

        return players.FirstOrDefault(p => p.PlayerName.ToLower() == search) ?? players.FirstOrDefault(p => p.PlayerName.ToLower().Contains(search));
    }

    private string FormatMessage(string message, CCSPlayerController? player, CCSPlayerController? target, string? arg1, string? arg2, string? arg3)
    {
        if (player != null)
        {
            message = message.Replace("[PLAYER]", player.PlayerName);
            message = message.Replace("[PLAYERNAME]", player.PlayerName);

            if (player.PlayerPawn.Value?.AbsOrigin != null)
            {
                var pos = player.PlayerPawn.Value.AbsOrigin;
                message = message.Replace("[PLAYERCOORDINATE]", $"{pos.X} {pos.Y} {pos.Z}");
            }
        }

        if (target != null)
        {
            message = message.Replace("[TARGET]", target.PlayerName);
            message = message.Replace("[PLAYER/TARGET]", target.PlayerName);

            if (target.PlayerPawn.Value != null)
            {
                message = message.Replace("[TARGETHEALTH]", target.PlayerPawn.Value.Health.ToString());

                if (target.PlayerPawn.Value.AbsOrigin != null)
                {
                    var pos = target.PlayerPawn.Value.AbsOrigin;
                    message = message.Replace("[TARGETCOORDINATE]", $"{pos.X} {pos.Y} {pos.Z}");
                }
            }

            message = message.Replace("[TARGETTEAM]", target.TeamNum.ToString());
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

        var randomPlayer = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && p.PawnIsAlive).OrderBy(x => Random.Shared.Next()).FirstOrDefault();
        if (randomPlayer != null)
        {
            message = message.Replace("[RANDOMPLAYER]", randomPlayer.PlayerName);
        }

        message = message.Replace("[SERVERIP]", ConVar.Find("ip")?.StringValue ?? "unknown");
        message = message.Replace("[SERVERPORT]", ConVar.Find("hostport")?.GetPrimitiveValue<int>().ToString() ?? "27015");
        message = message.Replace("[HOSTNAME]", ConVar.Find("hostname")?.StringValue ?? "unknown");
        message = message.Replace("[MAPNAME]", Server.MapName);
        message = message.Replace("[PLAYERCOUNT]", Utilities.GetPlayers().Count(p => p.IsValid && !p.IsBot).ToString());
        message = message.Replace("[ALIVECOUNT]", Utilities.GetPlayers().Count(p => p.IsValid && p.PawnIsAlive).ToString());
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
