using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;

namespace VIPCore;

public partial class VIPCore
{
    private string BackLabel => $"{CC.Yellow}{Localizer["vip.menu_back"]}{CC.Default}";

    private const string VipIcon = "https://raw.githubusercontent.com/ByDexterTR/CS2Plugins/refs/heads/main/img/vip.png";

    private void OpenMainMenu(CCSPlayerController player)
    {
        var items = new List<(string display, Action<CCSPlayerController> onSelect)>();

        foreach (var name in GetGroupFeatures(player))
        {
            var module = FindModule(name);
            if (module == null || !module.ShowInMenu)
                continue;

            var m = module;
            Action<CCSPlayerController> action = m.MenuType == VipFeatureType.Toggle
                ? p => ToggleFeature(p, m)
                : p => OpenFeatureMenu(p, m);

            items.Add(($"{m.DisplayName}: {CurrentLabel(player, m)}", action));
        }

        if (items.Count == 0)
            items.Add((Localizer["vip.menu_empty"], _ => { }));

        string title = string.Equals(Config.MenuType, "chat", StringComparison.OrdinalIgnoreCase)
            ? Localizer["vip.menu_title"]
            : Localizer["vip.menu_features"];

        OpenMenu(player, title, items);
    }

    private void ToggleFeature(CCSPlayerController player, VipModule module)
    {
        string next = GetSetting(player.SteamID, module.Name) == "off" ? "on" : "off";
        SetSetting(player, module.Name, next);
        module.OnSelect(player, next);

        string label = next == "on" ? Localizer["vip.option_on"] : Localizer["vip.option_off"];
        player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.setting_changed", module.DisplayName, label]}");
        OpenMainMenu(player);
    }

    private void OpenFeatureMenu(CCSPlayerController player, VipModule module)
    {
        var cats = module.SelectCategories(player);
        if (cats.Count > 0)
        {
            OpenCategoryList(player, module, cats);
            return;
        }

        var options = new List<VipFeatureOption>
        {
            new(Localizer["vip.option_off"], "off")
        };

        if (module.MenuType == VipFeatureType.Toggle)
            options.Add(new VipFeatureOption(Localizer["vip.option_on"], "on"));
        else
            options.AddRange(module.SelectOptions(player));

        string current = GetSetting(player.SteamID, module.Name);
        var items = new List<(string display, Action<CCSPlayerController> onSelect)>
        {
            (BackLabel, OpenMainMenu)
        };

        foreach (var option in options)
        {
            var opt = option;
            string mark = opt.Value == current ? $"{CC.Green}{opt.Display} *{CC.Default}" : opt.Display;
            items.Add((mark, p =>
            {
                SetSetting(p, module.Name, opt.Value);
                module.OnSelect(p, opt.Value);
                p.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.setting_changed", module.DisplayName, opt.Display]}");
                OpenMainMenu(p);
            }));
        }

        OpenMenu(player, module.DisplayName, items);
    }

    private void OpenCategoryList(CCSPlayerController player, VipModule module, List<VipFeatureOption> cats)
    {
        string state = GetSetting(player.SteamID, module.Name);
        var items = new List<(string display, Action<CCSPlayerController> onSelect)>
        {
            (BackLabel, OpenMainMenu)
        };

        string offLabel = Localizer["vip.option_off"];
        items.Add((state == "off" ? $"{CC.Green}{offLabel} *{CC.Default}" : offLabel, p =>
        {
            SetSetting(p, module.Name, "off");
            module.OnSelect(p, "off");
            p.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.setting_changed", module.DisplayName, offLabel]}");
            OpenMainMenu(p);
        }));

        string onLabel = Localizer["vip.option_on"];
        items.Add((state != "off" ? $"{CC.Green}{onLabel} *{CC.Default}" : onLabel, p =>
        {
            SetSetting(p, module.Name, "on");
            module.OnSelect(p, "on");
            p.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.setting_changed", module.DisplayName, onLabel]}");
            OpenMainMenu(p);
        }));

        foreach (var category in cats)
        {
            var cat = category;
            string current = GetSetting(player.SteamID, $"{module.Name}@{cat.Value}");
            var curOpt = module.CategoryOptions(player, cat.Value).FirstOrDefault(o => o.Value == current);
            string label = curOpt != null ? $"{CC.Green}{curOpt.Display}{CC.Default}" : Localizer["vip.option_off"];
            items.Add(($"{cat.Display}: {label}", p => OpenCategoryOptions(p, module, cat)));
        }

        OpenMenu(player, module.DisplayName, items);
    }

    private void OpenCategoryOptions(CCSPlayerController player, VipModule module, VipFeatureOption cat)
    {
        string key = $"{module.Name}@{cat.Value}";
        string current = GetSetting(player.SteamID, key);

        var options = new List<VipFeatureOption> { new(Localizer["vip.option_off"], "off") };
        options.AddRange(module.CategoryOptions(player, cat.Value));

        var items = new List<(string display, Action<CCSPlayerController> onSelect)>
        {
            (BackLabel, p => OpenFeatureMenu(p, module))
        };

        foreach (var option in options)
        {
            var opt = option;
            string mark = opt.Value == current ? $"{CC.Green}{opt.Display} *{CC.Default}" : opt.Display;
            items.Add((mark, p =>
            {
                SetSetting(p, key, opt.Value);
                if (opt.Value != "off")
                    SetSetting(p, module.Name, "on");
                module.OnSelect(p, opt.Value);
                p.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer["vip.setting_changed", $"{module.DisplayName} - {cat.Display}", opt.Display]}");
                OpenFeatureMenu(p, module);
            }));
        }

        OpenMenu(player, $"{module.DisplayName} - {cat.Display}", items);
    }

    private string CurrentLabel(CCSPlayerController player, VipModule module)
    {
        string value = GetSetting(player.SteamID, module.Name);

        if (value == "off")
            return Localizer["vip.option_off"];
        if (module.MenuType == VipFeatureType.Toggle)
            return Localizer["vip.option_on"];
        if (module.SelectCategories(player).Count > 0)
            return $"{CC.Green}{Localizer["vip.option_on"]}{CC.Default}";

        var match = module.SelectOptions(player).FirstOrDefault(o => o.Value == value);
        return $"{CC.Green}{match?.Display ?? value}{CC.Default}";
    }

    private void OpenMenu(CCSPlayerController player, string title,
        List<(string display, Action<CCSPlayerController> onSelect)> items)
    {
        switch (Config.MenuType?.ToLower())
        {
            case "chat":
                var chat = new ChatMenu(title);
                foreach (var item in items)
                {
                    var it = item;
                    chat.AddMenuOption(it.display, (p, _) => it.onSelect(p));
                }
                MenuManager.OpenChatMenu(player, chat);
                break;

            case "wasd":
                OpenWasdMenu(player, title, items);
                break;

            default:
                var menu = new CenterHtmlMenu($"<font color='#ffb300' class='fontSize-l'><img src='{VipIcon}'> {title}</font>", this);
                foreach (var item in items)
                {
                    var it = item;
                    menu.AddMenuOption(ChatColorUtil.ToHtml(it.display), (p, _) => it.onSelect(p));
                }
                MenuManager.OpenCenterHtmlMenu(this, player, menu);
                break;
        }
    }
}
