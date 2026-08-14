using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using MySqlConnector;

namespace Ads;

public class AdsFileException : Exception
{
  public string FileName { get; }

  public AdsFileException(string path, JsonException inner)
    : base(Describe(path, inner), inner)
  {
    FileName = Path.GetFileName(path);
  }

  private static string Describe(string path, JsonException inner)
  {
    string name = Path.GetFileName(path);
    return inner.LineNumber.HasValue
      ? $"{name}: satir {inner.LineNumber + 1} - {inner.Message}"
      : $"{name}: {inner.Message}";
  }
}

public class LegacyPropModel : PropModel
{
  [JsonPropertyName("name")] public string LegacyName { get; set; } = "";
}

public class LegacyPropsData
{
  [JsonPropertyName("models")] public List<LegacyPropModel> Models { get; set; } = new();

  public PropsData Upgrade()
  {
    var data = new PropsData();

    foreach (var model in Models)
    {
      model.Name = string.IsNullOrWhiteSpace(model.LegacyName) ? model.Path : model.LegacyName;
      model.Flag = JsonText.Blank(model.Flag);
      model.IgnoreFlag = JsonText.Blank(model.IgnoreFlag);
      data.Models.Add(model);
    }

    return data;
  }
}

public class LegacyPropAd : PropAd
{
  [JsonPropertyName("map")] public string LegacyMap { get; set; } = "*";
}

public class LegacyMapsData
{
  [JsonPropertyName("props")] public List<LegacyPropAd> Props { get; set; } = new();

  public MapsData Upgrade()
  {
    var data = new MapsData();

    foreach (var ad in Props)
    {
      ad.Map = string.IsNullOrWhiteSpace(ad.LegacyMap) ? "*" : ad.LegacyMap;
      ad.Flag = JsonText.Blank(ad.Flag);
      ad.IgnoreFlag = JsonText.Blank(ad.IgnoreFlag);
      data.Props.Add(ad);
    }

    return data;
  }
}

public interface IAdsStorage
{
  void Init();
  AdsData Load();
  void Save(AdsData data);
  PropsData LoadProps();
  void SaveProps(PropsData data);
  MapsData LoadMaps();
  void SaveMaps(MapsData data);
}

public class AdsJsonStorage : IAdsStorage
{
  private readonly string _adsPath;
  private readonly string _propsPath;
  private readonly string _mapsPath;
  private readonly string _settingsPath;
  private readonly object _ioLock = new();

