using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;

public enum MuteMode : byte { None, Enemy, Team, All }

public class Sesler : BasePlugin
{
  public override string ModuleName => "Sesler";
  public override string ModuleVersion => "1.0.1";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "Oyuncu ses kontrolü - Bıçak, Silah, Ayak/Yürüme, Oyuncu/Hasar, MVP Müzik";

  private readonly Dictionary<ulong, Pref> _prefs = new();

  private static readonly string[] ModeLabels = { "Herkesin Açık", "Düşmanları Sustur", "Takımı Sustur", "Herkesi Sustur" };
  private static readonly string[] ModeColors = { "green", "orange", "yellow", "red" };

  public override void Load(bool hotReload)
  {
    HookUserMessage(208, OnSound, HookMode.Pre);
    HookUserMessage(369, OnWeaponSound, HookMode.Pre);
    HookUserMessage(452, OnWeaponEvent, HookMode.Pre);
    RegisterEventHandler<EventRoundMvp>(OnRoundMvp);
  }

  public override void Unload(bool hotReload)
  {
    UnhookUserMessage(208, OnSound, HookMode.Pre);
    UnhookUserMessage(369, OnWeaponSound, HookMode.Pre);
    UnhookUserMessage(452, OnWeaponEvent, HookMode.Pre);
  }

  private Pref GetPref(CCSPlayerController p)
  {
    if (!_prefs.TryGetValue(p.SteamID, out var pref))
    {
      pref = new Pref();
      _prefs[p.SteamID] = pref;
    }
    return pref;
  }

