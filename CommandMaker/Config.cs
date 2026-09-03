using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace CommandMaker;

public class CommandMakerConfig : BasePluginConfig
{
  [JsonPropertyName("ConfigPath")]
  public string ConfigPath { get; set; } = "commands.json";

  [JsonPropertyName("reload_cmd")]
  public string ReloadCommands { get; set; } = "css_cm_reload,css_commandmaker_reload";

  [JsonPropertyName("reload_flag")]
  public string ReloadFlag { get; set; } = "@css/root";

  [JsonPropertyName("list_cmd")]
  public string ListCommands { get; set; } = "css_cmdlist,css_komutlar";
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


  [JsonPropertyName("arg1_list")]
  public string? Arg1List { get; set; }

  [JsonPropertyName("arg1_default")]
  public string? Arg1Default { get; set; }

  [JsonPropertyName("arg2_list")]
  public string? Arg2List { get; set; }

  [JsonPropertyName("arg2_default")]
  public string? Arg2Default { get; set; }

  [JsonPropertyName("arg3_list")]
  public string? Arg3List { get; set; }

  [JsonPropertyName("arg3_default")]
  public string? Arg3Default { get; set; }

  [JsonPropertyName("flag")]
  [JsonConverter(typeof(StringOrArrayConverter))]
  public List<string>? Flag { get; set; }

  [JsonPropertyName("target_flag")]
  [JsonConverter(typeof(StringOrArrayConverter))]
  public List<string>? TargetFlag { get; set; }

  [JsonPropertyName("ignore_immunity")]
  public bool IgnoreImmunity { get; set; } = false;

  [JsonPropertyName("team_filter")]
  public string? TeamFilter { get; set; }

  [JsonPropertyName("alive_filter")]
  public string? AliveFilter { get; set; }

  [JsonPropertyName("cooldown")]
  public float Cooldown { get; set; } = 0f;

  [JsonPropertyName("global_cooldown")]
  public float GlobalCooldown { get; set; } = 0f;

  [JsonPropertyName("uses_per_round")]
  public int UsesPerRound { get; set; } = 0;

  [JsonPropertyName("min_players")]
  public int MinPlayers { get; set; } = 0;

  [JsonPropertyName("warmup_only")]
  public bool WarmupOnly { get; set; } = false;

  [JsonPropertyName("no_warmup")]
  public bool NoWarmup { get; set; } = false;

  [JsonPropertyName("description")]
  public string? Description { get; set; }

  [JsonPropertyName("menu_title")]
  public string? MenuTitle { get; set; }

  [JsonPropertyName("menu")]
  public List<MenuEntry>? Menu { get; set; }

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

  [JsonPropertyName("setangle")]
  public string? SetAngle { get; set; }

  [JsonPropertyName("addhealth")]
  public string? AddHealth { get; set; }

  [JsonPropertyName("addarmor")]
  public string? AddArmor { get; set; }

  [JsonPropertyName("addmoney")]
  public string? AddMoney { get; set; }

  [JsonPropertyName("setclantag")]
  public string? SetClanTag { get; set; }

  [JsonPropertyName("dropweapon")]
  public string? DropWeapon { get; set; }

  [JsonPropertyName("screencolor")]
  public string? ScreenColor { get; set; }

  [JsonPropertyName("emitsound")]
  public string? EmitSound { get; set; }

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

  [JsonPropertyName("targetchat")]
  [JsonConverter(typeof(StringOrArrayConverter))]
  public List<string>? TargetChat { get; set; }

  [JsonPropertyName("targetcenter")]
  public string? TargetCenter { get; set; }

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

  [JsonIgnore]
  public string Key { get; set; } = "";

  [JsonIgnore]
  public List<CompiledAction> Compiled { get; } = new();

  [JsonIgnore]
  public int ArgCount => Math.Clamp(Args, 0, 3);

  [JsonIgnore]
  public string FlagText => _flagText ??= FlagsToText(Flag);

  [JsonIgnore]
  public string TargetFlagText => _targetFlagText ??= FlagsToText(TargetFlag);

  [JsonIgnore]
  public bool HasTargetFlag => TargetFlagText.Length > 0;

  private string? _flagText;
  private string? _targetFlagText;

  private static string FlagsToText(List<string>? flags)
  {
    if (flags is not { Count: > 0 })
      return "";

    return string.Join(',', flags.Select(f => f.Replace(';', ',')))
      .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
      .Aggregate("", (a, b) => a.Length == 0 ? b : a + "," + b);
  }

  public (string? Type, int? Min, int? Max, int? Length, string? List, string? Default) ArgSpec(int index) => index switch
  {
    0 => (Arg1, Arg1NumberMin, Arg1NumberMax, Arg1WordLength, Arg1List, Arg1Default),
    1 => (Arg2, Arg2NumberMin, Arg2NumberMax, Arg2WordLength, Arg2List, Arg2Default),
    2 => (Arg3, Arg3NumberMin, Arg3NumberMax, Arg3WordLength, Arg3List, Arg3Default),
    _ => (null, null, null, null, null, null)
  };
}

public class MenuEntry
{
  [JsonPropertyName("text")]
  public string Text { get; set; } = "";

  [JsonPropertyName("command")]
  public string Command { get; set; } = "";

  [JsonPropertyName("flag")]
  [JsonConverter(typeof(StringOrArrayConverter))]
  public List<string>? Flag { get; set; }

  [JsonPropertyName("close")]
  public bool Close { get; set; } = true;

  [JsonIgnore]
  public string FlagText => _flagText ??= Flag is { Count: > 0 }
    ? string.Join(',', Flag.Select(f => f.Replace(';', ',')))
    : "";

  private string? _flagText;
}

public sealed class CompiledAction
{
  public readonly string Value;
  public readonly Action<ActionContext> Run;

  public CompiledAction(string value, Action<ActionContext> run)
  {
    Value = value;
    Run = run;
  }
}

public class CommandsConfig
{
  [JsonPropertyName("Commands")]
  public List<CommandDefinition> Commands { get; set; } = new();
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