  private static readonly JsonSerializerOptions Opts = new()
  {
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  public AdsJsonStorage(string directory)
  {
    _adsPath = Path.Combine(directory, "ads.json");
    _propsPath = Path.Combine(directory, "props.json");
    _mapsPath = Path.Combine(directory, "maps.json");
    _settingsPath = Path.Combine(directory, "settings.json");
  }

  public string FilePath => _adsPath;
  public string PropsFilePath => _propsPath;
  public string MapsFilePath => _mapsPath;
  public string SettingsFilePath => _settingsPath;

  public void Init()
  {
    lock (_ioLock)
    {
      Directory.CreateDirectory(Path.GetDirectoryName(_adsPath)!);

      if (!File.Exists(_adsPath))
        File.WriteAllText(_adsPath, JsonSerializer.Serialize(AdsData.Sample(), Opts));

      if (!File.Exists(_propsPath))
        File.WriteAllText(_propsPath, JsonSerializer.Serialize(PropsData.Sample().ToJson(), Opts));

      if (!File.Exists(_mapsPath))
        File.WriteAllText(_mapsPath, JsonSerializer.Serialize(MapsData.Sample().ToJson(), Opts));

      if (File.Exists(_propsPath) && HasKey(File.ReadAllText(_propsPath), "models"))
        SaveProps(LoadProps());

      if (File.Exists(_mapsPath) && HasKey(File.ReadAllText(_mapsPath), "props"))
        SaveMaps(LoadMaps());
    }
  }

  public AdsConfig LoadSettings()
  {
    lock (_ioLock)
    {
      Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);

      if (!File.Exists(_settingsPath))
      {
        var created = new AdsConfig();
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(created, Opts));
        return created;
      }

      AdsConfig config;
      try
      {
        config = JsonSerializer.Deserialize<AdsConfig>(File.ReadAllText(_settingsPath)) ?? new AdsConfig();
      }
      catch (JsonException ex)
      {
        throw new AdsFileException(_settingsPath, ex);
      }

      File.WriteAllText(_settingsPath, JsonSerializer.Serialize(config, Opts));
      return config;
    }
  }

  public AdsData Load() => Read<AdsData>(_adsPath);
  public void Save(AdsData data) => Write(_adsPath, data);

  public PropsData LoadProps()
  {
    lock (_ioLock)
    {
      if (!File.Exists(_propsPath))
        return new PropsData();

      string json = File.ReadAllText(_propsPath);

      try
      {
        if (HasKey(json, "models"))
          return JsonSerializer.Deserialize<LegacyPropsData>(json)?.Upgrade() ?? new PropsData();

        var raw = JsonSerializer.Deserialize<Dictionary<string, PropModel>>(json);
        return raw == null ? new PropsData() : PropsData.FromJson(raw);
      }
      catch (JsonException ex)
      {
        throw new AdsFileException(_propsPath, ex);
      }
    }
  }

  public void SaveProps(PropsData data) => Write(_propsPath, data.ToJson());

  public MapsData LoadMaps()
  {
    lock (_ioLock)
    {
      if (!File.Exists(_mapsPath))
        return new MapsData();

      string json = File.ReadAllText(_mapsPath);

      try
      {
        if (HasKey(json, "props"))
          return JsonSerializer.Deserialize<LegacyMapsData>(json)?.Upgrade() ?? new MapsData();

        var raw = JsonSerializer.Deserialize<Dictionary<string, List<PropAd>>>(json);
        return raw == null ? new MapsData() : MapsData.FromJson(raw);
      }
      catch (JsonException ex)
      {
        throw new AdsFileException(_mapsPath, ex);
      }
    }
  }

  public void SaveMaps(MapsData data) => Write(_mapsPath, data.ToJson());

  private static bool HasKey(string json, string key)
  {
    try
    {
      using var doc = JsonDocument.Parse(json);
      return doc.RootElement.ValueKind == JsonValueKind.Object
             && doc.RootElement.TryGetProperty(key, out var value)
             && value.ValueKind == JsonValueKind.Array;
    }
    catch
    {
      return false;
    }
  }

  private T Read<T>(string path) where T : new()
  {
    lock (_ioLock)
    {
      if (!File.Exists(path))
        return new T();

      try
      {
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path)) ?? new T();
      }
      catch (JsonException ex)
      {
        throw new AdsFileException(path, ex);
      }
    }
  }

  private void Write<T>(string path, T data)
  {
    lock (_ioLock)
    {
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      File.WriteAllText(path, JsonSerializer.Serialize(data, Opts));
    }
  }

  public void Backup()
  {
    lock (_ioLock)
    {
      foreach (var path in new[] { _adsPath, _propsPath, _mapsPath })
      {
        if (File.Exists(path))
          File.Copy(path, path + ".backup", true);
      }
    }
  }
}

public class AdsMySqlStorage : IAdsStorage
{
  private readonly string _connString;
  private readonly string _database;

  private readonly string _propModels;
  private readonly string _props;
  private readonly string _screenTexts;
  private readonly string _hudSays;
  private readonly string _chatSays;
  private readonly string _events;

  public AdsMySqlStorage(AdsMySqlSettings cfg)
  {
    _connString = new MySqlConnectionStringBuilder
    {
      Server = cfg.Host,
      Port = cfg.Port,
      Database = cfg.Database,
      UserID = cfg.User,
      Password = cfg.Password,
      Pooling = true
    }.ConnectionString;

    _database = cfg.Database;

    string prefix = cfg.TablePrefix;
    _propModels = prefix + "propmodels";
    _props = prefix + "props";
    _screenTexts = prefix + "screentexts";
    _hudSays = prefix + "hudsays";
    _chatSays = prefix + "chatsays";
    _events = prefix + "events";
  }

  private const string Head = "`id` INT NOT NULL AUTO_INCREMENT, `sort_order` INT NOT NULL DEFAULT 0,";
  private const string Flags = "`flag` VARCHAR(128) NOT NULL DEFAULT '', `ignoreflag` VARCHAR(128) NOT NULL DEFAULT '',";
  private const string Tail = "PRIMARY KEY (`id`), KEY `order` (`sort_order`)";

