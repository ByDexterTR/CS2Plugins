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

        Exec(conn, $@"CREATE TABLE IF NOT EXISTS `{_settings}` (
            `steamid` BIGINT UNSIGNED NOT NULL,
            `feature` VARCHAR(64) NOT NULL,
            `value` VARCHAR(64) NOT NULL,
            PRIMARY KEY (`steamid`, `feature`));");
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

        using var cmd = new MySqlCommand($"SELECT steamid, feature, value FROM `{_settings}`;", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            ulong steamId = reader.GetUInt64(0);
            if (!result.TryGetValue(steamId, out var dict))
            {
                dict = new();
                result[steamId] = dict;
            }
            dict[reader.GetString(1)] = reader.GetString(2);
        }

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

        using var cmd = new MySqlCommand($"SELECT feature, value FROM `{_settings}` WHERE steamid = @s;", conn);
        cmd.Parameters.AddWithValue("@s", steamId);
        using var reader = cmd.ExecuteReader();

        var result = new Dictionary<string, string>();
        while (reader.Read())
            result[reader.GetString(0)] = reader.GetString(1);

        return result;
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

    public void UpsertSetting(ulong steamId, string feature, string value)
    {
        using var conn = new MySqlConnection(_connString);
        conn.Open();

        using var cmd = new MySqlCommand(
            $@"INSERT INTO `{_settings}` (steamid, feature, value) VALUES (@s, @f, @v)
               ON DUPLICATE KEY UPDATE value = @v;", conn);
        cmd.Parameters.AddWithValue("@s", steamId);
        cmd.Parameters.AddWithValue("@f", feature);
        cmd.Parameters.AddWithValue("@v", value);
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

    public void DeleteSetting(ulong steamId, string feature)
    {
        using var conn = new MySqlConnection(_connString);
        conn.Open();

        using var cmd = new MySqlCommand($"DELETE FROM `{_settings}` WHERE steamid = @s AND feature = @f;", conn);
        cmd.Parameters.AddWithValue("@s", steamId);
        cmd.Parameters.AddWithValue("@f", feature);
        cmd.ExecuteNonQuery();
    }

    private static void Exec(MySqlConnection conn, string sql)
    {
        using var cmd = new MySqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }
}
