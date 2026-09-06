using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class HitMarker : VipModule
{
    private class Entry
    {
        public string Name { get; set; } = "";
        public string Particle { get; set; } = "";
        public string Headshot { get; set; } = "";
        public float Duration { get; set; } = 0.25f;
        public float Distance { get; set; } = 60f;
        public float OffsetX { get; set; } = 0f;
        public float OffsetY { get; set; } = 0f;
    }

    private class Live
    {
        public required CParticleSystem Particle;
        public required CCSPlayerController Owner;
        public required Entry Def;
    }

    private static ConVar? _cvFfa;

    private readonly Live?[] _active = new Live?[64];
    private readonly Dictionary<uint, ulong> _viewers = new();
    private int _count;

    public override string Name => "HitMarker";
    public override string DisplayName => Core.Localizer["vip.module.hitmarker"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player) =>
        Entries(player).Select(e => new VipFeatureOption(e.Name, e.Name)).ToList();

    private List<Entry> Entries(CCSPlayerController player) =>
        (GroupValue<List<Entry>>(player) ?? new())
            .Where(e => e.Name.Length > 0 && e.Particle.Length > 0).ToList();

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerHurt>(OnHurt);
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) => { Remove(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) => { Remove(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        Core.RegisterEventHandler<EventRoundEnd>((_, __) => { RemoveAll(); return HookResult.Continue; });
        Core.HookTick(OnTick);
        Core.HookTransmit(OnCheckTransmit);
        Core.HookEntityDeleted(entity =>
        {
            if (_viewers.Count > 0)
                _viewers.Remove(entity.Index);
        });
        Core.HookMapStart(_ => { Array.Clear(_active); _viewers.Clear(); _count = 0; });
        Core.HookPrecache(manifest =>
        {
            foreach (var entries in Core.GetAllGroupValues<List<Entry>>(Name))
                foreach (var entry in entries)
                {
                    if (entry.Particle.Length > 0)
                        try { manifest.AddResource(entry.Particle); } catch { }
                    if (entry.Headshot.Length > 0)
                        try { manifest.AddResource(entry.Headshot); } catch { }
                }
        });
    }

    public override void OnUnload() => RemoveAll();

    public override void OnSelect(CCSPlayerController player, string value) => Remove(player.Slot);

    private HookResult OnHurt(EventPlayerHurt ev, GameEventInfo info)
    {
        var attacker = ev.Attacker;
        var victim = ev.Userid;
        if (attacker == null || !attacker.IsValid || attacker.IsBot || victim == null || attacker.Slot == victim.Slot)
            return HookResult.Continue;

        if (!Active(attacker))
            return HookResult.Continue;

        _cvFfa ??= ConVar.Find("mp_teammates_are_enemies");
        bool ffa = _cvFfa?.GetPrimitiveValue<bool>() ?? false;
        if (!ffa && victim.Team == attacker.Team)
            return HookResult.Continue;

        string setting = Setting(attacker);
        if (setting.Length == 0 || setting == "off")
            return HookResult.Continue;

        var entry = Entries(attacker).FirstOrDefault(e => e.Name == setting);
        if (entry == null)
            return HookResult.Continue;

        Show(attacker, entry, ev.Hitgroup == (int)HitGroup_t.HITGROUP_HEAD);
        return HookResult.Continue;
    }

    private void Show(CCSPlayerController player, Entry entry, bool headshot)
    {
        int slot = player.Slot;
        if (slot < 0 || slot >= 64)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
            return;

        string file = headshot && entry.Headshot.Length > 0 ? entry.Headshot : entry.Particle;
        if (file.Length == 0)
            return;

        Remove(slot);

        var particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
        if (particle == null || !particle.IsValid)
            return;

        particle.EffectName = file;
        particle.StartActive = true;
        particle.Teleport(Aim(pawn, entry), Look(pawn), new Vector());
        particle.DispatchSpawn();
        particle.AcceptInput("Start");

        ulong mask = 1UL << slot;
        var watchers = new List<CCSPlayerController>();
        AddSpectators(player, watchers);
        foreach (var watcher in watchers)
            if (watcher.Slot < 64)
                mask |= 1UL << watcher.Slot;

        _viewers[particle.Index] = mask;
        _active[slot] = new Live { Particle = particle, Owner = player, Def = entry };
        _count++;

        Core.AddTimer(MathF.Max(entry.Duration, 0.05f), () =>
        {
            var live = _active[slot];
            if (live != null && ReferenceEquals(live.Particle, particle))
            {
                _active[slot] = null;
                _count--;
            }

            ParticleTrail.Stop(particle);
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }

    private static QAngle Look(CCSPlayerPawn pawn)
    {
        var eye = pawn.EyeAngles;
        return new QAngle(eye.X, eye.Y, eye.Z);
    }

    private static Vector Aim(CCSPlayerPawn pawn, Entry entry)
    {
        var origin = pawn.AbsOrigin!;
        var view = pawn.ViewOffset;
        var eye = pawn.EyeAngles;

        float pitch = eye.X * (MathF.PI / 180f);
        float yaw = eye.Y * (MathF.PI / 180f);
        float cp = MathF.Cos(pitch), sp = MathF.Sin(pitch);
        float cy = MathF.Cos(yaw), sy = MathF.Sin(yaw);

        float distance = MathF.Max(entry.Distance, 0f);

        return new Vector(
            origin.X + view.X + cp * cy * distance + sy * entry.OffsetX,
            origin.Y + view.Y + cp * sy * distance - cy * entry.OffsetX,
            origin.Z + view.Z - sp * distance + entry.OffsetY);
    }

    private void OnTick()
    {
        if (_count == 0)
            return;

        for (int slot = 0; slot < 64; slot++)
        {
            var live = _active[slot];
            if (live == null)
                continue;

            if (!live.Particle.IsValid)
            {
                _active[slot] = null;
                _count--;
                continue;
            }

            var pawn = live.Owner.IsValid ? live.Owner.PlayerPawn.Value : null;
            if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
                continue;

            live.Particle.Teleport(Aim(pawn, live.Def), Look(pawn), new Vector());
        }
    }

    private void RemoveAll()
    {
        for (int slot = 0; slot < 64; slot++)
            Remove(slot);
    }

    private void Remove(int slot)
    {
        if (slot < 0 || slot >= 64)
            return;

        var live = _active[slot];
        if (live == null)
            return;

        _active[slot] = null;
        _count--;
        ParticleTrail.Stop(live.Particle);
    }

    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_viewers.Count == 0)
            return;

        foreach (var (info, viewer) in infoList)
        {
            if (viewer == null || !viewer.IsValid || viewer.Slot >= 64)
                continue;

            ulong bit = 1UL << viewer.Slot;
            foreach (var (index, mask) in _viewers)
                if ((mask & bit) == 0 && info.TransmitEntities.Contains(index))
                    info.TransmitEntities.Remove(index);
        }
    }
}
