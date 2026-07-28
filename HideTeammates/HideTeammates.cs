using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using static CounterStrikeSharp.API.Core.Listeners;
using ByDexter.Shared;

namespace HideTeammates;

public class HideTeammatesConfig : BasePluginConfig
{
  [JsonPropertyName("cmd_hide")]
  public string Commands { get; set; } = "css_hide,css_gizle";

  [JsonPropertyName("flag_hide")]
  public string Flag { get; set; } = "";

  [JsonPropertyName("mode_hide")]
  public int Mode { get; set; } = 1;

  [JsonPropertyName("disable_sound")]
  public int DisableSound { get; set; } = 1;
}

public class HideTeammates : BasePlugin, IPluginConfig<HideTeammatesConfig>
{
  public override string ModuleName => "HideTeammates";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public HideTeammatesConfig Config { get; set; } = new();

  private const int MaxSlots = 64;

  private readonly bool[] _hidden = new bool[MaxSlots];
  private readonly HashSet<ulong> _saved = new();
  private readonly object _ioLock = new();
  private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

  private string JsonPath => Path.Combine(ModuleDirectory, "players.json");

  public void OnConfigParsed(HideTeammatesConfig config)
  {
    if (config.Mode < 1 || config.Mode > 3)
      config.Mode = 1;
    config.DisableSound = config.DisableSound != 0 ? 1 : 0;
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    LoadPrefs();

    foreach (var name in Util.Split(Config.Commands))
      AddCommand(name, "Takim arkadaslarini gizler", OnHideCommand);

    RegisterListener<OnClientAuthorized>((slot, steamId) =>
    {
      if (slot >= 0 && slot < MaxSlots)
        _hidden[slot] = _saved.Contains(steamId.SteamId64);
    });

    RegisterListener<OnClientDisconnect>(slot =>
    {
      if (slot >= 0 && slot < MaxSlots)
        _hidden[slot] = false;
    });

    RegisterListener<CheckTransmit>(OnCheckTransmit);

    if (Config.DisableSound == 1)
    {
      HookUserMessage(208, OnSound, HookMode.Pre);
      HookUserMessage(369, OnWeaponSound, HookMode.Pre);
      HookUserMessage(452, OnWeaponEvent, HookMode.Pre);
    }

    if (hotReload)
    {
      foreach (var player in Utilities.GetPlayers())
      {
        if (player == null || !player.IsValid || player.IsBot || player.SteamID == 0)
          continue;
        _hidden[player.Slot] = _saved.Contains(player.SteamID);
      }
    }
  }

  public override void Unload(bool hotReload)
  {
    if (Config.DisableSound == 1)
    {
      UnhookUserMessage(208, OnSound, HookMode.Pre);
      UnhookUserMessage(369, OnWeaponSound, HookMode.Pre);
      UnhookUserMessage(452, OnWeaponEvent, HookMode.Pre);
    }
  }

