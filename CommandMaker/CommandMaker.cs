using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using static CounterStrikeSharp.API.Core.Listeners;

namespace CommandMaker;

public partial class CommandMaker : BasePlugin, IPluginConfig<CommandMakerConfig>
{
  public override string ModuleName => "CommandMaker";
  public override string ModuleVersion => "1.0.8";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public CommandMakerConfig Config { get; set; } = new();

  private static readonly JsonSerializerOptions JsonOpts = new()
  {
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true
  };

  private CommandsConfig? _commandsConfig;
  private readonly Dictionary<string, CommandDefinition> _registeredCommands = new();
  private readonly Dictionary<string, CommandInfo.CommandCallback> _commandCallbacks = new();
  private readonly Dictionary<int, PlayerState> _states = new();
  private readonly Dictionary<string, float> _globalCooldowns = new(StringComparer.Ordinal);
  private WasdMenuManager _menus = null!;
  private readonly List<int> _expired = new();
  private readonly HashSet<string> _models = new(StringComparer.OrdinalIgnoreCase);
  private int _loadWarnings;
  private int _godmodeCount;

  internal sealed class PlayerState
  {
    public CCSPlayerController? Controller;
    public float Speed = 1f;
    public float Gravity = 1f;
    public bool HasSpeed;
    public bool HasGravity;
    public bool Godmode;
    public string? Center;
    public float CenterEnd;
    public Dictionary<string, float>? Cooldowns;
    public Dictionary<string, int>? RoundUses;

    public bool NeedsTick => HasSpeed || HasGravity || Center != null;

    public bool IsEmpty => !NeedsTick && !Godmode
      && (Cooldowns == null || Cooldowns.Count == 0)
      && (RoundUses == null || RoundUses.Count == 0);
  }

  internal PlayerState State(int userId)
  {
    if (!_states.TryGetValue(userId, out var state))
    {
      state = new PlayerState();
      _states[userId] = state;
    }

    return state;
  }

  internal void SetGodmode(int userId, bool enabled)
  {
    var state = State(userId);
    if (state.Godmode == enabled)
      return;

    state.Godmode = enabled;
    _godmodeCount += enabled ? 1 : -1;

    if (_godmodeCount < 0)
      _godmodeCount = 0;
  }

  private string Message(string text) => $" {CC.Orchid}{ChatPrefix}{CC.Default} {text}";

  public void OnConfigParsed(CommandMakerConfig config)
  {
    Config = config;
    LoadCommands();
  }

  public override void Load(bool hotReload)
  {
    base.Load(hotReload);

    _menus = new WasdMenuManager(this,
      () => Localizer["menu.scroll"],
      () => Localizer["menu.select"],
      () => Localizer["menu.exit"]);

    RegisterListener<OnServerPrecacheResources>(OnPrecacheResources);
    HudGuard.Install(this);
    RegisterListener<OnTick>(OnTick);
    RegisterListener<OnEntityTakeDamagePre>(OnEntityDamage);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
    RegisterEventHandler<EventRoundStart>(OnRoundStart);
    RegisterListener<OnMapStart>(OnMapStart);

    foreach (var name in Util.Split(Config.ReloadCommands))
      AddCommand(name, "Reloads the CommandMaker command definitions", OnReloadCommand);

    foreach (var name in Util.Split(Config.ListCommands))
      AddCommand(name, "Lists the commands you may use", OnListCommand);
  }

  public override void Unload(bool hotReload)
  {
    _menus.Clear();
    UnregisterDynamicCommands();
    ResetTrackedPlayers();
  }

  private void UnregisterDynamicCommands()
  {
    foreach (var kvp in _commandCallbacks)
      RemoveCommand(kvp.Key, kvp.Value);

    _registeredCommands.Clear();
    _commandCallbacks.Clear();
  }

  private void OnMapStart(string mapName)
  {
    ClearEntityCache();
    _menus.Clear();
    ResetTrackedPlayers();
  }

