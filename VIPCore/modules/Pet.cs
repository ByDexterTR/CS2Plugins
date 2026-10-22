using System.Globalization;
using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace VIPCore;

public class Pet : VipModule
{
    public class Anim
    {
        public string Idle { get; set; } = "";
        public string Run { get; set; } = "";
        public string Death { get; set; } = "";
        public string Spawn { get; set; } = "";
    }

    public class Entry
    {
        public string Name { get; set; } = "";
        public string Model { get; set; } = "";
        public bool Flying { get; set; }
        public float Speed { get; set; } = 260f;
        public float Distance { get; set; } = 55f;
        public float Height { get; set; } = 45f;
        public float Scale { get; set; } = 1f;
        public float Yaw { get; set; } = 0f;
        public string Required { get; set; } = "";
        public string Team { get; set; } = "";
        public Anim Animations { get; set; } = new();
    }

    private class Companion
    {
        public required CDynamicProp Prop;
        public required Entry Def;
        public required int UserId;
        public required int Slot;
        public Vector3 Position;
        public Vector3 Owner;
        public float Yaw;
        public bool Running;
        public float LastTime;
        public bool Dying;
    }

    private const float RunThreshold = 40f;
    private const float BobSpeed = 3.4f;
    private const float BobSize = 3f;
    private const float Smoothing = 6f;
    private const float TeleportJump = 300f;
    private const float SnapDistance = 1500f;

    private readonly Dictionary<int, Companion> _pets = new();
    private readonly List<int> _gone = new();
    private readonly List<int> _lost = new();
    private readonly Vector _draw = new();
    private readonly QAngle _angle = new();

    public override string Name => "Pet";
    public override string DisplayName => Core.Localizer["vip.module.pet"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player)
    {
        var entries = GroupValue<List<Entry>>(player) ?? new();
        return entries.Where(e => e.Name.Length > 0 && e.Model.Length > 0 && HasFlags(player, e.Required))
            .Select(e => new VipFeatureOption(e.Name, e.Name)).ToList();
    }

