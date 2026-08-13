using System.Text.Json;
using MySqlConnector;

namespace VIPCore;

public class MySqlStorage : IVipStorage
{
    private readonly string _connString;
    private readonly string _database;
    private readonly string _users;
    private readonly string _settings;

    public MySqlStorage(MySqlSettings cfg)
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
        _users = cfg.TablePrefix + "users";
        _settings = cfg.TablePrefix + "settings";
    }

    public bool SupportsLiveRefresh => true;

    public void Init()
    {
        TryCreateDatabase();

        using var conn = new MySqlConnection(_connString);
        conn.Open();

        Exec(conn, $@"CREATE TABLE IF NOT EXISTS `{_users}` (
            `steamid` BIGINT UNSIGNED NOT NULL,
            `vip_group` VARCHAR(64) NOT NULL,
            `expires` BIGINT NOT NULL,
            PRIMARY KEY (`steamid`));");

        Migrate(conn);

        Exec(conn, $"CREATE TABLE IF NOT EXISTS `{_settings}` {SettingsSchema}");
    }

    private const string SettingsSchema = @"(
            `steamid` BIGINT UNSIGNED NOT NULL,
            `settings` JSON NOT NULL,
            PRIMARY KEY (`steamid`));";

    private void Migrate(MySqlConnection conn)
    {
        if (!HasColumn(conn, _settings, "feature"))
            return;

        var packed = new Dictionary<ulong, Dictionary<string, string>>();
        using (var cmd = new MySqlCommand($"SELECT steamid, feature, value FROM `{_settings}`;", conn))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                ulong steamId = reader.GetUInt64(0);
                if (!packed.TryGetValue(steamId, out var dict))
                {
                    dict = new();
                    packed[steamId] = dict;
                }
                dict[reader.GetString(1)] = reader.GetString(2);
            }
        }

        string staging = _settings + "_migrating";
        Exec(conn, $"DROP TABLE IF EXISTS `{staging}`;");
        Exec(conn, $"CREATE TABLE `{staging}` {SettingsSchema}");

        foreach (var (steamId, dict) in packed)
        {
            if (dict.Count == 0)
                continue;

            using var cmd = new MySqlCommand($"INSERT INTO `{staging}` (steamid, settings) VALUES (@s, @d);", conn);
            cmd.Parameters.AddWithValue("@s", steamId);
            cmd.Parameters.AddWithValue("@d", Pack(dict));
            cmd.ExecuteNonQuery();
        }

        Exec(conn, $"DROP TABLE `{_settings}`;");
        Exec(conn, $"RENAME TABLE `{staging}` TO `{_settings}`;");
    }

    private bool HasColumn(MySqlConnection conn, string table, string column)
    {
        using var cmd = new MySqlCommand(
            @"SELECT COUNT(*) FROM information_schema.COLUMNS
              WHERE TABLE_SCHEMA = @db AND TABLE_NAME = @t AND COLUMN_NAME = @c;", conn);
        cmd.Parameters.AddWithValue("@db", _database);
        cmd.Parameters.AddWithValue("@t", table);
        cmd.Parameters.AddWithValue("@c", column);
        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    }

    private static readonly JsonSerializerOptions PackOpts = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static string Pack(Dictionary<string, string> settings) =>
        JsonSerializer.Serialize(settings, PackOpts);

    private static Dictionary<string, string> Unpack(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        catch (JsonException)
        {
            return new();
        }
    }

    public Dictionary<ulong, VipEntry> LoadVips()
    {
        var result = new Dictionary<ulong, VipEntry>();
        using var conn = new MySqlConnection(_connString);
        conn.Open();

        using var cmd = new MySqlCommand($"SELECT steamid, vip_group, expires FROM `{_users}`;", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result[reader.GetUInt64(0)] = new VipEntry { Group = reader.GetString(1), Expires = reader.GetInt64(2) };

        return result;
    }

    public Dictionary<ulong, Dictionary<string, string>> LoadSettings()
    {
        var result = new Dictionary<ulong, Dictionary<string, string>>();
        using var conn = new MySqlConnection(_connString);
        conn.Open();

        using var cmd = new MySqlCommand($"SELECT steamid, settings FROM `{_settings}`;", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result[reader.GetUInt64(0)] = Unpack(reader.GetString(1));

        return result;
    }

    public VipEntry? LoadVip(ulong steamId)
    {
        using var conn = new MySqlConnection(_connString);
        conn.Open();

        using var cmd = new MySqlCommand($"SELECT vip_group, expires FROM `{_users}` WHERE steamid = @s;", conn);
        cmd.Parameters.AddWithValue("@s", steamId);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return new VipEntry { Group = reader.GetString(0), Expires = reader.GetInt64(1) };

        return null;
    }

    public Dictionary<string, string>? LoadSettings(ulong steamId)
    {
        using var conn = new MySqlConnection(_connString);
        conn.Open();

        return LoadSettings(conn, steamId);
    }

    private Dictionary<string, string> LoadSettings(MySqlConnection conn, ulong steamId)
    {
        using var cmd = new MySqlCommand($"SELECT settings FROM `{_settings}` WHERE steamid = @s;", conn);
        cmd.Parameters.AddWithValue("@s", steamId);

        return cmd.ExecuteScalar() is string json ? Unpack(json) : new Dictionary<string, string>();
    }

    public void UpsertVip(ulong steamId, VipEntry entry)
    {
        using var conn = new MySqlConnection(_connString);
        conn.Open();

        using var cmd = new MySqlCommand(
            $@"INSERT INTO `{_users}` (steamid, vip_group, expires) VALUES (@s, @g, @e)
               ON DUPLICATE KEY UPDATE vip_group = @g, expires = @e;", conn);
        cmd.Parameters.AddWithValue("@s", steamId);
        cmd.Parameters.AddWithValue("@g", entry.Group);
        cmd.Parameters.AddWithValue("@e", entry.Expires);
        cmd.ExecuteNonQuery();
    }

    public void DeleteVip(ulong steamId)
    {
        using var conn = new MySqlConnection(_connString);
        conn.Open();

        using (var cmd = new MySqlCommand($"DELETE FROM `{_users}` WHERE steamid = @s;", conn))
        {
            cmd.Parameters.AddWithValue("@s", steamId);
            cmd.ExecuteNonQuery();
        }
        using (var cmd = new MySqlCommand($"DELETE FROM `{_settings}` WHERE steamid = @s;", conn))
        {
            cmd.Parameters.AddWithValue("@s", steamId);
            cmd.ExecuteNonQuery();
        }
    }

    public void ApplySettings(ulong steamId, Dictionary<string, string?> ops)
    {
        if (ops.Count == 0)
            return;

        using var conn = new MySqlConnection(_connString);
        conn.Open();

        var settings = LoadSettings(conn, steamId);
        bool changed = false;

        foreach (var (feature, value) in ops)
        {
            if (value == null)
            {
                changed |= settings.Remove(feature);
                continue;
            }

            if (settings.TryGetValue(feature, out var current) && current == value)
                continue;

            settings[feature] = value;
            changed = true;
        }

        if (changed)
            Save(conn, steamId, settings);
    }

    private void Save(MySqlConnection conn, ulong steamId, Dictionary<string, string> settings)
    {
        if (settings.Count == 0)
        {
            using var delete = new MySqlCommand($"DELETE FROM `{_settings}` WHERE steamid = @s;", conn);
            delete.Parameters.AddWithValue("@s", steamId);
            delete.ExecuteNonQuery();
            return;
        }

        using var cmd = new MySqlCommand(
            $@"INSERT INTO `{_settings}` (steamid, settings) VALUES (@s, @d)
               ON DUPLICATE KEY UPDATE settings = @d;", conn);
        cmd.Parameters.AddWithValue("@s", steamId);
        cmd.Parameters.AddWithValue("@d", Pack(settings));
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
}
