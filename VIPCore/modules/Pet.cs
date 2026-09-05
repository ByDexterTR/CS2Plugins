using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

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
        public Anim Animations { get; set; } = new();
    }

    private class Companion
    {
        public required CDynamicProp Prop;
        public required Entry Def;
        public required int UserId;
        public Vector Position = new(0, 0, 0);
        public float Yaw;
        public bool Running;
        public float LastTime;
    }

    private const float RunThreshold = 40f;
    private const float BobSpeed = 3.4f;
    private const float BobSize = 3f;
    private const float Smoothing = 6f;

    private readonly Dictionary<int, Companion> _pets = new();

    public override string Name => "Pet";
    public override string DisplayName => Core.Localizer["vip.module.pet"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player)
    {
        var entries = GroupValue<List<Entry>>(player) ?? new();
        return entries.Where(e => e.Name.Length > 0 && e.Model.Length > 0)
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
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) => { Kill(ev.Userid); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) => { Remove(ev.Userid?.UserId ?? -1); return HookResult.Continue; });
        Core.RegisterEventHandler<EventRoundEnd>((_, __) => { RemoveAll(); return HookResult.Continue; });
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
            Spawn(player);
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

    private void Kill(CCSPlayerController? player)
    {
        int userId = player?.UserId ?? -1;
        if (userId < 0 || !_pets.TryGetValue(userId, out var pet))
            return;

        if (pet.Def.Animations.Death.Length > 0 && pet.Prop.IsValid)
            pet.Prop.AcceptInput("SetAnimation", value: pet.Def.Animations.Death);

        Core.AddTimer(1.2f, () =>
        {
            if (_pets.TryGetValue(userId, out var current) && ReferenceEquals(current, pet))
                Remove(userId);
        });
    }

    private void Spawn(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || !IsAlive(player) || !Active(player))
            return;

        int userId = player.UserId ?? -1;
        if (userId < 0)
            return;

        Remove(userId);

        var entries = GroupValue<List<Entry>>(player) ?? new();
        var def = entries.FirstOrDefault(e => e.Name == Setting(player));
        if (def == null || def.Model.Length == 0)
            return;

        var pawn = player.PlayerPawn.Value;
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
        prop.SetModel(def.Model);
        prop.Teleport(start, new QAngle(), new Vector());
        prop.DispatchSpawn();
        prop.AcceptInput("Start");

        EffectHide.Track(EffectHide.Pet, prop.Index, player.Slot);

        var pet = new Companion
        {
            Prop = prop,
            Def = def,
            UserId = userId,
            Position = new Vector(start.X, start.Y, start.Z),
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

            string first = def.Animations.Spawn.Length > 0 ? def.Animations.Spawn : def.Animations.Idle;
            if (first.Length > 0)
                prop.AcceptInput("SetAnimation", value: first);

            if (def.Animations.Spawn.Length > 0 && def.Animations.Idle.Length > 0)
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

        float now = Server.CurrentTime;

        foreach (int userId in _pets.Keys.ToList())
        {
            var pet = _pets[userId];
            if (!pet.Prop.IsValid)
            {
                Remove(userId);
                continue;
            }

            var owner = Utilities.GetPlayerFromUserid(userId);
            var pawn = owner?.PlayerPawn.Value;
            var origin = pawn?.AbsOrigin;
            if (owner == null || !owner.IsValid || pawn == null || !pawn.IsValid || origin == null || !IsAlive(owner))
            {
                Remove(userId);
                continue;
            }

            var target = Chase(pet.Position, origin, pet.Def);

            float dt = Math.Clamp(now - pet.LastTime, 0f, 0.2f);
            pet.LastTime = now;

            Step(pet, target, dt);
        }
    }

    private static Vector Chase(Vector here, Vector origin, Entry def)
    {
        float height = origin.Z + (def.Flying ? def.Height : 0f);

        float dx = origin.X - here.X;
        float dy = origin.Y - here.Y;
        float flat = MathF.Sqrt(dx * dx + dy * dy);

        if (flat <= def.Distance)
            return new Vector(here.X, here.Y, height);

        float keep = def.Distance / flat;
        return new Vector(
            origin.X - dx * keep,
            origin.Y - dy * keep,
            height);
    }

    private void Step(Companion pet, Vector target, float dt)
    {
        float dx = target.X - pet.Position.X;
        float dy = target.Y - pet.Position.Y;
        float dz = target.Z - pet.Position.Z;
        float dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

        float ease = MathF.Min(1f, Smoothing * dt);
        float step = MathF.Min(pet.Def.Speed * dt, dist * ease + pet.Def.Speed * dt * 0.15f);
        float moved = 0f;

        if (dist > 0.5f)
        {
            moved = MathF.Min(step, dist);

            if (step >= dist)
            {
                pet.Position.X = target.X;
                pet.Position.Y = target.Y;
                pet.Position.Z = target.Z;
            }
            else
            {
                float scale = step / dist;
                pet.Position.X += dx * scale;
                pet.Position.Y += dy * scale;
                pet.Position.Z += dz * scale;
            }

            float want = MathF.Atan2(dy, dx) * (180f / MathF.PI);
            float turn = ((want - pet.Yaw + 540f) % 360f) - 180f;
            pet.Yaw += turn * MathF.Min(1f, 10f * dt);
        }

        bool running = dt > 0f && moved / dt > RunThreshold;

        float bob = pet.Def.Flying ? MathF.Sin(Server.CurrentTime * BobSpeed) * BobSize : 0f;
        var draw = new Vector(pet.Position.X, pet.Position.Y, pet.Position.Z + bob);

        pet.Prop.Teleport(draw, new QAngle(0, pet.Yaw, 0), new Vector());

        if (running == pet.Running)
            return;

        pet.Running = running;
        string anim = running ? pet.Def.Animations.Run : pet.Def.Animations.Idle;
        if (anim.Length > 0)
            pet.Prop.AcceptInput("SetAnimation", value: anim);
    }
}
