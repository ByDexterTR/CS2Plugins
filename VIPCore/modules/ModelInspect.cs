using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class ModelInspect : VipModule
{
    private class Session
    {
        public required CDynamicProp Prop;
        public required float StartAngle;
        public required float StartTime;
        public required float EndTime;
        public required float Spin;
    }

    private readonly Session?[] _previews = new Session?[64];
    private readonly float[] _nextUse = new float[64];

    public override string Name => "ModelInspect";
    public override string DisplayName => Core.Localizer["vip.module.modelinspect"];
    public override bool ShowInMenu => false;
    public override bool AlwaysLoad => true;

    private ModelInspectSettings Settings => Core.InspectSettings;

    public override void OnLoad()
    {
        Core.RegisterAliasedCommand(Core.InspectCommands, OnInspect);
        Core.RegisterListener<OnServerPrecacheResources>(manifest =>
        {
            foreach (var cfg in Core.GetAllGroupValues<PlayerModel.Cfg>("PlayerModel"))
                foreach (var def in cfg.Ct.Concat(cfg.T))
                    if (def.Model.Length > 0)
                        manifest.AddResource(def.Model);
        });
        Core.RegisterListener<OnTick>(OnTick);
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) => { Remove(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) => { Remove(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        Core.RegisterEventHandler<EventRoundStart>((_, _) => { RemoveAll(); return HookResult.Continue; });
        Core.RegisterListener<OnMapStart>(_ => RemoveAll());
    }

    public override void OnUnload() => RemoveAll();

    private void OnInspect(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || !Settings.Enabled)
            return;

        if (!Core.IsClientVip(player))
        {
            player.PrintToChat($" {CC.Orchid}{Core.Localizer["chat_prefix"]}{CC.Default} {Core.Localizer["vip.no_access"]}");
            return;
        }

        OpenTeamMenu(player);
    }

    private void OpenTeamMenu(CCSPlayerController player)
    {
        var cfg = Core.GetGroupValue<PlayerModel.Cfg>(player, "PlayerModel");

        var items = new List<(string display, Action<CCSPlayerController> onSelect)>
        {
            (Core.Localizer["vip.inspect.current"], p => Preview(p, null))
        };

        if (cfg != null)
        {
            if (Models(cfg.Ct).Count > 0)
                items.Add(("CT", p => OpenModelMenu(p, "ct")));
            if (Models(cfg.T).Count > 0)
                items.Add(("T", p => OpenModelMenu(p, "t")));
        }

        Core.OpenCustomMenu(player, DisplayName, items);
    }

    private void OpenModelMenu(CCSPlayerController player, string team)
    {
        var cfg = Core.GetGroupValue<PlayerModel.Cfg>(player, "PlayerModel");
        if (cfg == null)
            return;

        var models = Models(team == "ct" ? cfg.Ct : cfg.T);

        var items = new List<(string display, Action<CCSPlayerController> onSelect)>
        {
            ($"{CC.LightRed}{Core.Localizer["vip.menu_back"]}{CC.Default}", OpenTeamMenu)
        };

        foreach (var model in models)
        {
            var entry = model;
            items.Add((entry.Name, p => Preview(p, entry.Model)));
        }

        Core.OpenCustomMenu(player, team == "ct" ? "CT" : "T", items);
    }

    private static List<PlayerModel.ModelDef> Models(List<PlayerModel.ModelDef> list) =>
        list.Where(def => def.Name.Length > 0 && def.Model.Length > 0).ToList();

    private void Preview(CCSPlayerController player, string? model)
    {
        if (!player.IsValid || !Settings.Enabled || !Core.IsClientVip(player) || !IsAlive(player))
            return;

        int slot = player.Slot;
        float now = Server.CurrentTime;
        float cooldown = Settings.Cooldown;

        if (cooldown > 0f && now < _nextUse[slot])
        {
            int left = (int)MathF.Ceiling(_nextUse[slot] - now);
            player.PrintToChat($" {CC.Orchid}{Core.Localizer["chat_prefix"]}{CC.Default} {Core.Localizer["vip.inspect.cooldown", left]}");
            return;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
            return;

        model ??= pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState.ModelName;
        if (string.IsNullOrEmpty(model))
            return;

        Remove(slot);

        var prop = Spawn(pawn, model, out float angle);
        if (prop == null)
            return;

        float duration = Math.Max(Settings.Duration, 0.5f);
        _previews[slot] = new Session
        {
            Prop = prop,
            StartAngle = angle,
            StartTime = now,
            EndTime = now + duration,
            Spin = Settings.Spin
        };

        if (cooldown > 0f)
            _nextUse[slot] = now + cooldown;
    }

    private CDynamicProp? Spawn(CCSPlayerPawn pawn, string model, out float angle)
    {
        angle = pawn.EyeAngles.Y + 180f;

        var prop = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (prop == null || !prop.IsValid)
            return null;

        var node = prop.CBodyComponent?.SceneNode?.Owner?.Entity;
        if (node != null)
            node.Flags = (uint)(node.Flags & ~(1 << 2));

        var collision = prop.Collision;
        if (collision != null)
        {
            collision.SolidType = SolidType_t.SOLID_NONE;
            collision.SolidFlags = 12;
        }

        prop.SetModel(model);
        prop.Spawnflags = 256u;
        prop.Teleport(Position(pawn), new QAngle(0, angle, 0), new Vector());
        prop.DispatchSpawn();

        return prop;
    }

    private Vector Position(CCSPlayerPawn pawn)
    {
        var origin = pawn.AbsOrigin!;
        float yaw = pawn.EyeAngles.Y * MathF.PI / 180f;

        return new Vector(
            origin.X + MathF.Cos(yaw) * Settings.Distance,
            origin.Y + MathF.Sin(yaw) * Settings.Distance,
            origin.Z + pawn.ViewOffset.Z + Settings.Height);
    }

    private void OnTick()
    {
        float now = Server.CurrentTime;

        for (int slot = 0; slot < 64; slot++)
        {
            var preview = _previews[slot];
            if (preview == null)
                continue;

            if (now >= preview.EndTime || !preview.Prop.IsValid)
            {
                Remove(slot);
                continue;
            }

            var player = Utilities.GetPlayerFromSlot(slot);
            if (!IsAlive(player) || player == null || !Core.IsClientVip(player))
            {
                Remove(slot);
                continue;
            }

            float progress = (now - preview.StartTime) / (preview.EndTime - preview.StartTime);
            preview.Prop.Teleport(null, new QAngle(0, preview.StartAngle + preview.Spin * progress, 0), null);
        }
    }

    private void Remove(int slot)
    {
        if (slot < 0 || slot >= 64)
            return;

        var preview = _previews[slot];
        _previews[slot] = null;

        if (preview != null && preview.Prop.IsValid && preview.Prop.DesignerName == "prop_dynamic")
            preview.Prop.Remove();
    }

    private void RemoveAll()
    {
        for (int slot = 0; slot < 64; slot++)
            Remove(slot);
    }
}
