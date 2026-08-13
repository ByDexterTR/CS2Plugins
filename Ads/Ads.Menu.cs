using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace Ads;

public partial class Ads
{
  private enum PendingInput
  {
    None,
    Flag,
    IgnoreFlag
  }

  private readonly PropAd?[] _selected = new PropAd?[MaxSlots];
  private readonly PendingInput[] _awaiting = new PendingInput[MaxSlots];
  private readonly int[] _axis = new int[MaxSlots];

  private const string BackColor = "#FF8077";

  private void ShowMainMenu(CCSPlayerController player)
  {
    var items = new List<WasdItem>
    {
      new() { Text = Localizer["ads.menu_place"], OnSelect = ShowPropMenu },
      new() { Text = Localizer["ads.menu_edit"], OnSelect = ShowEditMenu },
      new() { Text = Localizer["ads.menu_sql"], OnSelect = ShowSqlMenu },
      new() { Text = Localizer["ads.menu_manage"], OnSelect = ShowManageMenu }
    };

    _menus.Open(player, Localizer["ads.menu_main"], items);
  }

  private void ShowPropMenu(CCSPlayerController player)
  {
    if (_propsData.Models.Count == 0)
    {
      Reply(player, Localizer["ads.no_models", _json.PropsFilePath, "models"]);
      return;
    }

    var items = new List<WasdItem> { BackItem(ShowMainMenu) };

    foreach (var model in _propsData.Models)
    {
      if (string.IsNullOrWhiteSpace(model.Path))
        continue;

      var selected = model;
      items.Add(new WasdItem
      {
        Text = Label(selected.Name, selected.Path),
        OnSelect = p =>
        {
          PlaceProp(p, selected);
          ShowPropMenu(p);
        }
      });
    }

    _menus.Open(player, Localizer["ads.menu_place"], items);
  }

  private void ShowEditMenu(CCSPlayerController player)
  {
    var target = _selected[player.Slot];
    string pickLabel = target == null
      ? Localizer["ads.menu_pick"]
      : Localizer["ads.menu_pick_current", ShortName(target.Path)];

    var items = new List<WasdItem>
    {
      BackItem(ShowMainMenu),
      Action(pickLabel, PickTarget, ShowEditMenu),
      new() { Text = Localizer["ads.menu_transform"], OnSelect = ShowTransformMenu },
      new() { Text = Localizer["ads.menu_properties"], OnSelect = ShowPropertiesMenu },
      Action("ads.menu_delete", DeleteSelected, ShowEditMenu)
    };

    _menus.Open(player, Localizer["ads.menu_edit"], items);
  }

  private void ShowTransformMenu(CCSPlayerController player)
  {
    float rotate = Config.RotateStep;
    float move = Config.MoveStep;
    string axis = AxisName(_axis[player.Slot]);

    var items = new List<WasdItem>
    {
      BackItem(ShowEditMenu),
      Action("ads.menu_reposition", RepositionSelected, ShowTransformMenu),
      Action(Localizer["ads.menu_axis", axis], CycleAxis, ShowTransformMenu),
      Action(Localizer["ads.menu_rotate_plus", axis, rotate], p => RotateSelected(p, rotate), ShowTransformMenu),
      Action(Localizer["ads.menu_rotate_minus", axis, rotate], p => RotateSelected(p, -rotate), ShowTransformMenu),
      Action(Localizer["ads.menu_move_plus", axis, move], p => MoveSelected(p, move), ShowTransformMenu),
      Action(Localizer["ads.menu_move_minus", axis, move], p => MoveSelected(p, -move), ShowTransformMenu)
    };

    _menus.Open(player, Localizer["ads.menu_transform"], items);
  }

  private void ShowPropertiesMenu(CCSPlayerController player)
  {
    var target = _selected[player.Slot];
    float step = Config.ScaleStep;

    string collision = target?.Solid == true
      ? Localizer["ads.menu_collision_on"]
      : Localizer["ads.menu_collision_off"];

    var items = new List<WasdItem>
    {
      BackItem(ShowEditMenu),
      Action(Localizer["ads.menu_scale_up", step], p => ScaleSelected(p, step), ShowPropertiesMenu),
      Action(Localizer["ads.menu_scale_down", step], p => ScaleSelected(p, -step), ShowPropertiesMenu),
      Action(collision, ToggleCollision, ShowPropertiesMenu),
      Action(Localizer["ads.menu_skin", target?.Skin ?? 0], CycleSkin, ShowPropertiesMenu),
      Action(Localizer["ads.menu_flag", FlagLabel(target?.Flag)], p => AskInput(p, PendingInput.Flag), null),
      Action(Localizer["ads.menu_ignoreflag", FlagLabel(target?.IgnoreFlag)], p => AskInput(p, PendingInput.IgnoreFlag), null)
    };

    _menus.Open(player, Localizer["ads.menu_properties"], items);
  }