  private void ResetTrackedPlayers()
  {
    foreach (var (userId, state) in _states)
    {
      if (!state.HasSpeed && !state.HasGravity)
        continue;

      var pawn = Util.FromUserId(userId)?.PlayerPawn.Value;
      if (pawn == null || !pawn.IsValid)
        continue;

      pawn.VelocityModifier = 1.0f;
      Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
      pawn.ActualGravityScale = 1.0f;
    }

    _states.Clear();
    _godmodeCount = 0;
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
  {
    foreach (var state in _states.Values)
      state.RoundUses = null;

    return HookResult.Continue;
  }

  private void OnListCommand(CCSPlayerController? player, CommandInfo info)
  {
    var lines = new List<string>();

    foreach (var (name, cmd) in _registeredCommands)
    {
      if (name != cmd.Key)
        continue;

      if (player != null && !Util.HasAccess(player, cmd.FlagText))
        continue;

      var aliases = _registeredCommands
        .Where(kvp => ReferenceEquals(kvp.Value, cmd) && kvp.Key != cmd.Key)
        .Select(kvp => kvp.Key)
        .ToList();

      string title = aliases.Count > 0 ? $"{name} ({string.Join(", ", aliases)})" : name;
      string description = string.IsNullOrEmpty(cmd.Description) ? "" : $" {CC.Default}- {cmd.Description}";

      lines.Add($" {CC.Gold}{title}{description}");
    }

    if (lines.Count == 0)
    {
      info.ReplyToCommand(Message(Localizer["commandmaker.list_empty"]));
      return;
    }

    info.ReplyToCommand(Message(Localizer["commandmaker.list_title", lines.Count]));
    foreach (var line in lines)
      info.ReplyToCommand(line);
  }

  private void OnReloadCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player != null && !Util.HasAccess(player, Config.ReloadFlag))
    {
      info.ReplyToCommand(Message(Localizer["commandmaker.no_permission"]));
      return;
    }

    LoadCommands();
    info.ReplyToCommand(Message(Localizer["commandmaker.commands_loaded", _registeredCommands.Count]));

