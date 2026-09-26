using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Ads;

public partial class Ads
{
  private static readonly Random Rng = new();

  private ScreenTextAd[] _eventScreenTexts = Array.Empty<ScreenTextAd>();
  private float[,] _eventCooldown = new float[0, MaxSlots];
  private readonly int[] _hudEventAd = new int[MaxSlots];
  private readonly int[] _textEventAd = new int[MaxSlots];
  private readonly HashSet<string> _eventNames = new(StringComparer.Ordinal);
  private string[] _eventKeys = Array.Empty<string>();
  private string[] _eventTypes = Array.Empty<string>();

  private void BuildEvents()
  {
    int count = _data.Events.Count;
    _eventScreenTexts = new ScreenTextAd[count];
    _eventCooldown = new float[count, MaxSlots];
    _eventKeys = new string[count];
    _eventTypes = new string[count];
    _eventNames.Clear();

    Array.Fill(_hudEventAd, -1);
    Array.Fill(_textEventAd, -1);

    for (int i = 0; i < count; i++)
    {
      var ad = _data.Events[i];
      _eventScreenTexts[i] = ad.ToScreenText();
      _eventKeys[i] = NormalizeEvent(ad.Event);
      _eventTypes[i] = ad.Type.Trim().ToLowerInvariant();

      if (!string.IsNullOrWhiteSpace(ad.Text))
        _eventNames.Add(_eventKeys[i]);
    }
  }

  private bool Wants(string eventName) => _eventNames.Contains(eventName);

  private void RegisterEventAds()
  {
    RegisterEventHandler<EventRoundStart>(OnRoundStart);
    RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
    RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
    RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
    RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
    RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
    RegisterEventHandler<EventBombBeginplant>(OnBombBeginPlant);
    RegisterEventHandler<EventBombPlanted>(OnBombPlanted);
    RegisterEventHandler<EventBombBegindefuse>(OnBombBeginDefuse);
    RegisterEventHandler<EventBombDefused>(OnBombDefused);
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
  {
    Server.NextFrame(SpawnWorldAds);

    if (Wants("round_start"))
      Trigger("round_start", null, null);
    return HookResult.Continue;
  }

  private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
  {
    if (!Wants("round_end"))
      return HookResult.Continue;

    Trigger("round_end", null, null, ("winner", TeamName(@event.Winner)));
    return HookResult.Continue;
  }

  private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
  {
    if (!Wants("player_hurt"))
      return HookResult.Continue;

    Trigger("player_hurt", @event.Userid, @event.Attacker,
      ("damage", @event.DmgHealth.ToString()),
      ("health", @event.Health.ToString()),
      ("armor", @event.Armor.ToString()),
      ("weapon", @event.Weapon));
    return HookResult.Continue;
  }

  private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
  {
    if (!Wants("player_death"))
      return HookResult.Continue;

    Trigger("player_death", @event.Userid, @event.Attacker,
      ("weapon", @event.Weapon),
      ("headshot", @event.Headshot ? "1" : "0"));
    return HookResult.Continue;
  }

  private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
  {
    if (!Wants("player_team"))
      return HookResult.Continue;

    if (@event.Disconnect)
      return HookResult.Continue;

    Trigger("player_team", @event.Userid, null, ("team", TeamName(@event.Team)));
    return HookResult.Continue;
  }

  private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
  {
    if (!Wants("player_connect_full"))
      return HookResult.Continue;

    var player = @event.Userid;
    if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
      return HookResult.Continue;

    int userId = Util.UserId(player);
    AddTimer(2f, () =>
    {
      var target = Util.FromUserId(userId);
      if (target != null && !target.IsBot)
        Trigger("player_connect_full", target, null);
    });
    return HookResult.Continue;
  }

  private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    int slot = @event.Userid?.Slot ?? -1;
    if (slot < 0 || slot >= MaxSlots)
      return HookResult.Continue;

    RemoveScreenText(slot);
    _screenTextSource[slot] = null;
    _overrideText[slot] = null;
    _overrideHud[slot] = null;
    _hudShown[slot] = false;
    _selected[slot] = null;
    _awaiting[slot] = PendingInput.None;
    _axis[slot] = 0;

    for (int i = 0; i < _eventCooldown.GetLength(0); i++)
      _eventCooldown[i, slot] = 0f;

    return HookResult.Continue;
  }

  private HookResult OnBombBeginPlant(EventBombBeginplant @event, GameEventInfo info)
  {
    if (!Wants("bomb_beginplant"))
      return HookResult.Continue;

    Trigger("bomb_beginplant", @event.Userid, null, ("site", SiteName()));
    return HookResult.Continue;
  }

  private HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
  {
    if (!Wants("bomb_planted"))
      return HookResult.Continue;

    var planter = @event.Userid;
    Server.NextFrame(() => Trigger("bomb_planted", planter, null, ("site", SiteName())));
    return HookResult.Continue;
  }

  private HookResult OnBombBeginDefuse(EventBombBegindefuse @event, GameEventInfo info)
  {
    if (!Wants("bomb_begindefuse"))
      return HookResult.Continue;

    Trigger("bomb_begindefuse", @event.Userid, null, ("kit", @event.Haskit ? "1" : "0"));
    return HookResult.Continue;
  }

