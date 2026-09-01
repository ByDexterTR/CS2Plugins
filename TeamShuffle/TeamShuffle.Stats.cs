using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace TeamShuffle;

public class PlayerStats
{
  [JsonPropertyName("version")]
  public int Version { get; set; } = 1;

  [JsonPropertyName("updated")]
  public long Updated { get; set; }

  [JsonPropertyName("rounds")]
  public int Rounds { get; set; }

  [JsonPropertyName("kills")]
  public int Kills { get; set; }

  [JsonPropertyName("deaths")]
  public int Deaths { get; set; }

  [JsonPropertyName("assists")]
  public int Assists { get; set; }

  [JsonPropertyName("damage")]
  public float Damage { get; set; }

  [JsonPropertyName("mvp")]
  public int Mvp { get; set; }

  [JsonPropertyName("first_kills")]
  public int FirstKills { get; set; }

  [JsonPropertyName("shots")]
  public int Shots { get; set; }

  [JsonPropertyName("hits")]
  public Dictionary<string, int> Hits { get; set; } = [];

  [JsonPropertyName("multi_kills")]
  public Dictionary<string, int> MultiKills { get; set; } = [];

  [JsonPropertyName("clutches")]
  public Dictionary<string, int> Clutches { get; set; } = [];

  [JsonIgnore]
  public int LifeKills;

  [JsonIgnore]
  public bool Played;

  [JsonIgnore]
  public bool Dirty;

  public int Hit(string group) => Hits.TryGetValue(group, out int value) ? value : 0;

  public int TotalHits()
  {
    int total = 0;

    foreach (int value in Hits.Values)
      total += value;

    return total;
  }

  public float WeightedClutches() =>
    Bucket(Clutches, "2") + Bucket(Clutches, "3") * 2f + Bucket(Clutches, "4") * 3f + Bucket(Clutches, "5") * 5f;

  public static int Bucket(Dictionary<string, int> source, string key) =>
    source.TryGetValue(key, out int value) ? value : 0;

  public static void Add(Dictionary<string, int> source, string key, int amount = 1) =>
    source[key] = Bucket(source, key) + amount;
}

public partial class TeamShuffle
{
  private const int ShrinkRounds = 5;

  private static readonly JsonSerializerOptions StatsJson = new() { WriteIndented = true };

  private readonly Dictionary<CsTeam, (int UserId, int Enemies)> clutchCandidates = new();
  private bool roundFirstKillTaken;

  private string StatsDirectory => Path.Combine(ModuleDirectory, "players");

  private string StatsFile(ulong steamId) => Path.Combine(StatsDirectory, $"{steamId}.json");

  private static string HitGroup(int hitGroup) => hitGroup switch
  {
    1 => "head",
    2 => "chest",
    3 => "stomach",
    4 or 5 => "arm",
    6 or 7 => "leg",
    _ => "other"
  };

  private void LoadStats(CCSPlayerController player)
  {
    ulong steamId = Util.SteamId(player);
    if (steamId == 0UL || player.IsBot || player.IsHLTV)
      return;

    string path = StatsFile(steamId);

    Task.Run(() =>
    {
      PlayerStats? loaded = null;

      try
      {
        if (File.Exists(path))
          loaded = JsonSerializer.Deserialize<PlayerStats>(File.ReadAllText(path));
      }
      catch (Exception ex)
      {
        Logger.LogError("TeamShuffle: {File} okunamadi: {Message}", path, ex.Message);
      }

      if (loaded == null)
        return;

      Server.NextFrame(() => MergeLoaded(steamId, loaded));
    });
  }

  private void MergeLoaded(ulong steamId, PlayerStats loaded)
  {
    if (!stats.TryGetValue(steamId, out var live))
    {
      stats[steamId] = loaded;
      return;
    }

    loaded.Rounds += live.Rounds;
    loaded.Kills += live.Kills;
    loaded.Deaths += live.Deaths;
    loaded.Assists += live.Assists;
    loaded.Damage += live.Damage;
    loaded.Mvp += live.Mvp;
    loaded.FirstKills += live.FirstKills;
    loaded.Shots += live.Shots;

    foreach (var (key, value) in live.Hits)
      PlayerStats.Add(loaded.Hits, key, value);

    foreach (var (key, value) in live.MultiKills)
      PlayerStats.Add(loaded.MultiKills, key, value);

    foreach (var (key, value) in live.Clutches)
      PlayerStats.Add(loaded.Clutches, key, value);

    loaded.LifeKills = live.LifeKills;
    loaded.Played = live.Played;
    loaded.Dirty = live.Dirty;

    stats[steamId] = loaded;
  }

  private void SaveStats(ulong steamId, PlayerStats entry)
  {
    if (steamId == 0UL)
      return;

    entry.Updated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    entry.Dirty = false;

    string path = StatsFile(steamId);
    string payload;

    try
    {
      payload = JsonSerializer.Serialize(entry, StatsJson);
    }
    catch (Exception ex)
    {
      Logger.LogError("TeamShuffle: {SteamId} yazilamadi: {Message}", steamId, ex.Message);
      return;
    }

    string temp = $"{path}.{Guid.NewGuid():N}.tmp";

    Task.Run(() =>
    {
      try
      {
        Directory.CreateDirectory(StatsDirectory);
        File.WriteAllText(temp, payload);
        File.Move(temp, path, true);
      }
      catch (Exception ex)
      {
        Logger.LogError("TeamShuffle: {File} yazilamadi: {Message}", path, ex.Message);

        try
        {
          File.Delete(temp);
        }
        catch
        {
        }
      }
    });
  }