  public void Init()
  {
    TryCreateDatabase();

    using var conn = new MySqlConnection(_connString);
    conn.Open();

    Exec(conn, $@"CREATE TABLE IF NOT EXISTS `{_propModels}` ({Head}
      `name` VARCHAR(64) NOT NULL DEFAULT '',
      `path` VARCHAR(255) NOT NULL DEFAULT '',
      `scale` FLOAT NOT NULL DEFAULT 1,
      `skin` INT NOT NULL DEFAULT 0,
      `solid` TINYINT NOT NULL DEFAULT 0,
      `skins` VARCHAR(128) NOT NULL DEFAULT '',
      {Flags} {Tail});");

    Exec(conn, $@"CREATE TABLE IF NOT EXISTS `{_props}` ({Head}
      `path` VARCHAR(255) NOT NULL DEFAULT '',
      `map` VARCHAR(64) NOT NULL DEFAULT '*',
      `pos` VARCHAR(64) NOT NULL DEFAULT '0 0 0',
      `angle` VARCHAR(64) NOT NULL DEFAULT '0 0 0',
      `scale` FLOAT NOT NULL DEFAULT 1,
      `skin` INT NOT NULL DEFAULT 0,
      `solid` TINYINT NOT NULL DEFAULT 0,
      {Flags} {Tail});");

    Exec(conn, $@"CREATE TABLE IF NOT EXISTS `{_screenTexts}` ({Head}
      `text` TEXT,
      `life` FLOAT NOT NULL DEFAULT 8,
      `timer` FLOAT NOT NULL DEFAULT 30,
      `x` FLOAT NOT NULL DEFAULT 0,
      `y` FLOAT NOT NULL DEFAULT 0,
      `size` FLOAT NOT NULL DEFAULT 32,
      `color` VARCHAR(32) NOT NULL DEFAULT '#FFFFFF',
      `justify` VARCHAR(16) NOT NULL DEFAULT 'left',
      `background` TINYINT NOT NULL DEFAULT 0,
      {Flags} {Tail});");

    Exec(conn, $@"CREATE TABLE IF NOT EXISTS `{_hudSays}` ({Head}
      `text` TEXT,
      `life` FLOAT NOT NULL DEFAULT 6,
      `timer` FLOAT NOT NULL DEFAULT 45,
      {Flags} {Tail});");

    Exec(conn, $@"CREATE TABLE IF NOT EXISTS `{_chatSays}` ({Head}
      `text` TEXT,
      `timer` FLOAT NOT NULL DEFAULT 60,
      {Flags} {Tail});");

    Exec(conn, $@"CREATE TABLE IF NOT EXISTS `{_events}` ({Head}
      `event` VARCHAR(32) NOT NULL DEFAULT '',
      `target` VARCHAR(16) NOT NULL DEFAULT 'all',
      `type` VARCHAR(16) NOT NULL DEFAULT 'chatsay',
      `text` TEXT,
      `life` FLOAT NOT NULL DEFAULT 4,
      `cooldown` FLOAT NOT NULL DEFAULT 10,
      `chance` INT NOT NULL DEFAULT 100,
      `x` FLOAT NOT NULL DEFAULT 0,
      `y` FLOAT NOT NULL DEFAULT 0,
      `size` FLOAT NOT NULL DEFAULT 32,
      `color` VARCHAR(32) NOT NULL DEFAULT '#FFFFFF',
      `justify` VARCHAR(16) NOT NULL DEFAULT 'left',
      `background` TINYINT NOT NULL DEFAULT 0,
      {Flags} {Tail});");
  }

  public AdsData Load()
  {
    var data = new AdsData();

    using var conn = new MySqlConnection(_connString);
    conn.Open();

    Read(conn, _screenTexts, "`text`, life, timer, x, y, `size`, color, justify, background, `flag`, ignoreflag", reader =>
      data.ScreenTexts.Add(new ScreenTextAd
      {
        Text = Str(reader, 0),
        Life = Flt(reader, 1),
        Timer = Flt(reader, 2),
        X = Flt(reader, 3),
        Y = Flt(reader, 4),
        Size = Flt(reader, 5),
        Color = Str(reader, 6),
        Justify = Str(reader, 7),
        Background = Int(reader, 8) != 0,
        Flag = Str(reader, 9),
        IgnoreFlag = Str(reader, 10)
      }));

    Read(conn, _hudSays, "`text`, life, timer, `flag`, ignoreflag", reader =>
      data.HudSays.Add(new HudSayAd
      {
        Text = Str(reader, 0),
        Life = Flt(reader, 1),
        Timer = Flt(reader, 2),
        Flag = Str(reader, 3),
        IgnoreFlag = Str(reader, 4)
      }));

    Read(conn, _chatSays, "`text`, timer, `flag`, ignoreflag", reader =>
      data.ChatSays.Add(new ChatSayAd
      {
        Text = Str(reader, 0),
        Timer = Flt(reader, 1),
        Flag = Str(reader, 2),
        IgnoreFlag = Str(reader, 3)
      }));

    Read(conn, _events, "`event`, target, `type`, `text`, life, cooldown, chance, x, y, `size`, color, justify, background, `flag`, ignoreflag", reader =>
      data.Events.Add(new EventAd
      {
        Event = Str(reader, 0),
        Target = Str(reader, 1),
        Type = Str(reader, 2),
        Text = Str(reader, 3),
        Life = Flt(reader, 4),
        Cooldown = Flt(reader, 5),
        Chance = Int(reader, 6),
        X = Flt(reader, 7),
        Y = Flt(reader, 8),
        Size = Flt(reader, 9),
        Color = Str(reader, 10),
        Justify = Str(reader, 11),
        Background = Int(reader, 12) != 0,
        Flag = Str(reader, 13),
        IgnoreFlag = Str(reader, 14)
      }));

    return data;
  }

