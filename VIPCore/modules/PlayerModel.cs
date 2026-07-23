using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class PlayerModel : VipModule
{
    private class ModelDef
    {
        public string Name { get; set; } = "";
        public string Model { get; set; } = "";
        public string Arm { get; set; } = "";
        public bool Leg { get; set; } = true;
    }

    private class Cfg
    {
        public List<ModelDef> Ct { get; set; } = new();
        public List<ModelDef> T { get; set; } = new();
    }

    private static PlayerModel? _instance;
    private readonly bool[] _legHidden = new bool[64];

    public static bool LegsHidden(int slot) =>
        slot >= 0 && slot < 64 && _instance?._legHidden[slot] == true;

    public override string Name => "PlayerModel";
    public override string DisplayName => Core.Localizer["vip.module.playermodel"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectCategories(CCSPlayerController player)
    {
        var cfg = GroupValue<Cfg>(player) ?? new Cfg();
        var cats = new List<VipFeatureOption>();
        if (cfg.Ct.Any(m => m.Name.Length > 0 && m.Model.Length > 0))
            cats.Add(new VipFeatureOption("CT", "ct"));
        if (cfg.T.Any(m => m.Name.Length > 0 && m.Model.Length > 0))
            cats.Add(new VipFeatureOption("T", "t"));
        return cats;
    }

    public override List<VipFeatureOption> CategoryOptions(CCSPlayerController player, string category)
    {
        var cfg = GroupValue<Cfg>(player) ?? new Cfg();
        var list = category == "ct" ? cfg.Ct : cfg.T;
        return list.Where(m => m.Name.Length > 0 && m.Model.Length > 0)
            .Select(m => new VipFeatureOption(m.Name, m.Name)).ToList();
    }

    public override void OnLoad()
    {
        _instance = this;
        Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
        Core.RegisterListener<OnServerPrecacheResources>(manifest =>
        {
            foreach (var cfg in Core.GetAllGroupValues<Cfg>(Name))
            {
                foreach (var def in cfg.Ct.Concat(cfg.T))
                {
                    if (def.Model.Length > 0)
                        manifest.AddResource(def.Model);
                    if (def.Arm.Length > 0)
                        manifest.AddResource(def.Arm);
                }
            }
        });
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (player != null && player.IsValid && player.Slot < 64)
            _legHidden[player.Slot] = false;

        if (!Active(player))
            return HookResult.Continue;

        string category = player!.Team switch
        {
            CsTeam.CounterTerrorist => "ct",
            CsTeam.Terrorist => "t",
            _ => ""
        };
        if (category.Length == 0)
            return HookResult.Continue;

        string selection = CategorySetting(player, category);
        if (selection == "off")
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(player) ?? new Cfg();
        var list = category == "ct" ? cfg.Ct : cfg.T;
        var def = list.FirstOrDefault(m => m.Name == selection);
        if (def == null || def.Model.Length == 0)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            if (!IsAlive(player))
                return;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                return;

            pawn.SetModel(def.Model);

            if (!def.Leg)
            {
                _legHidden[player.Slot] = true;
                var render = pawn.Render;
                pawn.Render = Color.FromArgb(254, render.R, render.G, render.B);
                Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            }
        });

        return HookResult.Continue;
    }
}
