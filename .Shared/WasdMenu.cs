using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace ByDexter.Shared;

public class WasdItem
{
  public string Text = "";
  public Action<CCSPlayerController>? OnSelect;
  public bool Enabled = true;
}

public class WasdMenuManager
{
  private const int PerPage = 5;

  private readonly BasePlugin _plugin;
  private readonly Func<string> _scrollLabel;
  private readonly Func<string> _selectLabel;
  private readonly Func<string> _exitLabel;

  private readonly Dictionary<int, Session> _sessions = new();
  private readonly Dictionary<int, Dictionary<string, int>> _memory = new();
  private bool _tickHooked;

  private class Session
  {
    public CCSPlayerController Player = null!;
    public string Title = "";
    public List<WasdItem> Items = new();
    public int Selected;
    public PlayerButtons OldButtons;
    public string Html = "";
  }

  public WasdMenuManager(BasePlugin plugin, Func<string> scrollLabel, Func<string> selectLabel, Func<string> exitLabel)
  {
    _plugin = plugin;
    _scrollLabel = scrollLabel;
    _selectLabel = selectLabel;
    _exitLabel = exitLabel;
  }

  public void Open(CCSPlayerController player, string title, List<WasdItem> items)
  {
    if (items.Count == 0)
      return;

    int slot = player.Slot;

    if (_sessions.TryGetValue(slot, out var existing))
    {
      if (!_memory.TryGetValue(slot, out var mem))
        _memory[slot] = mem = new();
      mem[existing.Title] = existing.Selected;
    }

    int selected = 0;
    if (_memory.TryGetValue(slot, out var stored) && stored.TryGetValue(title, out var last))
      selected = Math.Clamp(last, 0, items.Count - 1);

    var session = new Session
    {
      Player = player,
      Title = title,
      Items = items,
      Selected = selected,
      OldButtons = _sessions.TryGetValue(slot, out var prev) ? prev.OldButtons : player.Buttons
    };
    session.Html = BuildHtml(session);
    _sessions[slot] = session;

    if (!_tickHooked)
    {
      _plugin.RegisterListener<OnTick>(OnTick);
      _tickHooked = true;
    }
  }

  public bool IsOpen(CCSPlayerController player) => _sessions.ContainsKey(player.Slot);

  public void Close(CCSPlayerController player)
  {
    if (_sessions.TryGetValue(player.Slot, out var session))
      CloseSession(session, true);
  }

  public void Clear()
  {
    foreach (var session in _sessions.Values)
    {
      if (session.Player.IsValid)
        session.Player.PrintToCenterHtml(" ");
    }
    _sessions.Clear();
    _memory.Clear();
  }

  private void OnTick()
  {
    if (_sessions.Count == 0)
      return;

    foreach (var session in _sessions.Values.ToList())
    {
      var player = session.Player;

      if (player == null || !player.IsValid)
      {
        CloseSession(session, false);
        continue;
      }

      var buttons = player.Buttons;
      var old = session.OldButtons;
      session.OldButtons = buttons;

      bool Pressed(PlayerButtons b) => (buttons & b) != 0 && (old & b) == 0;

      if (Pressed(PlayerButtons.Forward))
        Scroll(session, -1);
      else if (Pressed(PlayerButtons.Back))
        Scroll(session, 1);
      else if (Pressed(PlayerButtons.Use))
      {
        Select(session);
        continue;
      }
      else if (Pressed(PlayerButtons.Reload))
      {
        CloseSession(session, true);
        continue;
      }

      player.PrintToCenterHtml(session.Html);
    }
  }

  private void Scroll(Session session, int direction)
  {
    int count = session.Items.Count;
    if (count == 0)
      return;

    int next = Math.Clamp(session.Selected + direction, 0, count - 1);
    if (next == session.Selected)
      return;

    session.Selected = next;
    session.Html = BuildHtml(session);
  }

  private void Select(Session session)
  {
    if (session.Selected < 0 || session.Selected >= session.Items.Count)
      return;

    var item = session.Items[session.Selected];
    if (!item.Enabled)
      return;

    item.OnSelect?.Invoke(session.Player);
  }

  private void CloseSession(Session session, bool clear)
  {
    _sessions.Remove(session.Player.Slot);
    _memory.Remove(session.Player.Slot);

    if (clear && session.Player.IsValid)
      session.Player.PrintToCenterHtml(" ");
  }

  private string BuildHtml(Session session)
  {
    int count = session.Items.Count;
    int page = session.Selected / PerPage;
    int totalPages = (count + PerPage - 1) / PerPage;
    int offset = page * PerPage;
    int end = Math.Min(offset + PerPage, count);

    var builder = new StringBuilder();
    builder.Append($"<font color='#ffb300'>{session.Title}</font>");
    if (totalPages > 1)
      builder.Append($" ({page + 1}/{totalPages})");
    builder.Append("<br>");

    for (int i = offset; i < end; i++)
    {
      var item = session.Items[i];
      builder.Append(i == session.Selected
        ? $"<font color='#ffb300'>▸ </font><font color='#ffffff'>{item.Text}</font><br>"
        : $"<font color='#7f7f7f'>{item.Text}</font><br>");
    }

    builder.Append(
      $"<font class='fontSize-s' color='#4AC7EE'>W/S {_scrollLabel()}</font> | " +
      $"<font class='fontSize-s' color='#76C97A'>E {_selectLabel()}</font> | " +
      $"<font class='fontSize-s' color='#FF8077'>R {_exitLabel()}</font>");

    return builder.ToString();
  }
}