  private void OnHideCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid || player.IsBot || player.SteamID == 0)
      return;

    if (!Util.HasAccess(player, Config.Flag))
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["hide.no_permission"]}");
      return;
    }

    int slot = player.Slot;
    _hidden[slot] = !_hidden[slot];

    if (_hidden[slot])
      _saved.Add(player.SteamID);
    else
      _saved.Remove(player.SteamID);

    SavePrefs();

    string key = _hidden[slot] ? "hide.enabled" : "hide.disabled";
    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer[key, ModeLabel()]}");
  }

  private string ModeLabel() => Config.Mode switch
  {
    2 => Localizer["hide.mode_enemy"],
    3 => Localizer["hide.mode_all"],
    _ => Localizer["hide.mode_team"]
  };

  private void LoadPrefs()
  {
    lock (_ioLock)
    {
      try
      {
        if (!File.Exists(JsonPath))
          return;

        var raw = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(JsonPath));
        if (raw == null)
          return;

        foreach (var entry in raw)
          if (ulong.TryParse(entry, out var steamId))
            _saved.Add(steamId);
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "[HideTeammates] players.json okunamadi");
      }
    }
  }

  private void SavePrefs()
  {
    var snapshot = _saved.Select(id => id.ToString()).ToList();

    Task.Run(() =>
    {
      try
      {
        lock (_ioLock)
          File.WriteAllText(JsonPath, JsonSerializer.Serialize(snapshot, JsonOpts));
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "[HideTeammates] players.json yazilamadi");
      }
    });
  }

  private bool ShouldHide(CCSPlayerController viewer, CCSPlayerController target)
  {
    if (viewer.Slot == target.Slot)
      return false;

    if (Config.Mode == 3)
      return true;

    byte viewerTeam = viewer.TeamNum;
    byte targetTeam = target.TeamNum;

    if (viewerTeam <= (byte)CsTeam.Spectator || targetTeam <= (byte)CsTeam.Spectator)
      return false;

    return Config.Mode == 2 ? targetTeam != viewerTeam : targetTeam == viewerTeam;
  }

  private void OnCheckTransmit(CCheckTransmitInfoList infoList)
  {
    foreach ((CCheckTransmitInfo info, CCSPlayerController? viewer) in infoList)
    {
      if (viewer == null || !viewer.IsValid || viewer.IsHLTV || !_hidden[viewer.Slot])
        continue;

      if (!Util.IsAlive(viewer))
        continue;

      for (int slot = 0; slot < MaxSlots; slot++)
      {
        if (slot == viewer.Slot)
          continue;

        var target = Utilities.GetPlayerFromSlot(slot);
        if (target == null || !target.IsValid || !ShouldHide(viewer, target))
          continue;

        var pawn = target.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
          continue;

        info.TransmitEntities.Remove(pawn);

        var weapons = pawn.WeaponServices?.MyWeapons;
        if (weapons == null)
          continue;

        foreach (var handle in weapons)
        {
          var weapon = handle.Value;
          if (weapon != null && weapon.IsValid)
            info.TransmitEntities.Remove(weapon);
        }
      }
    }
  }

  private HookResult OnSound(UserMessage msg)
  {
    try
    {
      if (!SoundHashes.Contains(msg.ReadUInt("soundevent_hash")))
        return HookResult.Continue;

      var source = PlayerFromEntityIndex(msg.ReadInt("source_entity_index"));
      if (source == null)
        return HookResult.Continue;

      FilterRecipients(msg, source);
      return msg.Recipients.Count == 0 ? HookResult.Stop : HookResult.Continue;
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[HideTeammates] OnSound hook hatasi");
      return HookResult.Continue;
    }
  }

  private HookResult OnWeaponSound(UserMessage msg)
  {
    try
    {
      var source = PlayerFromEntityIndex(msg.ReadInt("entidx"));
      if (source == null)
        return HookResult.Continue;

      FilterRecipients(msg, source);
      return msg.Recipients.Count == 0 ? HookResult.Stop : HookResult.Continue;
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[HideTeammates] OnWeaponSound hook hatasi");
      return HookResult.Continue;
    }
  }

  private HookResult OnWeaponEvent(UserMessage msg)
  {
    try
    {
      var source = PlayerFromEntityIndex((int)(msg.ReadUInt("player") & 0x7FF));
      if (source == null)
        return HookResult.Continue;

      FilterRecipients(msg, source);
      return msg.Recipients.Count == 0 ? HookResult.Stop : HookResult.Continue;
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[HideTeammates] OnWeaponEvent hook hatasi");
      return HookResult.Continue;
    }
  }

  private void FilterRecipients(UserMessage msg, CCSPlayerController source)
  {
    if (msg.Recipients == null)
      return;

    for (int i = msg.Recipients.Count - 1; i >= 0; i--)
    {
      var listener = msg.Recipients[i];
      if (listener == null || !listener.IsValid || !_hidden[listener.Slot])
        continue;

      if (ShouldHide(listener, source))
        msg.Recipients.RemoveAt(i);
    }
  }

  private static CCSPlayerController? PlayerFromEntityIndex(int index)
  {
    if (index <= 0)
      return null;

    var entity = Utilities.GetEntityFromIndex<CBaseEntity>(index);
    if (entity == null || !entity.IsValid || entity.DesignerName != "player")
      return null;

    var controller = entity.As<CCSPlayerPawn>().Controller.Value?.As<CCSPlayerController>();
    return controller != null && controller.IsValid ? controller : null;
  }

  private static readonly HashSet<uint> SoundHashes = new()
  {
    1543034, 29217150, 46413566, 58439651, 62938228, 70011614, 70939233, 84876002, 96240187, 115843229,
    117596568, 123085364, 129081149, 135189076, 142772671, 144629619, 202030084, 282152614, 297379099, 318971924,
    400609565, 413358161, 417910549, 427534867, 515548944, 520432428, 524041390, 540697918, 585390608, 602548457,
    604181152, 662078688, 663530947, 708038349, 721782259, 737696412, 740474905, 757978684, 765706800, 769561685,
    782454593, 795825195, 803727624, 809738584, 819435812, 822973253, 839762874, 850911881, 856190898, 859178236,
    870100484, 892882552, 893108375, 931543849, 935062317, 961838155, 963985059, 988265811, 1016523349, 1019414932,
    1116700262, 1161855519, 1163426340, 1165397261, 1183624286, 1193078452, 1194093029, 1194677450, 1218015996, 1247386781,
    1248619277, 1253503839, 1269567645, 1284373691, 1342713723, 1346129716, 1388885460, 1395892944, 1403457606, 1404198078,
    1407794113, 1409986305, 1412313471, 1424056132, 1440734007, 1448154350, 1485322532, 1489357772, 1499777741, 1506215040,
    1517575510, 1535891875, 1540837791, 1543118744, 1557420499, 1598540856, 1627020521, 1635413700, 1661204257, 1664187801,
    1664329401, 1682747253, 1690105992, 1692050905, 1734994609, 1761772772, 1763490157, 1769891506, 1770765328, 1771184788,
    1792523944, 1803111098, 1815352525, 1818046345, 1823342283, 1826799645, 1855038793, 1904605142, 1909915699, 1939055066,
    1961884255, 2019962436, 2020934318, 2026488395, 2053595705, 2056150061, 2061955732, 2064477315, 2067683805, 2070478448,
    2106508305, 2133235849, 2158707679, 2162652424, 2189706910, 2192712263, 2207486967, 2231399653, 2236021746, 2240518199,
    2265091453, 2284698275, 2300993891, 2302139631, 2310318859, 2316086169, 2323025056, 2333790984, 2369733616, 2381346641,
    2447320252, 2448803175, 2479376962, 2486534908, 2546391140, 2551626319, 2594927130, 2633527058, 2638406226, 2684452812,
    2696334288, 2703682875, 2708661994, 2714245023, 2719685137, 2722081556, 2735369596, 2745524735, 2790760284, 2800858936,
    2804393637, 2804654127, 2829617974, 2831007164, 2860219006, 2883205713, 2892812682, 2899365092, 2902143738, 2967038404,
    3008782656, 3009312615, 3023174225, 3030200692, 3049902652, 3057812547, 3065316423, 3099536373, 3103360935, 3109879199,
    3123711576, 3124768561, 3161194970, 3166948458, 3172583021, 3184465677, 3193435079, 3204513405, 3218103073, 3257325156,
    3259510958, 3266483468, 3295206520, 3299941720, 3342414459, 3368720745, 3396420465, 3434104102, 3460445620, 3469219129,
    3475734633, 3524038396, 3535174312, 3568181087, 3573863551, 3601478655, 3616089666, 3634660983, 3638082858, 3663341586,
    3663896169, 3666239815, 3666896632, 3688939408, 3740948313, 3745215916, 3749333696, 3753692454, 3755338324, 3767841471,
    3797950766, 3802757032, 3806690332, 3847761506, 3926353328, 3952104171, 3984387113, 3988751453, 3997353267, 4002300972,
    4045299578, 4074593561, 4077119393, 4082928848, 4084367249, 4085076160, 4113422219, 4146949428, 4152012084, 4160462271,
    4161440937, 4163677892, 4188085033, 4203793682, 4204174059, 4222899547, 4242317911
  };
}
