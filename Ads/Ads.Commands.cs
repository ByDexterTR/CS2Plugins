using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace Ads;

public partial class Ads
{
  private void RegisterCommands()
  {
    foreach (var name in Util.Split(Config.Commands))
      AddCommand(name, "Reklam menusunu acar", OnMenuCommand);

    foreach (var name in Util.Split(Config.ReloadCommands))
      AddCommand(name, "Reklamlari ve proplari yeniler", OnReloadCommand);

    foreach (var name in Util.Split(Config.ImportSqlCommands))
      AddCommand(name, "Json dosyalarini MySQL'e aktarir", OnImportSqlCommand);

    foreach (var name in Util.Split(Config.ExportSqlCommands))
      AddCommand(name, "MySQL icerigini Json dosyalarina aktarir", OnExportSqlCommand);

    AddCommandListener("say", OnSay);
    AddCommandListener("say_team", OnSay);
  }

  private void Reply(CCSPlayerController? player, string message)
  {
    if (player == null || !player.IsValid)
      Server.PrintToConsole($"[Ads] {message}");
    else
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {message}");
  }

  private void OnMenuCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (!Util.HasAccess(player, Config.Flag))
      return;

    if (player == null || !player.IsValid)
    {
      Reply(player, Localizer["ads.ingame_only"]);
      return;
    }