  public void Save(AdsData data)
  {
    using var conn = new MySqlConnection(_connString);
    conn.Open();
    using var transaction = conn.BeginTransaction();

    Wipe(conn, transaction, _screenTexts, _hudSays, _chatSays, _events);

    int order = 0;
    foreach (var ad in data.ScreenTexts)
      Insert(conn, transaction, _screenTexts, order++, new()
      {
        ["text"] = ad.Text,
        ["life"] = ad.Life,
        ["timer"] = ad.Timer,
        ["x"] = ad.X,
        ["y"] = ad.Y,
        ["size"] = ad.Size,
        ["color"] = ad.Color,
        ["justify"] = ad.Justify,
        ["background"] = ad.Background ? 1 : 0,
        ["flag"] = ad.Flag ?? "",
        ["ignoreflag"] = ad.IgnoreFlag ?? ""
      });

    order = 0;
    foreach (var ad in data.HudSays)
      Insert(conn, transaction, _hudSays, order++, new()
      {
        ["text"] = ad.Text,
        ["life"] = ad.Life,
        ["timer"] = ad.Timer,
        ["flag"] = ad.Flag ?? "",
        ["ignoreflag"] = ad.IgnoreFlag ?? ""
      });

    order = 0;
    foreach (var ad in data.ChatSays)
      Insert(conn, transaction, _chatSays, order++, new()
      {
        ["text"] = ad.Text,
        ["timer"] = ad.Timer,
        ["flag"] = ad.Flag ?? "",
        ["ignoreflag"] = ad.IgnoreFlag ?? ""
      });

    order = 0;
    foreach (var ad in data.Events)
      Insert(conn, transaction, _events, order++, new()
      {
        ["event"] = ad.Event,
        ["target"] = ad.Target,
        ["type"] = ad.Type,
        ["text"] = ad.Text,
        ["life"] = ad.Life,
        ["cooldown"] = ad.Cooldown,
        ["chance"] = ad.Chance,
        ["x"] = ad.X,
        ["y"] = ad.Y,
        ["size"] = ad.Size,
        ["color"] = ad.Color,
        ["justify"] = ad.Justify,
        ["background"] = ad.Background ? 1 : 0,
        ["flag"] = ad.Flag ?? "",
        ["ignoreflag"] = ad.IgnoreFlag ?? ""
      });

    transaction.Commit();
  }

  public PropsData LoadProps()
  {
    var data = new PropsData();

    using var conn = new MySqlConnection(_connString);
    conn.Open();

    Read(conn, _propModels, "name, path, scale, skin, solid, skins, `flag`, ignoreflag", reader =>
      data.Models.Add(new PropModel
      {
        Name = Str(reader, 0),
        Path = Str(reader, 1),
        Scale = Flt(reader, 2),
        Skin = Int(reader, 3),
        Solid = Int(reader, 4) != 0,
        Skins = ParseSkins(Str(reader, 5)),
        Flag = Str(reader, 6),
        IgnoreFlag = Str(reader, 7)
      }));

    return data;
  }

  public void SaveProps(PropsData data)
  {
    using var conn = new MySqlConnection(_connString);
    conn.Open();
    using var transaction = conn.BeginTransaction();

    Wipe(conn, transaction, _propModels);

    int order = 0;
    foreach (var model in data.Models)
      Insert(conn, transaction, _propModels, order++, new()
      {
        ["name"] = model.Name,
        ["path"] = model.Path,
        ["scale"] = model.Scale,
        ["skin"] = model.Skin,
        ["solid"] = model.Solid ? 1 : 0,
        ["skins"] = model.Skins == null ? "" : string.Join(",", model.Skins),
        ["flag"] = model.Flag ?? "",
        ["ignoreflag"] = model.IgnoreFlag ?? ""
      });

    transaction.Commit();
  }

