using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public partial class VIPCore
{
    private const int WasdPerPage = 5;

    private readonly Dictionary<int, WasdSession> _wasd = new();
    private readonly Dictionary<int, Dictionary<string, int>> _wasdMemory = new();
    private bool _wasdTickHooked;

    private class WasdSession
    {
        public CCSPlayerController Player = null!;
        public string Title = "";
        public List<(string display, Action<CCSPlayerController> onSelect)> Items = new();
        public int Selected;
        public PlayerButtons OldButtons;
        public string Html = "";
    }

    private void OpenWasdMenu(CCSPlayerController player, string title,
        List<(string display, Action<CCSPlayerController> onSelect)> items)
    {
        int slot = player.Slot;

        if (_wasd.TryGetValue(slot, out var existing))
        {
            if (!_wasdMemory.TryGetValue(slot, out var mem))
                _wasdMemory[slot] = mem = new();
            mem[existing.Title] = existing.Selected;
        }

        int selected = 0;
        if (_wasdMemory.TryGetValue(slot, out var stored) && stored.TryGetValue(title, out var last))
            selected = Math.Clamp(last, 0, Math.Max(0, items.Count - 1));

        var session = new WasdSession
        {
            Player = player,
            Title = title,
            Items = items,
            Selected = selected,
            OldButtons = TryGetButtons(player, out var current) ? current : 0
        };
        session.Html = BuildWasdHtml(session);
        _wasd[slot] = session;

        if (!_wasdTickHooked)
        {
            RegisterListener<OnTick>(WasdOnTick);
            _wasdTickHooked = true;
        }
    }

    private static bool TryGetButtons(CCSPlayerController player, out PlayerButtons buttons) =>
        VipModule.TryGetButtons(player, out buttons);

    private void WasdOnTick()
    {
        if (_wasd.Count == 0)
            return;

        foreach (var session in _wasd.Values.ToList())
        {
            var player = session.Player;

            if (player == null || !player.IsValid)
            {
                CloseWasd(session, false);
                continue;
            }

            if (!TryGetButtons(player, out var buttons))
            {
                player.PrintToCenterHtml(session.Html);
                continue;
            }

            var old = session.OldButtons;
            session.OldButtons = buttons;

            bool Pressed(PlayerButtons b) => (buttons & b) != 0 && (old & b) == 0;

            if (Pressed(PlayerButtons.Forward))
                WasdScroll(session, -1);
            else if (Pressed(PlayerButtons.Back))
                WasdScroll(session, 1);
            else if (Pressed(PlayerButtons.Use))
            {
                WasdSelect(session);
                continue;
            }
            else if (Pressed(PlayerButtons.Reload))
            {
                CloseWasd(session, true);
                continue;
            }

            player.PrintToCenterHtml(session.Html);
        }
    }

    private void WasdScroll(WasdSession session, int direction)
    {
        int count = session.Items.Count;
        if (count == 0)
            return;

        int next = Math.Clamp(session.Selected + direction, 0, count - 1);
        if (next == session.Selected)
            return;

        session.Selected = next;
        session.Html = BuildWasdHtml(session);
    }

    private void WasdSelect(WasdSession session)
    {
        if (session.Selected < 0 || session.Selected >= session.Items.Count)
            return;

        session.Items[session.Selected].onSelect(session.Player);
    }

    private void CloseWasd(WasdSession session, bool clear)
    {
        _wasd.Remove(session.Player.Slot);
        _wasdMemory.Remove(session.Player.Slot);

        if (clear && session.Player.IsValid)
            session.Player.PrintToCenterHtml(" ");
    }

    private string BuildWasdHtml(WasdSession session)
    {
        int count = session.Items.Count;
        int page = session.Selected / WasdPerPage;
        int totalPages = (count + WasdPerPage - 1) / WasdPerPage;
        int offset = page * WasdPerPage;
        int end = Math.Min(offset + WasdPerPage, count);

        var builder = new StringBuilder();
        builder.Append($"<font color='#ffb300'>{session.Title}</font>");
        if (totalPages > 1)
            builder.Append($" ({page + 1}/{totalPages})");
        builder.Append("<br>");

        for (int i = offset; i < end; i++)
        {
            string text = ChatColorUtil.ToHtml(session.Items[i].display);
            builder.Append(i == session.Selected
                ? $"<font color='#ffb300'>▸ </font><font color='#ffffff'>{text}</font><br>"
                : $"<font color='#7f7f7f'>{text}</font><br>");
        }

        builder.Append(
            $"<font class='fontSize-s' color='#4AC7EE'>W/S {Localizer["vip.wasd_scroll"]}</font> | " +
            $"<font class='fontSize-s' color='#76C97A'>E {Localizer["vip.wasd_select"]}</font> | " +
            $"<font class='fontSize-s' color='#FF8077'>R {Localizer["vip.wasd_exit"]}</font>");

        return builder.ToString();
    }
}
