using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class WeaponGlow : VipModule
{
    private class Cfg
    {
        public string Color { get; set; } = "#FFFFFF";
        public int Range { get; set; } = 5000;
        public List<string> Ignore { get; set; } = new() { "weapon_c4" };
    }

    private const int RescanTicks = 320;
    private const string DroppedAnim = "dropped";

    private static readonly Cfg DefaultCfg = new();

    private readonly HashSet<uint> _weapons = new();
    private readonly Dictionary<uint, uint> _glows = new();
    private readonly HashSet<uint> _carried = new();
    private readonly HashSet<uint> _failed = new();
    private readonly List<uint> _stale = new();
    private Cfg _cfg = DefaultCfg;
    private int _nextScan;

    public override string Name => "WeaponGlow";
    public override string DisplayName => Core.Localizer["vip.module.weaponglow"];

    private void RefreshCfg() => _cfg = Core.GetAllGroupValues<Cfg>(Name).FirstOrDefault() ?? DefaultCfg;

    public override void OnLoad()
    {
        Core.HookEntitySpawned(OnSpawned);
        Core.HookEntityDeleted(OnDeleted);
        Core.HookTransmit(OnCheckTransmit);
        Core.HookTick(OnTick, 16);
        Core.RegisterEventHandler<EventRoundStart>((_, _) =>
        {
            Clear();
            RefreshCfg();
            _nextScan = 0;
            return HookResult.Continue;
        });

        RefreshCfg();
    }

    public override void OnUnload() => Clear();

    private static bool IsWeapon(CEntityInstance entity) =>
        entity.IsValid && entity.DesignerName?.StartsWith("weapon_", StringComparison.Ordinal) == true;

    private void OnSpawned(CEntityInstance entity)
    {
        if (IsWeapon(entity))
            _weapons.Add(entity.Index);
    }

    private void OnDeleted(CEntityInstance entity)
    {
        if (_weapons.Count == 0)
            return;

        _weapons.Remove(entity.Index);
        Destroy(entity.Index);
    }

    private void Rescan()
    {
        foreach (var entity in Utilities.GetAllEntities())
            if (entity != null && IsWeapon(entity))
                _weapons.Add(entity.Index);
    }

    private void CollectCarried()
    {
        _carried.Clear();

        foreach (var player in Core.Players)
        {
            var weapons = player?.PlayerPawn.Value?.WeaponServices?.MyWeapons;
            if (weapons == null)
                continue;

            foreach (var handle in weapons)
                if (handle.Value is { IsValid: true } weapon)
                    _carried.Add(weapon.Index);
        }
    }

    private void OnTick()
    {
        if (ActivePlayers().Count == 0)
        {
            if (_glows.Count > 0)
                DestroyAll();
            return;
        }

        int tick = Server.TickCount;
        if (tick >= _nextScan)
        {
            _nextScan = tick + RescanTicks;
            RefreshCfg();
            Rescan();
        }

        if (_weapons.Count == 0)
            return;

        var cfg = _cfg;
        _stale.Clear();
        CollectCarried();

        foreach (uint index in _weapons)
        {
            var weapon = Utilities.GetEntityFromIndex<CCSWeaponBase>((int)index);
            if (weapon == null || !weapon.IsValid)
            {
                _stale.Add(index);
                _failed.Remove(index);
                Destroy(index);
                continue;
            }

            if (_carried.Contains(index) || cfg.Ignore.Contains(weapon.DesignerName))
            {
                _failed.Remove(index);
                Destroy(index);
                continue;
            }

            if (_glows.TryGetValue(index, out uint glowIndex))
                Verify(index, glowIndex);
            else if (!_failed.Contains(index))
                Create(index, weapon, cfg);
        }

        foreach (uint index in _stale)
            _weapons.Remove(index);
    }

    private void Create(uint index, CCSWeaponBase weapon, Cfg cfg)
    {
        var state = weapon.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState;
        string? modelName = state?.ModelName;
        if (string.IsNullOrEmpty(modelName) || weapon.AbsOrigin == null)
        {
            _failed.Add(index);
            return;
        }

        var glow = Utilities.CreateEntityByName<CPhysicsProp>("prop_physics_override");
        if (glow == null || !glow.IsValid || glow.Entity == null)
        {
            _failed.Add(index);
            return;
        }

        var body = glow.CBodyComponent?.SceneNode?.Owner?.Entity;
        if (body != null)
            body.Flags = (uint)(body.Flags & ~(1 << 2));

        glow.Spawnflags = 256u;
        glow.Render = Color.Transparent;
        glow.Teleport(weapon.AbsOrigin, weapon.AbsRotation, Vector.Zero);

        using (var keyValues = new CEntityKeyValues())
        {
            keyValues.SetString("model", modelName);
            keyValues.SetString("defaultanim", DroppedAnim);
            keyValues.SetInt("solid", 0);
            keyValues.SetInt("spawnflags", 256);
            glow.DispatchSpawn(keyValues);
        }

        if (!glow.IsValid)
        {
            _failed.Add(index);
            return;
        }

        var target = glow.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState;
        if (target != null)
        {
            target.MeshGroupMask = state!.MeshGroupMask;
            Utilities.SetStateChanged(glow, "CBaseEntity", "m_CBodyComponent");
        }

        glow.AcceptInput("SetAnimation", null, null, DroppedAnim);

        glow.Glow.GlowColorOverride = TrailBeam.Resolve(cfg.Color);
        glow.Glow.GlowRange = cfg.Range;
        glow.Glow.GlowRangeMin = 0;
        glow.Glow.GlowTeam = -1;
        glow.Glow.GlowType = 3;

        glow.AcceptInput("DisableMotion");
        MakeNonSolid(glow);

        _glows[index] = glow.Index;

        Server.NextFrame(() =>
        {
            if (!glow.IsValid || !weapon.IsValid)
                return;

            glow.AcceptInput("SetParent", weapon, glow, "!activator");
            Snap(glow);
        });
    }

    private void Verify(uint index, uint glowIndex)
    {
        var glow = Utilities.GetEntityFromIndex<CPhysicsProp>((int)glowIndex);
        if (glow != null && glow.IsValid)
            return;

        _glows.Remove(index);
        _failed.Add(index);
    }

    private static void Snap(CBaseEntity entity)
    {
        var node = entity.CBodyComponent?.SceneNode;
        if (node == null)
            return;

        node.Origin.X = 0f;
        node.Origin.Y = 0f;
        node.Origin.Z = 0f;
        node.Rotation.X = 0f;
        node.Rotation.Y = 0f;
        node.Rotation.Z = 0f;

        Utilities.SetStateChanged(entity, "CBaseEntity", "m_CBodyComponent");
    }

    private static void MakeNonSolid(CBaseEntity entity)
    {
        var collision = entity.Collision;
        if (collision == null)
            return;

        collision.SolidType = SolidType_t.SOLID_NONE;
        collision.SolidFlags = 4;
        collision.CollisionGroup = 2;
        collision.CollisionAttribute.CollisionGroup = 2;
        collision.CollisionAttribute.CollisionFunctionMask = 0;
    }

    private void Destroy(uint index)
    {
        if (!_glows.Remove(index, out uint glowIndex))
            return;

        var glow = Utilities.GetEntityFromIndex<CPhysicsProp>((int)glowIndex);
        if (glow != null && glow.IsValid)
            glow.Remove();
    }

    private void DestroyAll()
    {
        foreach (uint index in _glows.Keys.ToList())
            Destroy(index);
    }

    private void Clear()
    {
        DestroyAll();
        _weapons.Clear();
        _failed.Clear();
    }

    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_glows.Count == 0)
            return;

        foreach (var (info, viewer) in infoList)
        {
            if (viewer == null || !viewer.IsValid || Active(viewer))
                continue;

            foreach (uint glowIndex in _glows.Values)
                if (info.TransmitEntities.Contains(glowIndex))
                    info.TransmitEntities.Remove(glowIndex);
        }
    }
}
