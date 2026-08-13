using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Ads;

public partial class Ads
{
  private enum AdKind
  {
    ScreenText,
    HudSay,
    ChatSay
  }

  private class QueueItem
  {
    public AdKind Kind;
    public int Index;
    public float Life;
    public float Timer;
  }

  private class AdQueue
  {
    public readonly List<QueueItem> Items = new();
    public int Cursor;
    public float NextAt = -1f;
    public QueueItem? Active;
    public float EndAt;
  }

  private readonly List<AdQueue> _queues = new();

  private readonly CPointWorldText?[] _screenTexts = new CPointWorldText?[MaxSlots];
  private readonly ScreenTextAd?[] _screenTextSource = new ScreenTextAd?[MaxSlots];
  private readonly float[] _lastPitch = new float[MaxSlots];
  private readonly float[] _lastYaw = new float[MaxSlots];
  private readonly float[] _lastEyeZ = new float[MaxSlots];
  private readonly bool[] _hudShown = new bool[MaxSlots];

  private readonly ScreenTextAd?[] _overrideText = new ScreenTextAd?[MaxSlots];
  private readonly float[] _overrideTextUntil = new float[MaxSlots];
  private readonly string?[] _overrideHud = new string?[MaxSlots];
  private readonly float[] _overrideHudUntil = new float[MaxSlots];

  private ScreenTextAd? _activeScreenText;
  private HudSayAd? _activeHudAd;

  private void BuildQueues()
  {
    ResetQueues();
    _queues.Clear();

    bool global = Config.QueueMode.Equals("global", StringComparison.OrdinalIgnoreCase);

    var screenText = new AdQueue();
    for (int i = 0; i < _data.ScreenTexts.Count; i++)
    {
      var ad = _data.ScreenTexts[i];
      screenText.Items.Add(new QueueItem { Kind = AdKind.ScreenText, Index = i, Life = ad.Life, Timer = ad.Timer });
    }

    var hudSay = new AdQueue();
    for (int i = 0; i < _data.HudSays.Count; i++)
    {
      var ad = _data.HudSays[i];
      hudSay.Items.Add(new QueueItem { Kind = AdKind.HudSay, Index = i, Life = ad.Life, Timer = ad.Timer });
    }

    var chatSay = new AdQueue();
    for (int i = 0; i < _data.ChatSays.Count; i++)
    {
      var ad = _data.ChatSays[i];
      chatSay.Items.Add(new QueueItem { Kind = AdKind.ChatSay, Index = i, Life = 0f, Timer = ad.Timer });
    }

    if (global)
    {
      var single = new AdQueue();
      single.Items.AddRange(screenText.Items);
      single.Items.AddRange(hudSay.Items);
      single.Items.AddRange(chatSay.Items);
      _queues.Add(single);
      return;
    }

    _queues.Add(screenText);
    _queues.Add(hudSay);
    _queues.Add(chatSay);
  }

  private void ResetQueues()
  {
    foreach (var queue in _queues)
    {
      queue.Active = null;
      queue.Cursor = 0;
      queue.NextAt = -1f;
      queue.EndAt = 0f;
    }

    _activeScreenText = null;
    _activeHudAd = null;

    for (int slot = 0; slot < MaxSlots; slot++)
    {
      _overrideText[slot] = null;
      _overrideHud[slot] = null;
    }
  }

  private void OnTick()
  {
    float now = Server.CurrentTime;

    foreach (var queue in _queues)
      TickQueue(queue, now);

    RenderPlayers(now);
  }

  private void TickQueue(AdQueue queue, float now)
  {
    if (queue.Items.Count == 0)
      return;

    if (queue.NextAt < 0f)
      queue.NextAt = now + MathF.Max(0f, queue.Items[queue.Cursor].Timer);

    if (queue.Active != null)
    {
      if (now < queue.EndAt)
        return;

      Deactivate(queue.Active);
      queue.Active = null;
      queue.NextAt = now + MathF.Max(0f, queue.Items[queue.Cursor].Timer);
      return;
    }

    if (now < queue.NextAt)
      return;

    var item = queue.Items[queue.Cursor];
    queue.Cursor = (queue.Cursor + 1) % queue.Items.Count;
    queue.Active = item;
    queue.EndAt = now + MathF.Max(0f, item.Life);
    Activate(item);
  }

  private void Activate(QueueItem item)
  {
    switch (item.Kind)
    {
      case AdKind.ScreenText:
        if (item.Index < _data.ScreenTexts.Count)
          _activeScreenText = _data.ScreenTexts[item.Index];
        break;

      case AdKind.HudSay:
        if (item.Index < _data.HudSays.Count)
          _activeHudAd = _data.HudSays[item.Index];
        break;

      case AdKind.ChatSay:
        if (item.Index < _data.ChatSays.Count)
          PrintChatAd(_data.ChatSays[item.Index]);
        break;
    }
  }

  private void Deactivate(QueueItem item)
  {
    switch (item.Kind)
    {
      case AdKind.ScreenText:
        _activeScreenText = null;
        break;

      case AdKind.HudSay:
        _activeHudAd = null;
        break;
    }
  }

  private void RenderPlayers(float now)
  {
    for (int slot = 0; slot < MaxSlots; slot++)
    {
      var player = Utilities.GetPlayerFromSlot(slot);
      if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
      {
        RemoveScreenText(slot);
        _screenTextSource[slot] = null;
        _hudShown[slot] = false;
        continue;
      }

      if (_overrideText[slot] != null && now >= _overrideTextUntil[slot])
        _overrideText[slot] = null;

      if (_overrideHud[slot] != null && now >= _overrideHudUntil[slot])
        _overrideHud[slot] = null;

      RenderScreenText(slot, player);
      RenderHud(slot, player);
    }
  }