    if (_loadWarnings > 0)
      info.ReplyToCommand(Message(Localizer["commandmaker.load_warnings", _loadWarnings]));
  }

  private void LoadCommands()
  {
    try
    {
      string configPath = Path.Combine(ModuleDirectory, Config.ConfigPath);

      if (!File.Exists(configPath))
        CreateDefaultCommandsJson(configPath);

      _commandsConfig = JsonSerializer.Deserialize<CommandsConfig>(File.ReadAllText(configPath), JsonOpts);

      if (_commandsConfig == null || _commandsConfig.Commands == null)
        return;

      UnregisterDynamicCommands();
      _loadWarnings = 0;
      _models.Clear();

      foreach (var cmd in _commandsConfig.Commands)
      {
        cmd.Key = "";

        if (!ValidateDefinition(cmd))
          continue;

        CompileActions(cmd);
        CollectModels(cmd);
        PrecompileMessages(cmd);
        RegisterDynamicCommand(cmd);
      }
    }
    catch (Exception ex)
    {
      Logger.LogError("CommandMaker: komut dosyasi yuklenemedi. {0}", ex.Message);
    }
  }

  private static readonly HashSet<string> KnownTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "default", "target", "playertarget", "execute", "menu"
  };

  private void Warn(string text, params object[] args)
  {
    _loadWarnings++;
    Logger.LogWarning("CommandMaker: " + text, args);
  }

  private bool ValidateDefinition(CommandDefinition cmd)
  {
    if (cmd.Command is not { Count: > 0 })
    {
      Warn("adi olmayan bir tanim atlandi.");
      return false;
    }

    string first = cmd.Command[0];

    if (!KnownTypes.Contains(cmd.Type))
    {
      Warn("'{0}' tanimindaki '{1}' tipi bilinmiyor, atlandi.", first, cmd.Type);
      return false;
    }

    if (cmd.Args != cmd.ArgCount)
      Warn("'{0}' tanimindaki args degeri {1}, 0-3 araligina kisitlandi.", first, cmd.Args);

    if (cmd.Flag is { Count: > 0 } && cmd.FlagText.Length == 0)
      Warn("'{0}' tanimindaki flag listesi bos, komut herkese acik.", first);

    if (cmd.TargetFlag is { Count: > 0 } && cmd.TargetFlagText.Length == 0)
      Warn("'{0}' tanimindaki target_flag listesi bos, yok sayildi.", first);

    if (cmd.Type.Equals("menu", StringComparison.OrdinalIgnoreCase) && cmd.Menu is not { Count: > 0 })
      Warn("'{0}' menu tipinde ama menu girdisi tanimlanmamis.", first);

    if (cmd.Type.Equals("execute", StringComparison.OrdinalIgnoreCase)
        && cmd.Execute is not { Count: > 0 } && cmd.SetCvar is not { Count: > 0 })
      Warn("'{0}' execute tipinde ama execute/setcvar tanimlanmamis.", first);

    for (int i = 0; i < cmd.ArgCount; i++)
    {
      var spec = cmd.ArgSpec(i);
      if (spec.Type is not (null or "number" or "float" or "word" or "list" or "player"))
        Warn("'{0}' tanimindaki arg{1} tipi '{2}' bilinmiyor, dogrulama yapilmayacak.", first, i + 1, spec.Type);
    }

    return true;
  }

  private void OnPrecacheResources(ResourceManifest manifest)
  {
    foreach (var model in _models)
      manifest.AddResource(model);
  }

  private void CollectModels(CommandDefinition cmd)
  {
    if (string.IsNullOrEmpty(cmd.SetModel))
      return;

    string template = ActionValue(cmd.SetModel);

    if (!template.Contains('['))
    {
      _models.Add(template);
      return;
    }

    for (int i = 0; i < cmd.ArgCount; i++)
    {
      var spec = cmd.ArgSpec(i);
      string token = $"[ARG{i + 1}]";

      if (spec.List == null || !template.Contains(token, StringComparison.OrdinalIgnoreCase))
        continue;

      foreach (var value in Util.Split(spec.List))
        _models.Add(template.Replace(token, value, StringComparison.OrdinalIgnoreCase));
    }
  }

  private void PrecompileMessages(CommandDefinition cmd)
  {
    PrecompileTemplates(cmd.Chat);
    PrecompileTemplates(cmd.Console);
    PrecompileTemplates(cmd.ServerChat);
    PrecompileTemplates(cmd.Execute);
    PrecompileTemplates(cmd.SetCvar);
    PrecompileTemplate(cmd.MenuTitle);

    if (cmd.Menu != null)
    {
      foreach (var entry in cmd.Menu)
        PrecompileTemplate(entry.Text);
    }

    PrecompileTemplates(cmd.TargetChat);
    PrecompileTemplate(cmd.Center);
    PrecompileTemplate(cmd.TargetCenter);
    PrecompileTemplate(cmd.ServerCenter);
  }

  private void RegisterDynamicCommand(CommandDefinition cmd)
  {
    foreach (var entry in cmd.Command)
    {
      foreach (var commandName in entry.Split(';'))
      {
        var trimmedName = commandName.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(trimmedName))
          continue;

        if (_registeredCommands.ContainsKey(trimmedName))
        {
          Warn("'{0}' komutu birden fazla tanimda var, ikincisi atlandi.", trimmedName);
          continue;
        }

        if (!trimmedName.StartsWith("css_"))
          Warn("'{0}' komutu css_ ile baslamiyor, sohbetten calistirilamaz.", trimmedName);

        if (cmd.Key.Length == 0)
          cmd.Key = trimmedName;

        _registeredCommands[trimmedName] = cmd;

        CommandInfo.CommandCallback callback = (player, info) => HandleDynamicCommand(player, info, cmd);

        _commandCallbacks[trimmedName] = callback;
        AddCommand(trimmedName, $"Dynamic command: {trimmedName}", callback);
      }
    }
  }

  private void HandleDynamicCommand(CCSPlayerController? player, CommandInfo info, CommandDefinition cmd)
  {
    if (player != null)
    {
      if (!Util.HasAccess(player, cmd.FlagText))
      {
        info.ReplyToCommand(Message(Localizer["commandmaker.no_permission"]));
        return;
      }

      if (!string.IsNullOrEmpty(cmd.TeamFilter))
      {
        var required = cmd.TeamFilter.Equals("CT", StringComparison.OrdinalIgnoreCase)
          ? CsTeam.CounterTerrorist
          : CsTeam.Terrorist;

        if (player.Team != required)
        {
          info.ReplyToCommand(Message(Localizer["commandmaker.team_only", cmd.TeamFilter.ToUpper()]));
          return;
        }
      }

      if (!string.IsNullOrEmpty(cmd.AliveFilter))
      {
        bool mustBeAlive = cmd.AliveFilter.Equals("alive", StringComparison.OrdinalIgnoreCase);
        if (player.PawnIsAlive != mustBeAlive)
        {
          info.ReplyToCommand(Message(mustBeAlive
            ? Localizer["commandmaker.alive_only"]
            : Localizer["commandmaker.dead_only"]));
          return;
        }
      }

      if (!CheckLimits(player, info, cmd))
        return;
    }

    bool executed = cmd.Type.ToLowerInvariant() switch
    {
      "default" => HandleDefaultCommand(player, info, cmd),
      "target" => HandleTargetCommand(player, info, cmd),
      "playertarget" => HandlePlayerTargetCommand(player, info, cmd),
      "execute" => HandleExecuteCommand(player, info, cmd),
      "menu" => HandleMenuCommand(player, info, cmd),
      _ => UnknownType(info, cmd)
    };

    if (executed && player != null)
      RecordUse(player, cmd);
  }

  private bool CheckLimits(CCSPlayerController player, CommandInfo info, CommandDefinition cmd)
  {
    float now = Server.CurrentTime;

    if (cmd.WarmupOnly && GameRules?.WarmupPeriod != true)
    {
      info.ReplyToCommand(Message(Localizer["commandmaker.warmup_only"]));
      return false;
    }

    if (cmd.NoWarmup && GameRules?.WarmupPeriod == true)
    {
      info.ReplyToCommand(Message(Localizer["commandmaker.no_warmup"]));
      return false;
    }

    if (cmd.MinPlayers > 0)
    {
      EnsureCounts();
      if (_cHumans < cmd.MinPlayers)
      {
        info.ReplyToCommand(Message(Localizer["commandmaker.min_players", cmd.MinPlayers]));
        return false;
      }
    }

    var state = State(Util.UserId(player));

    if (cmd.Cooldown > 0f
        && state.Cooldowns != null
        && state.Cooldowns.TryGetValue(cmd.Key, out float lastUse)
        && now < lastUse + cmd.Cooldown)
    {
      info.ReplyToCommand(Message(Localizer["commandmaker.cooldown_wait", (int)MathF.Ceiling(lastUse + cmd.Cooldown - now)]));
      return false;
    }

    if (cmd.GlobalCooldown > 0f
        && _globalCooldowns.TryGetValue(cmd.Key, out float lastGlobal)
        && now < lastGlobal + cmd.GlobalCooldown)
    {
      info.ReplyToCommand(Message(Localizer["commandmaker.global_cooldown_wait", (int)MathF.Ceiling(lastGlobal + cmd.GlobalCooldown - now)]));
      return false;
    }

    if (cmd.UsesPerRound > 0
        && state.RoundUses != null
        && state.RoundUses.TryGetValue(cmd.Key, out int used)
        && used >= cmd.UsesPerRound)
    {
      info.ReplyToCommand(Message(Localizer["commandmaker.round_limit", cmd.UsesPerRound]));
      return false;
    }

    return true;
  }

  private void RecordUse(CCSPlayerController player, CommandDefinition cmd)
  {
    float now = Server.CurrentTime;

    if (cmd.GlobalCooldown > 0f)
      _globalCooldowns[cmd.Key] = now;

    if (cmd.Cooldown <= 0f && cmd.UsesPerRound <= 0)
      return;

    var state = State(Util.UserId(player));

    if (cmd.Cooldown > 0f)
    {
      state.Cooldowns ??= new Dictionary<string, float>(StringComparer.Ordinal);
      state.Cooldowns[cmd.Key] = now;
    }

    if (cmd.UsesPerRound > 0)
    {
      state.RoundUses ??= new Dictionary<string, int>(StringComparer.Ordinal);
      state.RoundUses.TryGetValue(cmd.Key, out int used);
      state.RoundUses[cmd.Key] = used + 1;
    }
  }

  private bool UnknownType(CommandInfo info, CommandDefinition cmd)
  {
    info.ReplyToCommand(Message(Localizer["commandmaker.unknown_type", cmd.Type]));
    return false;
  }

  private bool HandleDefaultCommand(CCSPlayerController? player, CommandInfo info, CommandDefinition cmd)
  {
    SendCommandMessages(player, null, cmd, null, null, null, null, info.GetArg(0));
    RunServerLines(cmd.Execute, player, null, null, null, null, null);
    RunServerLines(cmd.SetCvar, player, null, null, null, null, null);
    return true;
  }

  private bool HandleMenuCommand(CCSPlayerController? player, CommandInfo info, CommandDefinition cmd)
  {
    if (player == null || !player.IsValid)
    {
      info.ReplyToCommand(Message(Localizer["commandmaker.player_only"]));
      return false;
    }

    if (cmd.Menu is not { Count: > 0 })
    {
      info.ReplyToCommand(Message(Localizer["commandmaker.menu_undefined"]));
      return false;
    }

    var items = new List<WasdItem>();

    foreach (var entry in cmd.Menu)
    {
      if (entry.Text.Length == 0 || entry.Command.Length == 0)
        continue;

      if (!Util.HasAccess(player, entry.FlagText))
        continue;

      string command = entry.Command;
      bool close = entry.Close;

      items.Add(new WasdItem
      {
        Text = FormatMessage(entry.Text, player, null, null, null, null),
        OnSelect = p =>
        {
          p.ExecuteClientCommandFromServer(SanitizeCommandValue(command));

          if (close)
            _menus.Close(p);
        }
      });
    }

    if (items.Count == 0)
    {
      info.ReplyToCommand(Message(Localizer["commandmaker.menu_empty"]));
      return false;
    }

    string title = string.IsNullOrEmpty(cmd.MenuTitle)
      ? cmd.Key
      : FormatMessage(cmd.MenuTitle, player, null, null, null, null);

    _menus.Open(player, title, items);
    SendCommandMessages(player, null, cmd, null, null, null, null, info.GetArg(0));
    return true;
  }

  private string BuildUsage(CommandDefinition cmd, string name, bool targeted)
  {
    string text = targeted
      ? $"{Localizer["commandmaker.usage_player", name]}"
      : $"{Localizer["commandmaker.usage", name]}";

    for (int i = 1; i <= cmd.ArgCount; i++)
      text += $" <arg{i}>";

    return text;
  }

  private bool TryReadArgs(CCSPlayerController? player, CommandInfo info, CommandDefinition cmd, int firstIndex, bool targeted, out string?[] args)
  {
    args = new string?[3];

    for (int i = 0; i < cmd.ArgCount; i++)
    {
      var spec = cmd.ArgSpec(i);
      string value;

      if (info.ArgCount < firstIndex + i + 1)
      {
        if (spec.Default == null)
        {
          info.ReplyToCommand(Message(BuildUsage(cmd, info.GetArg(0), targeted)));
          return false;
        }

        value = spec.Default;
      }
      else
      {
        value = info.GetArg(firstIndex + i);
      }

      if (!ValidateArgument(value, player, spec, out string error))
      {
        info.ReplyToCommand(error);
        return false;
      }

      args[i] = value;
    }

    return true;
  }

  private bool HandleTargetCommand(CCSPlayerController? player, CommandInfo info, CommandDefinition cmd)
  {
    if (info.ArgCount < 2)
    {
      info.ReplyToCommand(Message(BuildUsage(cmd, info.GetArg(0), true)));
      return false;
    }

    var targetName = info.GetArg(1);
    var targets = FindTargets(targetName, player, out var groupLabel);

    if (targets.Count == 0)
    {
      info.ReplyToCommand(Message(Localizer["commandmaker.player_not_found", targetName]));
      return false;
    }

    if (!FilterTargets(player, info, cmd, targets, false))
      return false;

    if (!TryReadArgs(player, info, cmd, 2, true, out var args))
      return false;

    string label = targets.Count == 1 ? targets[0].PlayerName : groupLabel ?? targets[0].PlayerName;

    foreach (var target in targets)
      ApplyCommandActions(player, target, cmd, args[0], args[1], args[2]);

    SendCommandMessages(player, targets[0], cmd, args[0], args[1], args[2], label, info.GetArg(0), targets);
    return true;
  }

  private bool HandlePlayerTargetCommand(CCSPlayerController? player, CommandInfo info, CommandDefinition cmd)
  {
    List<CCSPlayerController> targets;
    string? groupLabel = null;
    string?[] args;

    if (info.ArgCount >= 2)
    {
      var targetName = info.GetArg(1);
      targets = FindTargets(targetName, player, out groupLabel);

      if (targets.Count == 0)
      {
        info.ReplyToCommand(Message(Localizer["commandmaker.player_not_found", targetName]));
        return false;
      }

      if (!FilterTargets(player, info, cmd, targets, true))
        return false;

      if (!TryReadArgs(player, info, cmd, 2, true, out args))
        return false;
    }
    else
    {
      if (player == null)
      {
        info.ReplyToCommand(Message(Localizer["commandmaker.player_only"]));
        return false;
      }

      targets = new List<CCSPlayerController> { player };

      if (!TryReadArgs(player, info, cmd, 1, true, out args))
        return false;
    }

    string label = targets.Count == 1 ? targets[0].PlayerName : groupLabel ?? targets[0].PlayerName;

    foreach (var target in targets)
      ApplyCommandActions(player, target, cmd, args[0], args[1], args[2]);

    SendCommandMessages(player, targets[0], cmd, args[0], args[1], args[2], label, info.GetArg(0), targets);
    return true;
  }

  private bool HandleExecuteCommand(CCSPlayerController? player, CommandInfo info, CommandDefinition cmd)
  {
    if (cmd.Execute is not { Count: > 0 } && cmd.SetCvar is not { Count: > 0 })
    {
      info.ReplyToCommand(Message(Localizer["commandmaker.execute_undefined"]));
      return false;
    }

    if (!TryReadArgs(player, info, cmd, 1, false, out var args))
      return false;

    RunServerLines(cmd.Execute, player, null, args[0], args[1], args[2], null);
    RunServerLines(cmd.SetCvar, player, null, args[0], args[1], args[2], null);
    SendCommandMessages(player, null, cmd, args[0], args[1], args[2], null, info.GetArg(0));
    return true;
  }

  private bool FilterTargets(CCSPlayerController? player, CommandInfo info, CommandDefinition cmd, List<CCSPlayerController> targets, bool requireTargetFlag)
  {
    if (player == null)
      return true;

    bool othersTargeted = targets.Any(t => t.Slot != player.Slot);

    if (othersTargeted)
    {
      if (requireTargetFlag && !cmd.HasTargetFlag)
      {
        info.ReplyToCommand(Message(Localizer["commandmaker.self_only"]));
        return false;
      }

      if (cmd.HasTargetFlag && !Util.HasAccess(player, cmd.TargetFlagText))
      {
        info.ReplyToCommand(Message(Localizer["commandmaker.no_target_permission"]));
        return false;
      }
    }

    if (!cmd.IgnoreImmunity)
    {
      int before = targets.Count;
      targets.RemoveAll(t => t.Slot != player.Slot && !AdminManager.CanPlayerTarget(player, t));

      if (targets.Count == 0)
      {
        info.ReplyToCommand(Message(Localizer["commandmaker.target_immune"]));
        return false;
      }

      if (targets.Count != before)
        info.ReplyToCommand(Message(Localizer["commandmaker.target_immune_partial", before - targets.Count]));
    }

    return true;
  }

  private void SendCommandMessages(CCSPlayerController? player, CCSPlayerController? target, CommandDefinition cmd, string? arg1, string? arg2, string? arg3, string? targetLabel, string commandName, List<CCSPlayerController>? targets = null)
  {
    if (targets != null && (cmd.TargetChat is { Count: > 0 } || !string.IsNullOrEmpty(cmd.TargetCenter)))
    {
      foreach (var receiver in targets)
      {
        if (!receiver.IsValid || receiver.IsBot)
          continue;

        if (cmd.TargetChat is { Count: > 0 })
        {
          foreach (var line in cmd.TargetChat)
          {
            var text = FormatMessage(line, player, receiver, arg1, arg2, arg3, null);
            if (text.Length > 0 && text[0] != ' ')
              text = " " + text;

            receiver.PrintToChat(text);
          }
        }

        if (!string.IsNullOrEmpty(cmd.TargetCenter))
        {
          int receiverId = Util.UserId(receiver);
          if (receiverId < 0)
            continue;

          var state = State(receiverId);
          state.Center = FormatMessage(cmd.TargetCenter, player, receiver, arg1, arg2, arg3, null);
          state.CenterEnd = Server.CurrentTime + cmd.CenterTime;
        }
      }
    }

    if (cmd.Announce && player != null)
      Server.PrintToChatAll(Message(Localizer["commandmaker.announce", $"{CC.Orchid}{player.PlayerName}{CC.Default}", $"{CC.Gold}{commandName}{CC.Default}"]));

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
      int userId = Util.UserId(player);
      if (userId >= 0)
      {
        var state = State(userId);
        state.Center = centerMessage;
        state.CenterEnd = Server.CurrentTime + cmd.CenterTime;
      }
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
      float endTime = Server.CurrentTime + cmd.CenterTime;

      foreach (var p in Players)
      {
        int userId = Util.UserId(p);
        if (userId < 0)
          continue;

        var state = State(userId);
        state.Center = message;
        state.CenterEnd = endTime;
      }
    }
  }

  private void RunServerLines(List<string>? lines, CCSPlayerController? player, CCSPlayerController? target, string? arg1, string? arg2, string? arg3, string? targetLabel)
  {
    if (lines is not { Count: > 0 })
      return;

    foreach (var line in lines)
      Server.ExecuteCommand(FormatMessage(line, player, target, arg1, arg2, arg3, targetLabel, true));
  }

  private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player != null)
    {
      var userId = Util.UserId(player);

      if (_states.Remove(userId, out var state) && state.Godmode)
        _godmodeCount--;
    }

    return HookResult.Continue;
  }

  private void OnTick()
  {
    if (_states.Count == 0)
      return;

    float currentTime = Server.CurrentTime;
    bool hudFrame = Util.IsHudFrame();

    foreach (var (userId, state) in _states)
    {
      if (state.Center != null && currentTime >= state.CenterEnd)
        state.Center = null;

      if (state.IsEmpty)
      {
        _expired.Add(userId);
        continue;
      }

      if (!state.NeedsTick)
        continue;

      var player = state.Controller;
      if (player == null || !player.IsValid || player.UserId != userId)
      {
        player = Util.FromUserId(userId);
        state.Controller = player;
      }

      var pawn = player?.PlayerPawn.Value;
      if (player == null || pawn == null || !pawn.IsValid)
        continue;

      if (state.HasSpeed && MathF.Abs(pawn.VelocityModifier - state.Speed) > 0.001f)
      {
        pawn.VelocityModifier = state.Speed;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
      }

      if (state.HasGravity && MathF.Abs(pawn.ActualGravityScale - state.Gravity) > 0.001f)
        pawn.ActualGravityScale = state.Gravity;

      if (state.Center != null && hudFrame && !HudGuard.Blocked(player))
        player.PrintToCenterHtml(state.Center);
    }

    if (_expired.Count > 0)
    {
      foreach (var userId in _expired)
        _states.Remove(userId);

      _expired.Clear();
    }
  }

  private HookResult OnEntityDamage(CEntityInstance victimEnt, CTakeDamageInfo info)
  {
    if (_godmodeCount == 0)
      return HookResult.Continue;

    var victimController = Util.PawnController(victimEnt);
    if (victimController == null)
      return HookResult.Continue;

    if (_states.TryGetValue(Util.UserId(victimController), out var state) && state.Godmode)
      info.Damage = 0f;

    return HookResult.Continue;
  }
}
