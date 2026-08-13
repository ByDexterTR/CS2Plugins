using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

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

    LoadData();
    SpawnWorldAds();

    Reply(player, Localizer["ads.reloaded",
      _data.Props.Count, _data.ScreenTexts.Count, _data.HudSays.Count, _data.ChatSays.Count, _data.Events.Count]);
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
    try
    {
      var mysql = GetMySql();

      var fileAds = _json.Load();
      var fileProps = _json.LoadProps();
      var fileMaps = _json.LoadMaps();

      mysql.Save(fileAds);
      mysql.SaveProps(fileProps);
      mysql.SaveMaps(fileMaps);

      Reply(player, Localizer["ads.imported",
        fileMaps.Props.Count, fileAds.Events.Count, AdsCount(fileAds)]);
    }
    catch (Exception ex)
    {
      Reply(player, Localizer["ads.sql_error", ex.Message]);
    }
  }

  private void ExportSql(CCSPlayerController? player)
  {
    try
    {
      var mysql = GetMySql();

      var dbAds = mysql.Load();
      var dbProps = mysql.LoadProps();
      var dbMaps = mysql.LoadMaps();

      _json.Backup();
      _json.Save(dbAds);
      _json.SaveProps(dbProps);
      _json.SaveMaps(dbMaps);

      Reply(player, Localizer["ads.exported",
        dbMaps.Props.Count, dbAds.Events.Count, AdsCount(dbAds)]);
    }
    catch (Exception ex)
    {
      Reply(player, Localizer["ads.sql_error", ex.Message]);
    }
  }

  private void ReloadProps()
  {
    _mapsData.Props = _storage.LoadMaps().Props;
    _data.Props = _mapsData.Props;
    SpawnWorldAds();
    SyncListeners();
  }

  private void ReloadAds()
  {
    var loaded = _storage.Load();
    _data.ScreenTexts = loaded.ScreenTexts;
    _data.HudSays = loaded.HudSays;
    _data.ChatSays = loaded.ChatSays;
    _data.Events = loaded.Events;

    ClearScreenTexts();
    BuildQueues();
    BuildEvents();
    SyncListeners();
  }

  private void ReloadSettings(CCSPlayerController? player)
  {
    LoadSettings();
    SyncListeners();
    Reply(player, Localizer["ads.reloaded_settings", _json.SettingsFilePath]);
  }

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

  private void SaveMaps(CCSPlayerController? player)
  {
    try
    {
      SaveMaps();
    }
    catch (Exception ex)
    {
      Reply(player, Localizer["ads.save_error", ex.Message]);
    }
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

  private AdsMySqlStorage GetMySql()
  {
    if (_mysql != null)
      return _mysql;

    _mysql = new AdsMySqlStorage(Config.MySql);
    _mysql.Init();
    return _mysql;
  }
}