  private void ClearTemporary()
  {
    string directory = StatsDirectory;

    Task.Run(() =>
    {
      try
      {
        if (!Directory.Exists(directory))
          return;

        foreach (string file in Directory.EnumerateFiles(directory, "*.tmp"))
          File.Delete(file);
      }
      catch (Exception ex)
      {
        Logger.LogError("TeamShuffle: gecici dosyalar silinemedi: {Message}", ex.Message);
      }
    });
  }

  private void SaveDirty()
  {
    foreach (var (steamId, entry) in stats)
    {
      if (entry.Dirty)
        SaveStats(steamId, entry);
    }
  }

  private void SaveAll()
  {
    foreach (var (steamId, entry) in stats)
      SaveStats(steamId, entry);
  }

  private void CountShot(CCSPlayerController? player)
  {
    var entry = GetStats(player);
    if (entry == null)
      return;

    entry.Shots++;
    entry.Dirty = true;
  }

  private void CountHit(CCSPlayerController? attacker, int hitGroup, int damage)
  {
    var entry = GetStats(attacker);
    if (entry == null)
      return;

    entry.Damage += damage;
    PlayerStats.Add(entry.Hits, HitGroup(hitGroup));
    entry.Dirty = true;
  }

  private void CountKill(CCSPlayerController? killer, CCSPlayerController? victim, CCSPlayerController? assister)
  {
    var victimEntry = GetStats(victim);
    if (victimEntry != null)
    {
      victimEntry.Deaths++;
      victimEntry.Dirty = true;
      BucketLife(victimEntry);
    }

    var killerEntry = GetStats(killer);
    if (killerEntry != null)
    {
      killerEntry.Kills++;
      killerEntry.LifeKills++;
      killerEntry.Dirty = true;

      if (!roundFirstKillTaken)
        killerEntry.FirstKills++;
    }

    roundFirstKillTaken = true;

    var assistEntry = GetStats(assister);
    if (assistEntry != null)
    {
      assistEntry.Assists++;
      assistEntry.Dirty = true;
    }

    TrackClutch();
  }

  private static void BucketLife(PlayerStats entry)
  {
    if (entry.LifeKills >= 2)
      PlayerStats.Add(entry.MultiKills, Math.Min(entry.LifeKills, 5).ToString());

    entry.LifeKills = 0;
  }

  private void TrackClutch()
  {
    foreach (var team in new[] { CsTeam.Terrorist, CsTeam.CounterTerrorist })
    {
      if (clutchCandidates.ContainsKey(team))
        continue;

      CCSPlayerController? last = null;
      int alive = 0;
      int enemies = 0;

      foreach (var player in Utilities.GetPlayers())
      {
        if (!IsPlaying(player) || !Util.IsAlive(player))
          continue;

        if (player.Team == team)
        {
          alive++;
          last = player;
        }
        else
        {
          enemies++;
        }
      }

      if (alive == 1 && enemies >= 2 && last != null)
        clutchCandidates[team] = (Util.UserId(last), Math.Min(enemies, 5));
    }
  }

  private void CommitClutch(int winner)
  {
    var team = (CsTeam)winner;

    if (clutchCandidates.TryGetValue(team, out var candidate))
    {
      var entry = GetStats(Util.FromUserId(candidate.UserId));

      if (entry != null)
      {
        PlayerStats.Add(entry.Clutches, candidate.Enemies.ToString());
        entry.Dirty = true;
      }
    }

    clutchCandidates.Clear();
  }

  private void CommitRound()
  {
    foreach (var entry in stats.Values)
    {
      BucketLife(entry);

      if (!entry.Played)
        continue;

      entry.Rounds++;
      entry.Played = false;
      entry.Dirty = true;
    }

    SaveDirty();
  }

  private float BaseRating(PlayerStats entry)
  {
    if (entry.Rounds <= 0)
      return 0f;

    int totalHits = entry.TotalHits();
    float aim = totalHits > 0 ? (float)entry.Hit("head") / (totalHits + 20) : 0f;

    return entry.Damage / entry.Rounds * Config.ShuffleDamageRating
      + (float)entry.Kills / entry.Rounds * Config.ShuffleKillRating
      + (float)entry.Mvp / entry.Rounds * Config.ShuffleMvpRating
      + entry.WeightedClutches() / entry.Rounds * Config.ShuffleClutchRating
      + aim * Config.ShuffleAimRating;
  }

  private List<(CCSPlayerController Player, float Rating)> Rated(List<CCSPlayerController> players)
  {
    var raw = new List<(CCSPlayerController Player, float Base, int Rounds)>(players.Count);

    foreach (var player in players)
    {
      ulong key = Util.SteamId(player);

      if (key != 0UL && stats.TryGetValue(key, out var entry) && entry.Rounds > 0)
        raw.Add((player, BaseRating(entry), entry.Rounds));
      else
        raw.Add((player, 0f, 0));
    }

    float average = 0f;
    int known = 0;

    foreach (var item in raw)
    {
      if (item.Rounds > 0)
      {
        average += item.Base;
        known++;
      }
    }

    average = known > 0 ? average / known : 0f;

    return raw
      .Select(e => (e.Player, Rating: e.Rounds > 0
        ? (e.Rounds * e.Base + ShrinkRounds * average) / (e.Rounds + ShrinkRounds)
        : average))
      .OrderByDescending(e => e.Rating)
      .ThenBy(_ => Random.Shared.Next())
      .ToList();
  }
}