  [ConsoleCommand("css_ses", "Ses menüsünü açar")]
  public void OnCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid) return;
    ShowMainMenu(player);
  }

  private void ShowMainMenu(CCSPlayerController player)
  {
    var pref = GetPref(player);
    var menu = new CenterHtmlMenu("<font color='#8899a6' class='fontSize-l'><img src='https://images.weserv.nl/?url=em-content.zobj.net/source/twitter/408/speaker-high-volume_1f50a.png&w=24&h=24&fit=cover'> Sesler <img src='https://images.weserv.nl/?url=em-content.zobj.net/source/twitter/408/speaker-high-volume_1f50a.png&w=24&h=24&fit=cover'></font>", this);

    var items = new (string Label, Func<Pref, MuteMode> Get, Action<Pref, MuteMode> Set)[]
    {
      ("Bıçak Sesleri", p => p.Knife, (p, m) => p.Knife = m),
      ("Silah Sesleri", p => p.Weapon, (p, m) => p.Weapon = m),
      ("Ayak/Yürüme Sesleri", p => p.Foot, (p, m) => p.Foot = m),
      ("Oyuncu/Hasar Sesleri", p => p.Player, (p, m) => p.Player = m),
      ("MVP Müzik", p => p.Mvp, (p, m) => p.Mvp = m)
    };

    foreach (var item in items)
    {
      var current = item.Get(pref);
      menu.AddMenuOption($"{item.Label}: {StateLabel(current)}", (p, o) =>
      {
        ShowSubMenu(player, item.Label, item.Get, item.Set);
      });
    }

    MenuManager.OpenCenterHtmlMenu(this, player, menu);
  }

  private void ShowSubMenu(CCSPlayerController player, string label, Func<Pref, MuteMode> get, Action<Pref, MuteMode> set)
  {
    var pref = GetPref(player);
    var menu = new CenterHtmlMenu($"<font color='#8899a6'>{label}</font>", this);

    for (int i = 0; i < 4; i++)
    {
      var mode = (MuteMode)i;
      var current = get(pref);
      var prefix = current == mode ? "► " : "";
      var color = ModeColors[i];
      menu.AddMenuOption($"{prefix}<font color='{color}'>{ModeLabels[i]}</font>", (p, o) =>
      {
        set(pref, mode);
        ShowMainMenu(player);
      });
    }

    menu.AddMenuOption("<font color='gray'>← Geri</font>", (p, o) => ShowMainMenu(player));
    MenuManager.OpenCenterHtmlMenu(this, player, menu);
  }

  private string StateLabel(MuteMode mode)
  {
    var idx = (int)mode;
    return $"<font color='{ModeColors[idx]}'>{ModeLabels[idx]}</font>";
  }

  private HookResult OnSound(UserMessage msg)
  {
    var hash = msg.ReadUInt("soundevent_hash");

    var soundType = GetSoundType(hash);
    if (soundType == SoundType.None) return HookResult.Continue;

    var entityIndex = msg.ReadInt("source_entity_index");
    var entity = Utilities.GetEntityFromIndex<CBaseEntity>(entityIndex);

    FilterRecipients(msg, entity, pref => soundType switch
    {
      SoundType.Knife => pref.Knife,
      SoundType.Foot => pref.Foot,
      SoundType.Player => pref.Player,
      _ => MuteMode.None
    });

    return msg.Recipients.Count == 0 ? HookResult.Stop : HookResult.Continue;
  }

  private enum SoundType : byte { None, Knife, Foot, Player }

  private SoundType GetSoundType(uint hash)
  {
    if (KnifeHashes.Contains(hash)) return SoundType.Knife;
    if (FootHashes.Contains(hash)) return SoundType.Foot;
    if (PlayerHashes.Contains(hash)) return SoundType.Player;
    return SoundType.None;
  }

  private HookResult OnWeaponSound(UserMessage um)
  {
    var entityIndex = um.ReadInt("entidx");
    var entity = Utilities.GetEntityFromIndex<CBaseEntity>(entityIndex);
    if (entity == null)
      return HookResult.Continue;

    var pawn = entity.As<CBasePlayerPawn>();
    if (pawn == null || !pawn.IsValid || pawn.DesignerName != "player")
      return HookResult.Continue;

    var soundName = um.ReadString("sound") ?? string.Empty;
    if (soundName.Length == 0)
      return HookResult.Continue;

    var lower = soundName.ToLowerInvariant();
    bool looksLikeWeapon = lower.Contains("weapons/") || lower.Contains("weapon_") || lower.Contains("wpn_");
    if (!looksLikeWeapon)
      return HookResult.Continue;

    FilterRecipients(um, entity, pref => pref.Weapon);
    return um.Recipients.Count == 0 ? HookResult.Stop : HookResult.Continue;
  }

  private HookResult OnWeaponEvent(UserMessage um)
  {
    var entityHandle = um.ReadUInt("player");
    var entity = Utilities.GetEntityFromIndex<CBaseEntity>((int)(entityHandle & 0x7FF));

    FilterRecipients(um, entity, pref => pref.Weapon);
    return um.Recipients.Count == 0 ? HookResult.Stop : HookResult.Continue;
  }

  private HookResult OnRoundMvp(EventRoundMvp @event, GameEventInfo info)
  {
    if (@event?.Userid == null || !@event.Userid.IsValid) return HookResult.Continue;

    var worldEntity = Utilities.GetEntityFromIndex<CBaseEntity>(0);
    if (worldEntity == null || !worldEntity.IsValid) return HookResult.Continue;

    foreach (var player in Utilities.GetPlayers())
    {
      if (!player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected) continue;
      if (GetPref(player).Mvp != MuteMode.All) continue;

      worldEntity.EmitSound("StopSoundEvents.StopAllMusic", new RecipientFilter(player));
    }

    return HookResult.Continue;
  }

  private void FilterRecipients(UserMessage msg, CBaseEntity? soundSource, Func<Pref, MuteMode> getMode)
  {
    foreach (var listener in msg.Recipients.ToList())
    {
      if (listener == null || !listener.IsValid) continue;

      var pref = GetPref(listener);
      var mode = getMode(pref);

      if (ShouldMute(listener, soundSource, mode))
        msg.Recipients.Remove(listener);
    }
  }

  private bool ShouldMute(CCSPlayerController listener, CBaseEntity? soundSource, MuteMode mode)
  {
    if (mode == MuteMode.None) return false;
    if (mode == MuteMode.All) return true;

    if (soundSource == null) return false;

    var listenerTeam = listener.TeamNum;
    var sourceTeam = soundSource.TeamNum;

    if (mode == MuteMode.Enemy)
      return listenerTeam != sourceTeam;

    if (mode == MuteMode.Team)
      return listenerTeam == sourceTeam;

    return false;
  }

  private static readonly HashSet<uint> KnifeHashes = new()
  {
    3475734633,    1769891506,    3634660983
  };

  private static readonly HashSet<uint> FootHashes = new()
  {
    2800858936, 70011614, 1194677450, 1016523349, 2240518199, 3218103073, 520432428, 1818046345, 2207486967,    2302139631, 1939055066, 1409986305, 1803111098, 4113422219, 3997353267, 3009312615, 123085364, 782454593,    3257325156, 3434104102, 2745524735, 117596568, 29217150, 3460445620, 2684452812, 2067683805, 1388885460,    413358161, 988265811, 3802757032, 2633527058, 1627020521, 602548457, 859178236, 3749333696, 2899365092,    2061955732, 1535891875, 3368720745, 3057812547, 135189076, 2790760284, 2448803175, 3753692454, 3666896632,    3166948458, 3099536373, 1690105992, 115843229, 1763490157, 2546391140, 515548944, 1517575510, 1248619277,    1395892944, 2300993891, 1183624286, 540697918, 2829617974, 1826799645, 3193435079, 2860219006, 1855038793,    2892812682, 3342414459, 144629619, 721782259, 2133235849, 3161194970, 819435812, 2804393637, 4222899547,    1664187801, 2714245023, 1692050905, 961838155, 2638406226, 3008782656, 2070478448, 1247386781, 58439651,    3172583021, 1557420499, 1485322532, 1598540856, 4163677892, 4082928848, 2708661994, 893108375, 1506215040,    2231399653, 1116700262, 2594927130, 1019414932, 1218015996, 417910549, 3299941720, 931543849, 2026488395,    84876002, 1403457606, 2189706910, 1543034, 892882552, 70939233, 1404198078, 1664329401, 822973253,    3797950766, 4203793682, 3952104171, 1163426340, 870100484, 935062317, 1161855519, 1253503839, 1635413700,    2333790984, 96240187, 1165397261, 4084367249, 3109879199, 3984387113, 4045299578, 2551626319, 2479376962,    4085076160, 1661204257, 2236021746, 1440734007, 585390608, 1194093029, 3755338324, 4152012084, 757978684,    1448154350, 2053595705, 1909915699, 765706800, 2722081556, 1540837791, 3123711576, 1770765328, 1761772772,    1424056132, 4160462271, 3806690332, 740474905
  };

  private static readonly HashSet<uint> PlayerHashes = new()
  {
    3688939408, 2703682875, 46413566, 2735369596, 1961884255, 318971924, 662078688, 3469219129, 4161440937,    3568181087, 663530947, 1499777741, 202030084, 3065316423, 1682747253, 427534867, 2369733616, 3666239815,    297379099, 2804654127, 4188085033, 3030200692, 1734994609, 4077119393, 2696334288, 129081149, 2158707679,    3601478655, 3616089666, 2064477315, 1489357772, 3745215916, 839762874, 850911881, 4146949428, 4204174059,    1412313471, 1792523944, 1815352525, 2967038404, 142772671, 1407794113, 3204513405, 2883205713, 769561685,    3103360935, 2381346641, 803727624, 1284373691, 1543118744, 2056150061, 3767841471, 3988751453, 1771184788,    708038349, 3049902652, 3638082858, 1193078452, 3535174312, 2831007164, 524041390, 2447320252, 3124768561,    856190898, 3663341586, 1904605142, 795825195, 4242317911, 4002300972, 3259510958, 2106508305, 963985059,    62938228, 3926353328, 282152614, 2284698275, 2019962436, 3663896169, 3573863551, 1823342283, 2192712263,    3396420465, 2323025056, 3524038396, 2719685137, 2310318859, 2020934318, 3740948313, 2902143738, 400609565,    2316086169, 604181152, 2486534908
  };
}

public class Pref
{
  public MuteMode Knife;
  public MuteMode Foot;
  public MuteMode Player;
  public MuteMode Weapon;
  public MuteMode Mvp;
}
