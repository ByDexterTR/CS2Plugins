using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class SpawnEffect : VipModule
{
    private class Entry
    {
        public string Name { get; set; } = "";
        public string Particle { get; set; } = "";
        public float Lifetime { get; set; } = 3f;
        public bool Follow { get; set; } = true;
        public float Offset { get; set; } = 0f;
        public string Required { get; set; } = "";
        public string Team { get; set; } = "";
    }

    private const float DefaultLifetime = 3f;

    private readonly Dictionary<int, CParticleSystem> _active = new();

    public override string Name => "SpawnEffect";
    public override string DisplayName => Core.Localizer["vip.module.spawneffect"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player) =>
        Entries(player).Select(e => new VipFeatureOption(e.Name, e.Name)).ToList();

    private List<Entry> Entries(CCSPlayerController player) =>
        (GroupValue<List<Entry>>(player) ?? new())
        .Where(e => e.Name.Length > 0 && e.Particle.Length > 0 && Allowed(player, e.Required, e.Team))
        .ToList();

    public override void OnLoad()
    {
        EffectHide.Ensure(Core);
        Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) => { Stop(ev.Userid?.UserId); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) => { Stop(ev.Userid?.UserId); return HookResult.Continue; });
        Core.HookMapStart(_ => _active.Clear());
        Core.HookPrecache(manifest =>
        {
            foreach (var entries in Core.GetAllGroupValues<List<Entry>>(Name))
                foreach (var entry in entries)
                    if (entry.Particle.Length > 0)
                        manifest.AddResource(entry.Particle);
        });
    }

    public override void OnUnload()
    {
        foreach (var particle in _active.Values)
            if (particle.IsValid)
                particle.Remove();
        _active.Clear();
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (player?.UserId is not int userId || !Active(player))
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            var target = Utilities.GetPlayerFromUserid(userId);
            if (!IsAlive(target))
                return;

            string selected = Setting(target!);
            var entry = Entries(target!).FirstOrDefault(e => e.Name == selected);
            if (entry != null)
                Play(target!, userId, entry);
        });

        return HookResult.Continue;
    }

    private void Play(CCSPlayerController player, int userId, Entry entry)
    {
        var pawn = PlayerCache.Pawn(player);
        var origin = pawn?.AbsOrigin;
        if (pawn == null || !pawn.IsValid || origin == null)
            return;

        float lifetime = entry.Lifetime > 0 ? entry.Lifetime : DefaultLifetime;

        if (!entry.Follow)
        {
            ParticleUtil.Burst(Core, entry.Particle, new Vector(origin.X, origin.Y, origin.Z + entry.Offset),
                lifetime, EffectHide.SpawnEffect, player.Slot);
            return;
        }

        Stop(userId);
        var particle = ParticleUtil.SpawnAttached(entry.Particle, pawn, entry.Offset, EffectHide.SpawnEffect, player.Slot);
        if (particle == null)
            return;

        _active[userId] = particle;
        Core.AddTimer(lifetime, () =>
        {
            if (_active.TryGetValue(userId, out var current) && ReferenceEquals(current, particle))
                Stop(userId);
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }

    private void Stop(int? userId)
    {
        if (userId == null || !_active.Remove(userId.Value, out var particle))
            return;

        if (particle.IsValid)
            particle.Remove();
    }
}