  private HookResult OnBombDefused(EventBombDefused @event, GameEventInfo info)
  {
    if (!Wants("bomb_defused"))
      return HookResult.Continue;

    Trigger("bomb_defused", @event.Userid, null, ("site", SiteName()));
    return HookResult.Continue;
  }

  private void Trigger(string eventName, CCSPlayerController? victim, CCSPlayerController? attacker,
    params (string Key, string Value)[] vars)
  {
    float now = Server.CurrentTime;

    for (int i = 0; i < _data.Events.Count && i < _eventKeys.Length; i++)
    {
      var ad = _data.Events[i];
      if (!string.Equals(_eventKeys[i], eventName, StringComparison.Ordinal))
        continue;

      if (string.IsNullOrWhiteSpace(ad.Text))
        continue;

      if (ad.Chance < 100 && Rng.Next(100) >= ad.Chance)
        continue;

      string text = Substitute(ad.Text, victim, attacker, vars);

      foreach (var recipient in Recipients(ad.Target, victim, attacker))
      {
        int slot = recipient.Slot;

        if (!CanSee(recipient, ad.Flag, ad.IgnoreFlag))
          continue;

        if (ad.Cooldown > 0f && now < _eventCooldown[i, slot] && !IsRefresh(ad, i, slot, now))
          continue;

        _eventCooldown[i, slot] = now + ad.Cooldown;
        Show(ad, i, recipient, slot, Fill(text, recipient), now);
      }
    }
  }

  private bool IsRefresh(EventAd ad, int index, int slot, float now) => _eventTypes[index] switch
  {
    "hudsay" => _hudEventAd[slot] == index && now < _overrideHudUntil[slot],
    "screentext" => _textEventAd[slot] == index && now < _overrideTextUntil[slot],
    _ => false
  };

  private void Show(EventAd ad, int index, CCSPlayerController recipient, int slot, string text, float now)
  {
    switch (_eventTypes[index])
    {
      case "hudsay":
        _overrideHud[slot] = text;
        _overrideHudUntil[slot] = now + MathF.Max(0.1f, ad.Life);
        _hudEventAd[slot] = index;
        break;

      case "screentext":
        var template = _eventScreenTexts[index];
        _overrideText[slot] = ReferenceEquals(template.Text, text)
          ? template
          : new ScreenTextAd
          {
            Text = text,
            Life = template.Life,
            X = template.X,
            Y = template.Y,
            Size = template.Size,
            Color = template.Color,
            Justify = template.Justify,
            Background = template.Background
          };
        _overrideTextUntil[slot] = now + MathF.Max(0.1f, ad.Life);
        _textEventAd[slot] = index;
        break;

      default:
        foreach (var line in text.Replace("<br>", "\n").Split('\n'))
          recipient.PrintToChat($" {CC.Parse(line)}");
        break;
    }
  }

  private IEnumerable<CCSPlayerController> Recipients(string target, CCSPlayerController? victim, CCSPlayerController? attacker)
  {
    var primary = victim ?? attacker;

    switch (target.ToLowerInvariant())
    {
      case "victim":
        if (IsRecipient(victim))
          yield return victim!;
        break;

      case "attacker":
        if (IsRecipient(attacker))
          yield return attacker!;
        break;

      case "player":
        if (IsRecipient(primary))
          yield return primary!;
        break;

      case "both":
        if (IsRecipient(victim))
          yield return victim!;
        if (IsRecipient(attacker) && !ReferenceEquals(victim, attacker))
          yield return attacker!;
        break;

      case "ct":
      case "t":
        byte team = target.Equals("ct", StringComparison.OrdinalIgnoreCase)
          ? (byte)CsTeam.CounterTerrorist
          : (byte)CsTeam.Terrorist;
        foreach (var player in Utilities.GetPlayers())
        {
          if (IsRecipient(player) && player.TeamNum == team)
            yield return player;
        }
        break;

      default:
        foreach (var player in Utilities.GetPlayers())
        {
          if (IsRecipient(player))
            yield return player;
        }
        break;
    }
  }

  private static bool IsRecipient(CCSPlayerController? player) =>
    player != null && player.IsValid && !player.IsBot && !player.IsHLTV;

  private static string Substitute(string text, CCSPlayerController? victim, CCSPlayerController? attacker,
    (string Key, string Value)[] vars)
  {
    var primary = victim ?? attacker;

    text = text.Replace("{victim}", victim?.PlayerName ?? "")
               .Replace("{attacker}", attacker?.PlayerName ?? "")
               .Replace("{player}", primary?.PlayerName ?? "")
               .Replace("{map}", Server.MapName);

    foreach (var (key, value) in vars)
      text = text.Replace("{" + key + "}", value);

    return text;
  }

  private static string NormalizeEvent(string name)
  {
    return name.Trim().ToLowerInvariant() switch
    {
      "bomb_plant" => "bomb_beginplant",
      "bomb_defuse" => "bomb_begindefuse",
      var other => other
    };
  }

  private static string TeamName(int team) => team switch
  {
    (int)CsTeam.Terrorist => "T",
    (int)CsTeam.CounterTerrorist => "CT",
    (int)CsTeam.Spectator => "Spectator",
    _ => "Draw"
  };

  private static string SiteName()
  {
    foreach (var c4 in Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4"))
    {
      if (c4 != null && c4.IsValid)
        return c4.BombSite == 1 ? "B" : "A";
    }

    return "";
  }
}
