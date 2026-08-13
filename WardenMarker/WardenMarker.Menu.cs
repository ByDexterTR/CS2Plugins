using System.Globalization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using ByDexter.Shared;

namespace WardenMarker;

public partial class WardenMarker
{
  private const string BackColor = "#FF8077";

  private void OnMarkerCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return;

    if (!HasAccess(player))
    {
      player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["marker.no_access"]}");
      return;
    }

    ShowMainMenu(player);
  }

  private void ShowMainMenu(CCSPlayerController? player)
  {
    if (player == null || !player.IsValid)
      return;

    var settings = Get(player);

    var items = new List<WasdItem>
    {
      new()
      {
        Text = Localizer["marker.clear"],
        OnSelect = p =>
        {
          Clear(p.Slot);
          p.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["marker.cleared"]}");
        }
      },
      new() { Text = Localizer["marker.ring_menu"], OnSelect = ShowRingMenu },
      new() { Text = Localizer["marker.disc_menu"], OnSelect = ShowDiscMenu },
      new()
      {
        Text = Localizer["marker.key", KeyLabel(settings.Key)],
        OnSelect = p =>
        {
          settings.Key = settings.Key switch
          {
            "use" => "inspect",
            "inspect" => "ping",
            "ping" => "off",
            _ => "use"
          };
          Remember(p);
          ShowMainMenu(p);
        }
      }
    };

    _menus.Open(player, Localizer["marker.menu_title"], items);
  }

  private void ShowRingMenu(CCSPlayerController? player)
  {
    if (player == null || !player.IsValid)
      return;

    var settings = Get(player);

    var items = new List<WasdItem>
    {
      new() { Text = Localizer["marker.back"], Color = BackColor, OnSelect = ShowMainMenu },
      new()
      {
        Text = Localizer["marker.color", Hex(settings.Ring.Color)],
        Color = Hex(settings.Ring.Color),
        OnSelect = p =>
        {
          settings.Ring.Color = Next(Config.Ring.Colors, settings.Ring.Color);
          Apply(p);
          ShowRingMenu(p);
        }
      },
      new()
      {
        Text = Localizer["marker.size", Number(settings.Ring.Size)],
        OnSelect = p =>
        {
          settings.Ring.Size = Next(Config.Ring.Sizes, settings.Ring.Size);
          Apply(p);
          ShowRingMenu(p);
        }
      },
      new()
      {
        Text = Localizer["marker.width", Number(settings.Ring.Width)],
        OnSelect = p =>
        {
          settings.Ring.Width = Next(Config.Ring.Widths, settings.Ring.Width);
          Apply(p);
          ShowRingMenu(p);
        }
      }
    };

    _menus.Open(player, Localizer["marker.ring_title"], items);
  }

  private void ShowDiscMenu(CCSPlayerController? player)
  {
    if (player == null || !player.IsValid)
      return;

    var settings = Get(player);

    var items = new List<WasdItem>
    {
      new() { Text = Localizer["marker.back"], Color = BackColor, OnSelect = ShowMainMenu },
      new()
      {
        Text = Localizer["marker.disc", StateLabel(settings.Disc.Enabled)],
        OnSelect = p =>
        {
          settings.Disc.Enabled = !settings.Disc.Enabled;
          Apply(p);
          ShowDiscMenu(p);
        }
      },
      new()
      {
        Text = Localizer["marker.glow", StateLabel(settings.Disc.Glow)],
        OnSelect = p =>
        {
          settings.Disc.Glow = !settings.Disc.Glow;
          Apply(p);
          ShowDiscMenu(p);
        }
      },
      new()
      {
        Text = Localizer["marker.alpha", settings.Disc.Alpha],
        OnSelect = p =>
        {
          settings.Disc.Alpha = Next(Config.Disc.Alphas, settings.Disc.Alpha);
          Apply(p);
          ShowDiscMenu(p);
        }
      }
    };

    _menus.Open(player, Localizer["marker.disc_title"], items);
  }

  private void Apply(CCSPlayerController player)
  {
    Remember(player);
    Refresh(player);
  }

  private void Refresh(CCSPlayerController player)
  {
    int slot = player.Slot;
    if (slot < 0 || slot >= MaxSlots || _markers[slot] is not { } marker)
      return;

    var center = marker.Center;
    MarkerRing.Destroy(marker);
    _markers[slot] = MarkerRing.Create(slot, center, Get(player), Config);
  }

  private static T Next<T>(List<T> values, T current) where T : notnull
  {
    int index = values.FindIndex(value => value.Equals(current));
    return values[(index + 1) % values.Count];
  }

  private static string Hex(string color)
  {
    var resolved = MarkerRing.Resolve(color);
    return $"#{resolved.R:X2}{resolved.G:X2}{resolved.B:X2}";
  }

  private static string Number(float value) =>
    value % 1f == 0f ? ((int)value).ToString() : value.ToString("0.##", CultureInfo.InvariantCulture);

  private string StateLabel(bool value) =>
    value ? Localizer["marker.on"].ToString() : Localizer["marker.off"].ToString();

  private string KeyLabel(string key) => key switch
  {
    "inspect" => Localizer["marker.key_inspect"].ToString(),
    "ping" => Localizer["marker.key_ping"].ToString(),
    "off" => Localizer["marker.key_off"].ToString(),
    _ => Localizer["marker.key_use"].ToString()
  };

}