  private void ShowSqlMenu(CCSPlayerController player)
  {
    var items = new List<WasdItem>
    {
      BackItem(ShowMainMenu),
      Action("ads.menu_sql_import", ImportSql, ShowSqlMenu),
      Action("ads.menu_sql_export", ExportSql, ShowSqlMenu)
    };

    _menus.Open(player, Localizer["ads.menu_sql"], items);
  }

  private void ShowManageMenu(CCSPlayerController player)
  {
    var items = new List<WasdItem>
    {
      BackItem(ShowMainMenu),
      Action("ads.menu_reload_props", p =>
      {
        ReloadProps();
        Reply(p, Localizer["ads.reloaded_props", _data.Props.Count]);
      }, ShowManageMenu),
      Action("ads.menu_reload_ads", p =>
      {
        ReloadAds();
        Reply(p, Localizer["ads.reloaded_ads",
          _data.ScreenTexts.Count, _data.HudSays.Count, _data.ChatSays.Count, _data.Events.Count]);
      }, ShowManageMenu),
      Action("ads.menu_reload_settings", ReloadSettings, ShowManageMenu)
    };

    _menus.Open(player, Localizer["ads.menu_manage"], items);
  }

  private WasdItem BackItem(Action<CCSPlayerController> back) => new()
  {
    Text = Localizer["ads.menu_back"],
    Color = BackColor,
    OnSelect = p => back(p)
  };

  private WasdItem Action(string text, Action<CCSPlayerController> run, Action<CCSPlayerController>? reopen) => new()
  {
    Text = text.StartsWith("ads.") ? Localizer[text] : text,
    OnSelect = p =>
    {
      run(p);
      reopen?.Invoke(p);
    }
  };

  private void CycleAxis(CCSPlayerController player) =>
    _axis[player.Slot] = (_axis[player.Slot] + 1) % 3;

  private static string AxisName(int axis) => axis switch
  {
    0 => "X",
    1 => "Y",
    _ => "Z"
  };

  private string FlagLabel(string? flag) =>
    string.IsNullOrWhiteSpace(flag) ? Localizer["ads.flag_none"] : flag;

  private static string Label(string name, string path) =>
    string.IsNullOrWhiteSpace(name) ? ShortName(path) : name;

  private void PickTarget(CCSPlayerController player)
  {
    var aimed = FindAimedAd(player);
    if (aimed == null || aimed.Index >= _data.Props.Count)
      return;

    var target = _data.Props[aimed.Index];
    _selected[player.Slot] = target;
    Reply(player, Localizer["ads.prop_selected", ShortName(target.Path), target.Pos, target.Angle]);
  }

  private PropAd? GetSelected(CCSPlayerController player)
  {
    var target = _selected[player.Slot];
    if (target != null && _data.Props.Contains(target))
      return target;

    _selected[player.Slot] = null;
    Reply(player, Localizer["ads.prop_none"]);
    return null;
  }

  private void RotateSelected(CCSPlayerController player, float delta)
  {
    var target = GetSelected(player);
    if (target == null)
      return;

    target.Angle = RotateAxis(target.Angle, _axis[player.Slot], delta);
    ApplyTransform(target);
    SaveMaps(player);
    Reply(player, Localizer["ads.prop_updated", target.Pos, target.Angle]);
  }

  private void MoveSelected(CCSPlayerController player, float delta)
  {
    var target = GetSelected(player);
    if (target == null)
      return;

    target.Pos = MoveAxis(target.Pos, _axis[player.Slot], delta);
    ApplyTransform(target);
    SaveMaps(player);
    Reply(player, Localizer["ads.prop_updated", target.Pos, target.Angle]);
  }