    ShowMainMenu(player);
  }

  private void OnReloadCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (!Util.HasAccess(player, Config.Flag))
      return;

    var reply = Replier(player);

    LoadData(false, () =>
    {
      SpawnWorldAds();
      reply(Localizer["ads.reloaded",
        _data.Props.Count, _data.ScreenTexts.Count, _data.HudSays.Count, _data.ChatSays.Count, _data.Events.Count]);
    }, player);
  }

  private Action<string> Replier(CCSPlayerController? player)
  {
    int userId = Util.UserId(player);
    bool console = player == null;

    return message =>
    {
      if (console)
        Reply(null, message);
      else if (Util.FromUserId(userId) is { } target)
        Reply(target, message);
    };
  }

  private void OnImportSqlCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (!Util.HasAccess(player, Config.Flag))
      return;

    ImportSql(player);
  }

  private void OnExportSqlCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (!Util.HasAccess(player, Config.Flag))
      return;

    ExportSql(player);
  }

  private void ImportSql(CCSPlayerController? player)
  {
    AdsData fileAds;
    PropsData fileProps;
    MapsData fileMaps;

    try
    {
      fileAds = _json.Load();
      fileProps = _json.LoadProps();
      fileMaps = _json.LoadMaps();
    }
    catch (Exception ex)
    {
      Reply(player, Localizer["ads.sql_error", ex.Message]);
      return;
    }

    var mysql = MySqlTarget();
    var reply = Replier(player);

    Background(() =>
    {
      string message;
      try
      {
        mysql.Init();
        mysql.Save(fileAds);
        mysql.SaveProps(fileProps);
        mysql.SaveMaps(fileMaps);
        message = Localizer["ads.imported", fileMaps.Props.Count, fileAds.Events.Count, AdsCount(fileAds)];
      }
      catch (Exception ex)
      {
        message = Localizer["ads.sql_error", ex.Message];
      }

      Main(() => reply(message));
    });
  }

  private void ExportSql(CCSPlayerController? player)
  {
    var mysql = MySqlTarget();
    var json = _json;
    var reply = Replier(player);

    Background(() =>
    {
      string message;
      try
      {
        mysql.Init();
        var dbAds = mysql.Load();
        var dbProps = mysql.LoadProps();
        var dbMaps = mysql.LoadMaps();

        json.Backup();
        json.Save(dbAds);
        json.SaveProps(dbProps);
        json.SaveMaps(dbMaps);
        message = Localizer["ads.exported", dbMaps.Props.Count, dbAds.Events.Count, AdsCount(dbAds)];
      }
      catch (Exception ex)
      {
        message = Localizer["ads.sql_error", ex.Message];
      }

      Main(() => reply(message));
    });
  }

  private void ReloadProps(CCSPlayerController? player)
  {
    var reply = Replier(player);

    Fetch(storage => storage.LoadMaps(), maps =>
    {
      _mapsData = maps;
      _data.Props = _mapsData.Props;
      SpawnWorldAds();
      reply(Localizer["ads.reloaded_props", _data.Props.Count]);
    }, ex => ReportFileError(player, ex));
  }

  private void ReloadAds(CCSPlayerController? player)
  {
    var reply = Replier(player);

    Fetch(storage => storage.Load(), loaded =>
    {
      _data.ScreenTexts = loaded.ScreenTexts;
      _data.HudSays = loaded.HudSays;
      _data.ChatSays = loaded.ChatSays;
      _data.Events = loaded.Events;

      ClearScreenTexts();
      BuildQueues();
      BuildEvents();
      SyncListeners();
      reply(Localizer["ads.reloaded_ads",
        _data.ScreenTexts.Count, _data.HudSays.Count, _data.ChatSays.Count, _data.Events.Count]);
    }, ex => ReportFileError(player, ex));
  }

  private void ReloadSettings(CCSPlayerController? player)
  {
    try
    {
      LoadSettings(true);
    }
    catch (Exception ex)
    {
      ReportFileError(player, ex);
      return;
    }

    BuildQueues();
    SyncListeners();
    Reply(player, Localizer["ads.reloaded_settings", _json.SettingsFilePath]);

    if (Config.Storage.Equals("mysql", StringComparison.OrdinalIgnoreCase))
    {
      StartMySql(SpawnWorldAds);
    }
    else if (IsRemote(_storage))
    {
      _mysql = null;
      _storage = _json;
      LoadData(false, SpawnWorldAds, player);
    }
  }

  private void ReportFileError(CCSPlayerController? player, Exception ex)
  {
    Logger.LogError("{message}", ex.Message);
    if (player != null)
      Reply(player, ErrorText(ex));
  }

  private string ErrorText(Exception ex) => ex is AdsFileException
    ? Localizer["ads.file_error", ex.Message]
    : Localizer["ads.sql_error", ex.Message];

  private void PlaceProp(CCSPlayerController player, PropModel model)
  {
    if (!TryGetAimPoint(player, out var hit, out _))
      return;

    var ad = new PropAd
    {
      Path = model.Path,
      Map = Server.MapName,
      Pos = FormatVector(hit.X, hit.Y, hit.Z),
      Angle = "0 0 0",
      Scale = model.Scale <= 0f ? 1f : model.Scale,
      Skin = model.Skin,
      Solid = model.Solid,
      Flag = model.Flag,
      IgnoreFlag = model.IgnoreFlag
    };

    _data.Props.Add(ad);
    SaveMaps(player);
    SpawnWorldAds();
    Reply(player, Localizer["ads.added", ad.Pos]);
  }

  private PlacedAd? FindAimedAd(CCSPlayerController player)
  {
    if (!TryGetAimPoint(player, out var hit, out _))
      return null;

    PlacedAd? best = null;
    float bestDistance = float.MaxValue;

    foreach (var placed in _entities)
    {
      var origin = placed.Entity?.AbsOrigin;
      if (origin == null || placed.Entity?.IsValid != true)
        continue;

      float dx = origin.X - hit.X;
      float dy = origin.Y - hit.Y;
      float dz = origin.Z - hit.Z;
      float distance = dx * dx + dy * dy + dz * dz;

      if (distance < bestDistance)
      {
        bestDistance = distance;
        best = placed;
      }
    }

    if (best == null || bestDistance > 128f * 128f)
    {
      Reply(player, Localizer["ads.remove_none"]);
      return null;
    }

    return best;
  }

  private bool TryGetAimPoint(CCSPlayerController? player, out System.Numerics.Vector3 hit, out CCSPlayerPawn? pawn)
  {
    hit = default;
    pawn = player?.PlayerPawn.Value;

    if (player == null || !player.IsValid || pawn == null || !pawn.IsValid)
    {
      Reply(player, Localizer["ads.ingame_only"]);
      return false;
    }

    var result = NativeTrace.TraceFromEyes(pawn);
    if (result == null)
    {
      Reply(player, NativeTrace.LastError != null
        ? Localizer["ads.trace_unavailable", NativeTrace.LastError]
        : Localizer["ads.no_hit"]);
      return false;
    }

    hit = result.Value;
    return true;
  }

  private static int AdsCount(AdsData data) =>
    data.ScreenTexts.Count + data.HudSays.Count + data.ChatSays.Count;

  private AdsMySqlStorage MySqlTarget() => _mysql ?? new AdsMySqlStorage(Config.MySql);
}