  public MapsData LoadMaps()
  {
    var data = new MapsData();

    using var conn = new MySqlConnection(_connString);
    conn.Open();

    Read(conn, _props, "path, map, pos, angle, scale, skin, solid, `flag`, ignoreflag", reader =>
      data.Props.Add(new PropAd
      {
        Path = Str(reader, 0),
        Map = Str(reader, 1),
        Pos = Str(reader, 2),
        Angle = Str(reader, 3),
        Scale = Flt(reader, 4),
        Skin = Int(reader, 5),
        Solid = Int(reader, 6) != 0,
        Flag = Str(reader, 7),
        IgnoreFlag = Str(reader, 8)
      }));

    return data;
  }

  public void SaveMaps(MapsData data)
  {
    using var conn = new MySqlConnection(_connString);
    conn.Open();
    using var transaction = conn.BeginTransaction();

    Wipe(conn, transaction, _props);

    int order = 0;
    foreach (var ad in data.Props)
      Insert(conn, transaction, _props, order++, new()
      {
        ["path"] = ad.Path,
        ["map"] = ad.Map,
        ["pos"] = ad.Pos,
        ["angle"] = ad.Angle,
        ["scale"] = ad.Scale,
        ["skin"] = ad.Skin,
        ["solid"] = ad.Solid ? 1 : 0,
        ["flag"] = ad.Flag ?? "",
        ["ignoreflag"] = ad.IgnoreFlag ?? ""
      });

    transaction.Commit();
  }

  private static void Read(MySqlConnection conn, string table, string columns, Action<MySqlDataReader> row)
  {
    using var cmd = new MySqlCommand($"SELECT {columns} FROM `{table}` ORDER BY sort_order, id;", conn);
    using var reader = cmd.ExecuteReader();
    while (reader.Read())
      row(reader);
  }

  private static void Wipe(MySqlConnection conn, MySqlTransaction transaction, params string[] tables)
  {
    foreach (var table in tables)
    {
      using var cmd = new MySqlCommand($"DELETE FROM `{table}`;", conn, transaction);
      cmd.ExecuteNonQuery();
    }
  }

  private static void Insert(MySqlConnection conn, MySqlTransaction transaction, string table, int order,
    Dictionary<string, object> values)
  {
    var columns = new List<string> { "`sort_order`" };
    var parameters = new List<string> { "@order" };

    foreach (var key in values.Keys)
    {
      columns.Add($"`{key}`");
      parameters.Add("@" + key);
    }

    using var cmd = new MySqlCommand(
      $"INSERT INTO `{table}` ({string.Join(", ", columns)}) VALUES ({string.Join(", ", parameters)});",
      conn, transaction);

    cmd.Parameters.AddWithValue("@order", order);
    foreach (var (key, value) in values)
      cmd.Parameters.AddWithValue("@" + key, value);

    cmd.ExecuteNonQuery();
  }

  private void TryCreateDatabase()
  {
    try
    {
      var builder = new MySqlConnectionStringBuilder(_connString) { Database = "" };
      using var conn = new MySqlConnection(builder.ConnectionString);
      conn.Open();
      Exec(conn, $"CREATE DATABASE IF NOT EXISTS `{_database}`;");
    }
    catch { }
  }

  private static void Exec(MySqlConnection conn, string sql)
  {
    using var cmd = new MySqlCommand(sql, conn);
    cmd.ExecuteNonQuery();
  }

  private static List<int>? ParseSkins(string value)
  {
    if (string.IsNullOrWhiteSpace(value))
      return null;

    var list = new List<int>();
    foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
      if (int.TryParse(part, out int skin))
        list.Add(skin);
    }

    return list.Count == 0 ? null : list;
  }

  private static string Str(MySqlDataReader reader, int i) => reader.IsDBNull(i) ? "" : reader.GetString(i);
  private static float Flt(MySqlDataReader reader, int i) => reader.IsDBNull(i) ? 0f : (float)reader.GetDouble(i);
  private static int Int(MySqlDataReader reader, int i) => reader.IsDBNull(i) ? 0 : reader.GetInt32(i);
}