  private void RepositionSelected(CCSPlayerController player)
  {
    var target = GetSelected(player);
    if (target == null)
      return;

    if (!TryGetAimPoint(player, out var hit, out _))
      return;

    target.Pos = FormatVector(hit.X, hit.Y, hit.Z);
    ApplyTransform(target);
    SaveMaps(player);
    Reply(player, Localizer["ads.prop_updated", target.Pos, target.Angle]);
  }

  private void ScaleSelected(CCSPlayerController player, float delta)
  {
    var target = GetSelected(player);
    if (target == null)
      return;

    float scale = target.Scale + delta;
    target.Scale = scale < 0.05f ? 0.05f : scale;

    SaveMaps(player);
    SpawnWorldAds();
    Reply(player, Localizer["ads.scaled", target.Scale.ToString("0.##")]);
  }

  private void ToggleCollision(CCSPlayerController player)
  {
    var target = GetSelected(player);
    if (target == null)
      return;

    target.Solid = !target.Solid;
    SaveMaps(player);
    SpawnWorldAds();
    Reply(player, Localizer[target.Solid ? "ads.collision_on" : "ads.collision_off"]);
  }

  private void CycleSkin(CCSPlayerController player)
  {
    var target = GetSelected(player);
    if (target == null)
      return;

    var skins = SkinsFor(target.Path);
    if (skins.Count == 0)
    {
      Reply(player, Localizer["ads.no_skins", _json.PropsFilePath]);
      return;
    }

    int index = skins.IndexOf(target.Skin);
    target.Skin = skins[(index + 1) % skins.Count];

    SaveMaps(player);
    SpawnWorldAds();
    Reply(player, Localizer["ads.skin_changed", target.Skin]);
  }

  private List<int> SkinsFor(string path)
  {
    foreach (var model in _propsData.Models)
    {
      if (string.Equals(model.Path, path, StringComparison.OrdinalIgnoreCase) && model.Skins is { Count: > 0 })
        return model.Skins;
    }

    return new List<int>();
  }

  private void AskInput(CCSPlayerController player, PendingInput kind)
  {
    if (GetSelected(player) == null)
    {
      ShowPropertiesMenu(player);
      return;
    }

    _awaiting[player.Slot] = kind;
    _menus.Close(player);
    Reply(player, Localizer[kind == PendingInput.Flag ? "ads.ask_flag" : "ads.ask_ignoreflag"]);
  }

  private HookResult OnSay(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return HookResult.Continue;

    var kind = _awaiting[player.Slot];
    if (kind == PendingInput.None)
      return HookResult.Continue;

    _awaiting[player.Slot] = PendingInput.None;

    if (!Util.HasAccess(player, Config.Flag))
      return HookResult.Continue;

    string value = info.GetArg(1).Trim();
    if (value == "-" || value.Equals("iptal", StringComparison.OrdinalIgnoreCase) || value.Equals("cancel", StringComparison.OrdinalIgnoreCase))
      value = "";

    var target = _selected[player.Slot];
    if (target != null && _data.Props.Contains(target))
    {
      if (kind == PendingInput.Flag)
        target.Flag = value;
      else
        target.IgnoreFlag = value;

      SaveMaps(player);
      SpawnWorldAds();
      Reply(player, Localizer["ads.flag_set", FlagLabel(value)]);
    }

    Server.NextFrame(() =>
    {
      if (player.IsValid)
        ShowPropertiesMenu(player);
    });

    return HookResult.Handled;
  }

  private void DeleteSelected(CCSPlayerController player)
  {
    var target = GetSelected(player);
    if (target == null)
      return;

    _data.Props.Remove(target);
    _selected[player.Slot] = null;

    SaveMaps(player);
    SpawnWorldAds();
    Reply(player, Localizer["ads.removed"]);
  }

  private void ApplyTransform(PropAd target)
  {
    int index = _data.Props.IndexOf(target);
    if (index < 0)
      return;

    foreach (var placed in _entities)
    {
      if (placed.Index != index)
        continue;

      if (placed.Entity != null && placed.Entity.IsValid)
        placed.Entity.Teleport(ParseVector(target.Pos), ParseAngle(target.Angle), Vector.Zero);

      return;
    }
  }

  private static string ShortName(string path)
  {
    int slash = path.LastIndexOf('/');
    return slash >= 0 && slash < path.Length - 1 ? path[(slash + 1)..] : path;
  }
}
