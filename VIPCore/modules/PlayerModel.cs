using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class PlayerModel : VipModule
{
    public class ModelDef
    {
        public string Name { get; set; } = "";
        public string Model { get; set; } = "";
        public string Arm { get; set; } = "";
        public bool Leg { get; set; } = true;
    }

    public class Cfg
    {
        public List<ModelDef> Ct { get; set; } = new();
        public List<ModelDef> T { get; set; } = new();
        public float DeathDelay { get; set; } = DeathClock.SafeDelay;
    }

    private const string CtDefault = "agents/models/ctm_sas/ctm_sas.vmdl";
    private const string TDefault = "agents/models/tm_phoenix/tm_phoenix.vmdl";

    private static PlayerModel? _instance;
    private readonly bool[] _legHidden = new bool[64];
    private readonly bool[] _changed = new bool[64];
    private readonly string?[] _original = new string?[64];
    private readonly int[] _owner = new int[64];

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
        Core.HookMapStart(_ => Array.Clear(_changed));
        Core.HookMapChange(RevertAll);
        DeathClock.Ensure(Core);
        Core.HookPrecache(manifest =>
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

    public override void OnSelect(CCSPlayerController player, string value) => Server.NextFrame(() => Apply(player));

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (player != null && player.IsValid && player.Slot < 64)
            _legHidden[player.Slot] = false;

        Server.NextFrame(() => Apply(player));
        return HookResult.Continue;
    }

    private void RevertAll()
    {
        for (int slot = 0; slot < 64; slot++)
        {
            if (!_changed[slot])
                continue;

            _changed[slot] = false;
            var player = Utilities.GetPlayerFromSlot(slot);
            if (player == null || !player.IsValid || !IsAlive(player))
                continue;

            if (player.UserId != _owner[slot])
                continue;

            var pawn = PlayerCache.Pawn(player);
            if (pawn == null || !pawn.IsValid)
                continue;

            string model = _original[slot] ?? (player.Team == CsTeam.CounterTerrorist ? CtDefault : TDefault);
            pawn.SetModel(model);
        }
    }

    private bool IsCustom(string model)
    {
        foreach (var cfg in Core.GetAllGroupValues<Cfg>(Name))
            foreach (var def in cfg.Ct.Concat(cfg.T))
                if (def.Model == model)
                    return true;
        return false;
    }

    private void Apply(CCSPlayerController? player)
    {
        if (!Active(player) || !IsAlive(player))
            return;

        string category = player!.Team switch
        {
            CsTeam.CounterTerrorist => "ct",
            CsTeam.Terrorist => "t",
            _ => ""
        };
        if (category.Length == 0)
            return;

        string selection = CategorySetting(player, category);
        if (selection == "off")
            return;

        var cfg = GroupValue<Cfg>(player) ?? new Cfg();
        var list = category == "ct" ? cfg.Ct : cfg.T;
        var def = list.FirstOrDefault(m => m.Name == selection);
        if (def == null || def.Model.Length == 0)
            return;

        var pawn = PlayerCache.Pawn(player);
        if (pawn == null || !pawn.IsValid)
            return;

        float wait = DeathClock.Wait(player, cfg.DeathDelay);
        if (wait > 0f)
        {
            Core.AddTimer(wait, () => Apply(player), TimerFlags.STOP_ON_MAPCHANGE);
            return;
        }

        if (Spy.Disguised(player.Slot))
            return;

        string current = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState.ModelName ?? "";
        if (current != def.Model)
        {
            if (current.Length > 0 && !IsCustom(current))
                _original[player.Slot] = current;
            else if (_owner[player.Slot] != player.UserId)
                _original[player.Slot] = null;
            _owner[player.Slot] = player.UserId ?? -1;
            pawn.SetModel(def.Model);
            _changed[player.Slot] = true;
        }

        if (_legHidden[player.Slot] == def.Leg)
        {
            _legHidden[player.Slot] = !def.Leg;
            var render = pawn.Render;
            pawn.Render = Color.FromArgb(InvisPool.Alpha(player.Slot), render.R, render.G, render.B);
            NetState.Changed(pawn, "CBaseModelEntity", "m_clrRender");
        }
    }
}