    public override void OnLoad()
    {
        EffectHide.Ensure(Core);

        Core.RegisterEventHandler<EventPlayerSpawn>((ev, _) =>
        {
            var player = ev.Userid;
            Server.NextFrame(() => Spawn(player));
            return HookResult.Continue;
        });
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) =>
        {
            int userId = ev.Userid?.UserId ?? -1;
            if (_pets.TryGetValue(userId, out var pet))
                Die(userId, pet);
            return HookResult.Continue;
        });
        Core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) => { Remove(ev.Userid?.UserId ?? -1); return HookResult.Continue; });
        Core.HookMapStart(_ => _pets.Clear());
        Core.HookTick(OnTick);
        Core.HookPrecache(manifest =>
        {
            foreach (var entries in Core.GetAllGroupValues<List<Entry>>(Name))
                foreach (var entry in entries)
                    if (entry.Model.Length > 0)
                        try { manifest.AddResource(entry.Model); }
                        catch { }
        });
    }

    public override void OnUnload() => RemoveAll();

    public override void OnSelect(CCSPlayerController player, string value)
    {
        Remove(player.UserId ?? -1);
        if (value != "off")
            Server.NextFrame(() => Spawn(player, true));
    }

    private void RemoveAll()
    {
        foreach (int userId in _pets.Keys.ToList())
            Remove(userId);
    }

    private void Remove(int userId)
    {
        if (userId < 0 || !_pets.Remove(userId, out var pet))
            return;

        if (pet.Prop.IsValid)
            pet.Prop.Remove();
    }

    private void Die(int userId, Companion pet)
    {
        if (pet.Dying)
            return;

        pet.Dying = true;
        pet.Running = false;
        if (pet.Def.Animations.Death.Length > 0 && pet.Prop.IsValid)
            pet.Prop.AcceptInput("SetAnimation", value: pet.Def.Animations.Death);

        Core.AddTimer(1.2f, () =>
        {
            if (_pets.TryGetValue(userId, out var current) && ReferenceEquals(current, pet) && pet.Dying)
                Remove(userId);
        });
    }

    private static string PetName(CCSPlayerController player) => $"pet_{player.SteamID}";

    private static void RemoveNamed(string name, CDynamicProp? keep)
    {
        foreach (var prop in Utilities.FindAllEntitiesByDesignerName<CDynamicProp>("prop_dynamic"))
            if (prop.IsValid && prop.Entity?.Name == name && (keep == null || prop.Handle != keep.Handle))
                prop.Remove();
    }

    private bool Revive(CCSPlayerController player, Companion pet, Entry def, string name)
    {
        if (pet.Def.Name != def.Name || pet.Def.Model != def.Model || !pet.Prop.IsValid || pet.Prop.Entity?.Name != name)
            return false;

        var pawn = PlayerCache.Pointer(player);
        if (pawn == 0 || !PlayerCache.Origin(pawn, out var origin))
            return false;

        pet.Def = def;
        pet.Slot = player.Slot;
        pet.Owner = origin;
        pet.Position = Chase(pet.Position, origin, def);
        pet.LastTime = Server.CurrentTime;
        pet.Running = false;

        if (pet.Dying)
        {
            pet.Dying = false;
            if (def.Animations.Idle.Length > 0)
                pet.Prop.AcceptInput("SetAnimation", value: def.Animations.Idle);
        }

        RemoveNamed(name, pet.Prop);
        return true;
    }

    private void Spawn(CCSPlayerController? player, bool instant = false)
    {
        if (player == null || !player.IsValid || player.IsBot || !IsAlive(player) || !Active(player))
            return;

        int userId = player.UserId ?? -1;
        if (userId < 0)
            return;

        var entries = GroupValue<List<Entry>>(player) ?? new();
        var def = entries.FirstOrDefault(e => e.Name == Setting(player) && Allowed(player, e.Required, e.Team));
        string name = PetName(player);

        if (def != null && !instant && _pets.TryGetValue(userId, out var existing) && Revive(player, existing, def, name))
            return;

        Remove(userId);
        RemoveNamed(name, null);

        if (def == null || def.Model.Length == 0)
            return;

        var pawn = PlayerCache.Pawn(player);
        var origin = pawn?.AbsOrigin;
        if (pawn == null || !pawn.IsValid || origin == null)
            return;

        if (!EffectHide.AnyViewer(EffectHide.Pet, player.Slot))
            return;

        var start = new Vector(origin.X, origin.Y, origin.Z + (def.Flying ? def.Height : 0f));

        var prop = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
        if (prop == null || !prop.IsValid)
            return;

        prop.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);

        var keys = new CEntityKeyValues();
        keys.SetString("model", def.Model);
        keys.SetString("targetname", name);
        keys.SetInt("spawnflags", 256);
        keys.SetVector("origin", start);
        prop.DispatchSpawn(keys);
        keys.Dispose();

        if (!prop.IsValid)
            return;

        var collision = prop.Collision;
        if (collision != null)
        {
            collision.SolidType = SolidType_t.SOLID_NONE;
            collision.SolidFlags = 12;
        }

        prop.Teleport(start, new QAngle(0, def.Yaw, 0), new Vector());

        EffectHide.Track(EffectHide.Pet, prop.Index, player.Slot);

        var pet = new Companion
        {
            Prop = prop,
            Def = def,
            UserId = userId,
            Slot = player.Slot,
            Position = new Vector3(start.X, start.Y, start.Z),
            Owner = new Vector3(origin.X, origin.Y, origin.Z),
            LastTime = Server.CurrentTime
        };

        _pets[userId] = pet;

        Server.NextWorldUpdate(() =>
        {
            if (!prop.IsValid)
                return;

            if (Math.Abs(def.Scale - 1f) > 0.01f)
            {
                var skeleton = prop.CBodyComponent?.SceneNode?.GetSkeletonInstance();
                if (skeleton != null)
                    skeleton.Scale = def.Scale;
                prop.AcceptInput("SetScale", null, null, def.Scale.ToString(CultureInfo.InvariantCulture));
            }

            bool intro = !instant && def.Animations.Spawn.Length > 0;
            string first = intro ? def.Animations.Spawn : def.Animations.Idle;
            if (first.Length > 0)
                prop.AcceptInput("SetAnimation", value: first);

            if (intro && def.Animations.Idle.Length > 0)
                Core.AddTimer(1.1f, () =>
                {
                    if (prop.IsValid)
                        prop.AcceptInput("SetAnimation", value: def.Animations.Idle);
                });
        });
    }

    private void OnTick()
    {
        if (_pets.Count == 0)
            return;

        _lost.Clear();
        float now = Server.CurrentTime;
        _gone.Clear();

        foreach (var (userId, pet) in _pets)
        {
            if (!pet.Prop.IsValid)
            {
                _gone.Add(userId);
                if (!pet.Dying)
                    _lost.Add(pet.Slot);
                continue;
            }

            if (pet.Dying)
                continue;

            var owner = Core.PlayerAt(pet.Slot);
            if (owner == null || owner.UserId != userId)
                continue;

            nint pawn = PlayerCache.Pointer(owner);
            if (pawn == 0)
                continue;

            if (!IsAlive(owner))
            {
                Die(userId, pet);
                continue;
            }

            if (!TeamMatches(owner, pet.Def.Team))
            {
                _gone.Add(userId);
                continue;
            }

            if (!PlayerCache.Origin(pawn, out var origin))
                continue;

            float dt = Math.Clamp(now - pet.LastTime, 0f, 0.2f);
            pet.LastTime = now;

            float ownerSpeed = PlayerCache.Velocity(pawn).Length();
            float jump = Vector3.Distance(pet.Owner, origin);
            pet.Owner = origin;

            var target = Chase(pet.Position, origin, pet.Def);
            bool teleported = jump > TeleportJump && jump > ownerSpeed * dt * 3f + 64f;
            if (teleported || Vector3.Distance(pet.Position, origin) > SnapDistance)
            {
                pet.Position = target;
                pet.Yaw = MathF.Atan2(origin.Y - target.Y, origin.X - target.X) * (180f / MathF.PI);
            }

            Step(pet, target, dt, MathF.Max(pet.Def.Speed, ownerSpeed * 1.1f));
        }

        foreach (int userId in _gone)
            Remove(userId);

        if (_lost.Count == 0)
            return;

        foreach (int slot in _lost)
        {
            var player = Core.PlayerAt(slot);
            if (player != null && IsAlive(player))
                Server.NextFrame(() => Spawn(player, true));
        }
        _lost.Clear();
    }

    private static Vector3 Chase(Vector3 here, Vector3 origin, Entry def)
    {
        float height = origin.Z + (def.Flying ? def.Height : 0f);

        float dx = origin.X - here.X;
        float dy = origin.Y - here.Y;
        float flat = MathF.Sqrt(dx * dx + dy * dy);

        if (flat <= def.Distance)
            return new Vector3(here.X, here.Y, height);

        float keep = def.Distance / flat;
        return new Vector3(origin.X - dx * keep, origin.Y - dy * keep, height);
    }

    private void Step(Companion pet, Vector3 target, float dt, float speed)
    {
        var delta = target - pet.Position;
        float dist = delta.Length();

        float ease = MathF.Min(1f, Smoothing * dt);
        float step = MathF.Min(speed * dt, dist * ease + speed * dt * 0.15f);
        float moved = 0f;

        if (dist > 0.5f)
        {
            moved = MathF.Min(step, dist);
            pet.Position = step >= dist ? target : pet.Position + delta * (step / dist);

            float want = MathF.Atan2(delta.Y, delta.X) * (180f / MathF.PI);
            float turn = ((want - pet.Yaw + 540f) % 360f) - 180f;
            pet.Yaw += turn * MathF.Min(1f, 10f * dt);
        }

        bool running = dt > 0f && moved / dt > RunThreshold;

        float bob = pet.Def.Flying ? MathF.Sin(Server.CurrentTime * BobSpeed) * BobSize : 0f;
        _draw.X = pet.Position.X;
        _draw.Y = pet.Position.Y;
        _draw.Z = pet.Position.Z + bob;
        _angle.Y = pet.Yaw + pet.Def.Yaw;

        Teleporter.Teleport(pet.Prop, _draw, _angle, TrailBeam.ZeroVector);

        if (running == pet.Running)
            return;

        pet.Running = running;
        string anim = running ? pet.Def.Animations.Run : pet.Def.Animations.Idle;
        if (anim.Length > 0)
            pet.Prop.AcceptInput("SetAnimation", value: anim);
    }
}
