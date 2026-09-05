using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class ModelInspect : VipModule
{
    private class Session
    {
        public required CBaseModelEntity Prop;
        public required float StartAngle;
        public required float StartTime;
        public required float EndTime;
        public required float Spin;
    }

    private static readonly HashSet<string> PhysicsModels = new(StringComparer.OrdinalIgnoreCase);

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
        Core.HookPrecache(manifest =>
        {
            foreach (var cfg in Core.GetAllGroupValues<PlayerModel.Cfg>("PlayerModel"))
                foreach (var def in cfg.Ct.Concat(cfg.T))
                    if (def.Model.Length > 0)
                        manifest.AddResource(def.Model);

            foreach (var entries in Core.GetAllGroupValues<List<Pet.Entry>>("Pet"))
                foreach (var entry in entries)
                    if (entry.Model.Length > 0)
                        manifest.AddResource(entry.Model);

            foreach (var cfg in Core.GetAllGroupValues<Outfit.Cfg>("Outfit"))
                foreach (var entries in cfg.Values)
                    foreach (var entry in entries)
                        if (entry.Model.Length > 0)
                            manifest.AddResource(entry.Model);

            foreach (var entries in Core.GetAllGroupValues<List<CustomWeaponModel.Entry>>("CustomWeaponModel"))
                foreach (var entry in entries)
                    if (entry.Model.Length > 0 && !int.TryParse(entry.Model, out _))
                        manifest.AddResource(entry.Model);
        });
        Core.HookTick(OnTick);
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) => { Remove(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) => { Remove(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        Core.RegisterEventHandler<EventRoundStart>((_, _) => { RemoveAll(); return HookResult.Continue; });
        Core.HookMapStart(_ => RemoveAll());
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

        if (Pets(player).Count > 0)
            items.Add((Core.Localizer["vip.module.pet"], OpenPetMenu));

        foreach (var (category, entries) in Wearables(player))
        {
            string name = category;
            if (entries.Count > 0)
                items.Add((Label(name), p => OpenWearMenu(p, name)));
        }

        if (Weapons(player).Count > 0)
            items.Add((Core.Localizer["vip.module.customweaponmodel"], OpenWeaponMenu));

        Core.OpenCustomMenu(player, DisplayName, items);
    }

    private List<Pet.Entry> Pets(CCSPlayerController player) =>
        (Core.GetGroupValue<List<Pet.Entry>>(player, "Pet") ?? new())
        .Where(entry => entry.Name.Length > 0 && entry.Model.Length > 0).ToList();

    private Outfit.Cfg Wearables(CCSPlayerController player) =>
        Core.GetGroupValue<Outfit.Cfg>(player, "Outfit") ?? new();

    private List<CustomWeaponModel.Entry> Weapons(CCSPlayerController player) =>
        CustomWeaponModel.Usable(Core.GetGroupValue<List<CustomWeaponModel.Entry>>(player, "CustomWeaponModel"))
            .Where(entry => !int.TryParse(entry.Model, out _)).ToList();

    private static string Label(string name) =>
        name.Length > 0 ? char.ToUpperInvariant(name[0]) + name[1..] : name;

    private List<(string display, Action<CCSPlayerController> onSelect)> BackTo(Action<CCSPlayerController> parent) =>
        new() { ($"{CC.LightRed}{Core.Localizer["vip.menu_back"]}{CC.Default}", parent) };

    private void OpenPetMenu(CCSPlayerController player)
    {
        var items = BackTo(OpenTeamMenu);

        foreach (var entry in Pets(player))
        {
            var pet = entry;
            items.Add((pet.Name, p => Preview(p, pet.Model)));
        }

        Core.OpenCustomMenu(player, Core.Localizer["vip.module.pet"], items);
    }

    private void OpenWearMenu(CCSPlayerController player, string category)
    {
        if (!Wearables(player).TryGetValue(category, out var entries))
            return;

        var items = BackTo(OpenTeamMenu);

        foreach (var entry in entries)
        {
            if (entry.Name.Length == 0 || entry.Model.Length == 0)
                continue;

            var wear = entry;
            items.Add((wear.Name, p => Preview(p, wear.Model)));
        }

        Core.OpenCustomMenu(player, Label(category), items);
    }

    private void OpenWeaponMenu(CCSPlayerController player)
    {
        var items = BackTo(OpenTeamMenu);
        var seen = new HashSet<string>();

        foreach (var entry in Weapons(player))
        {
            string category = CustomWeaponModel.Category(entry.Weapon);
            if (!seen.Add(category))
                continue;

            items.Add((WeaponUtil.Label(entry.Weapon), p => OpenWeaponModelMenu(p, category)));
        }

        Core.OpenCustomMenu(player, Core.Localizer["vip.module.customweaponmodel"], items);
    }

    private void OpenWeaponModelMenu(CCSPlayerController player, string category)
    {
        var items = BackTo(OpenWeaponMenu);

        foreach (var entry in Weapons(player))
        {
            if (CustomWeaponModel.Category(entry.Weapon) != category)
                continue;

            var weapon = entry;
            items.Add((weapon.Name, p => Preview(p, weapon.Model)));
        }

        Core.OpenCustomMenu(player, WeaponUtil.Label(category), items);
    }

    private void OpenModelMenu(CCSPlayerController player, string team)
    {
        var cfg = Core.GetGroupValue<PlayerModel.Cfg>(player, "PlayerModel");
        if (cfg == null)
            return;

        var models = Models(team == "ct" ? cfg.Ct : cfg.T);
        var items = BackTo(OpenTeamMenu);

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

        if (!Show(player, pawn, model, now, PhysicsModels.Contains(model)))
            return;

        if (cooldown > 0f)
            _nextUse[slot] = now + cooldown;
    }

    private bool Show(CCSPlayerController player, CCSPlayerPawn pawn, string model, float now, bool physics)
    {
        var prop = Spawn(pawn, model, physics, out float angle);
        if (prop == null)
            return false;

        int slot = player.Slot;
        float duration = Math.Max(Settings.Duration, 0.5f);

        var session = new Session
        {
            Prop = prop,
            StartAngle = angle,
            StartTime = now,
            EndTime = now + duration,
            Spin = Settings.Spin
        };

        _previews[slot] = session;

        if (!physics)
            Server.NextFrame(() =>
            {
                if (prop.IsValid || !ReferenceEquals(_previews[slot], session))
                    return;

                PhysicsModels.Add(model);
                _previews[slot] = null;

                if (player.IsValid && pawn.IsValid && IsAlive(player))
                    Show(player, pawn, model, Server.CurrentTime, true);
            });

        return true;
    }

    private CBaseModelEntity? Spawn(CCSPlayerPawn pawn, string model, bool physics, out float angle)
    {
        angle = pawn.EyeAngles.Y + 180f;

        var prop = Utilities.CreateEntityByName<CBaseModelEntity>(physics ? "prop_physics_override" : "prop_dynamic");
        if (prop == null || !prop.IsValid)
            return null;

        var node = prop.CBodyComponent?.SceneNode?.Owner?.Entity;
        if (node != null)
            node.Flags = (uint)(node.Flags & ~(1 << 2));

        var position = Position(pawn);
        var angles = new QAngle(0, angle, 0);
        prop.Teleport(position, angles, new Vector());

        var keys = new CEntityKeyValues();
        keys.SetString("model", model);
        keys.SetInt("spawnflags", 256);
        keys.SetVector("origin", position);

        prop.DispatchSpawn(keys);
        keys.Dispose();

        if (!prop.IsValid)
            return null;

        var collision = prop.Collision;
        if (collision != null)
        {
            collision.SolidType = SolidType_t.SOLID_NONE;
            collision.SolidFlags = 12;
        }

        if (physics)
            prop.AcceptInput("DisableMotion");

        prop.Teleport(position, angles, new Vector());

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

        if (preview == null || !preview.Prop.IsValid)
            return;

        if (preview.Prop.DesignerName is "prop_dynamic" or "prop_physics_override")
            preview.Prop.Remove();
    }

    private void RemoveAll()
    {
        for (int slot = 0; slot < 64; slot++)
            Remove(slot);
    }
}