  private void RenderScreenText(int slot, CCSPlayerController player)
  {
    var ad = _overrideText[slot];
    if (ad == null && _activeScreenText != null && CanSee(player, _activeScreenText.Flag, _activeScreenText.IgnoreFlag))
      ad = _activeScreenText;

    if (!ReferenceEquals(ad, _screenTextSource[slot]))
    {
      RemoveScreenText(slot);
      _screenTextSource[slot] = ad;
    }

    if (ad == null)
      return;

    var pawn = player.PlayerPawn.Value;
    if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
    {
      RemoveScreenText(slot);
      return;
    }

    var entity = _screenTexts[slot];
    if (entity == null || !entity.IsValid)
    {
      CreateScreenText(slot, pawn, ad);
      return;
    }

    PlaceScreenText(slot, pawn, ad, false);
  }

  private void RenderHud(int slot, CCSPlayerController player)
  {
    if (_menus.IsOpen(player))
      return;

    string? html = _overrideHud[slot];
    if (html == null && _activeHudAd != null && CanSee(player, _activeHudAd.Flag, _activeHudAd.IgnoreFlag))
      html = _activeHudAd.Text;

    if (html != null)
    {
      player.PrintToCenterHtml(html);
      _hudShown[slot] = true;
      return;
    }

    if (_hudShown[slot])
    {
      player.PrintToCenterHtml(" ");
      _hudShown[slot] = false;
    }
  }

  private void PrintChatAd(ChatSayAd ad)
  {
    if (string.IsNullOrWhiteSpace(ad.Text))
      return;

    var lines = ad.Text.Replace("<br>", "\n").Split('\n');

    foreach (var player in Utilities.GetPlayers())
    {
      if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
        continue;

      if (!CanSee(player, ad.Flag, ad.IgnoreFlag))
        continue;

      foreach (var line in lines)
        player.PrintToChat($" {CC.Parse(line)}");
    }
  }

  private void CreateScreenText(int slot, CCSPlayerPawn pawn, ScreenTextAd ad)
  {
    if (string.IsNullOrWhiteSpace(ad.Text))
      return;

    var entity = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext");
    if (entity == null || entity.Handle == IntPtr.Zero)
      return;

    entity.MessageText = ad.Text.Replace("<br>", "\n");
    entity.Enabled = true;
    entity.Fullbright = true;
    entity.FontSize = ad.Size <= 0f ? 32f : ad.Size;
    entity.WorldUnitsPerPx = Config.UnitsPerPx;
    if (!string.IsNullOrEmpty(Config.Font))
      entity.FontName = Config.Font;
    entity.Color = Util.ParseColor(ad.Color, Color.White);
    entity.JustifyHorizontal = ParseJustify(ad.Justify);
    entity.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_TOP;
    entity.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;
    entity.DrawBackground = ad.Background;
    entity.BackgroundBorderHeight = 0.1f;
    entity.BackgroundBorderWidth = 0.1f;
    entity.DispatchSpawn();

    entity.AcceptInput("SetParent", pawn, null, "!activator");
    _screenTexts[slot] = entity;

    PlaceScreenText(slot, pawn, ad, true);
  }

  private void PlaceScreenText(int slot, CCSPlayerPawn pawn, ScreenTextAd ad, bool force)
  {
    var entity = _screenTexts[slot];
    if (entity == null || !entity.IsValid)
      return;

    var eyeAngles = pawn.EyeAngles;
    float eyeZ = pawn.ViewOffset?.Z ?? 64f;

    if (!force
        && MathF.Abs(_lastPitch[slot] - eyeAngles.X) < 0.01f
        && MathF.Abs(_lastYaw[slot] - eyeAngles.Y) < 0.01f
        && MathF.Abs(_lastEyeZ[slot] - eyeZ) < 0.01f)
      return;

    _lastPitch[slot] = eyeAngles.X;
    _lastYaw[slot] = eyeAngles.Y;
    _lastEyeZ[slot] = eyeZ;

    var origin = pawn.AbsOrigin!;

    double d2r = Math.PI / 180.0;
    double pitch = eyeAngles.X * d2r, yaw = eyeAngles.Y * d2r;
    double sp = Math.Sin(pitch), cp = Math.Cos(pitch), sy = Math.Sin(yaw), cy = Math.Cos(yaw);

    float fx = (float)(cp * cy), fy = (float)(cp * sy), fz = (float)(-sp);
    float rx = (float)sy, ry = (float)(-cy);
    float ux = (float)(sp * cy), uy = (float)(sp * sy), uz = (float)cp;

    float ex = origin.X, ey = origin.Y, ez = origin.Z + eyeZ;
    float forward = Config.Forward;

    var position = new Vector(
      ex + fx * forward + rx * ad.X + ux * ad.Y,
      ey + fy * forward + ry * ad.X + uy * ad.Y,
      ez + fz * forward + uz * ad.Y);

    entity.Teleport(position, new QAngle(0f, eyeAngles.Y + 270f, 90f - eyeAngles.X), null);
  }

  private void RemoveScreenText(int slot)
  {
    var entity = _screenTexts[slot];
    _screenTexts[slot] = null;
    if (entity != null && entity.IsValid)
      entity.Remove();
  }

  private void ClearScreenTexts()
  {
    for (int slot = 0; slot < MaxSlots; slot++)
    {
      RemoveScreenText(slot);
      _screenTextSource[slot] = null;
    }
  }

  private static PointWorldTextJustifyHorizontal_t ParseJustify(string value)
  {
    return value.ToLowerInvariant() switch
    {
      "center" => PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER,
      "right" => PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_RIGHT,
      _ => PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT
    };
  }
}
