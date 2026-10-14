using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class PlayerTrail : VipModule
{
    private class Cfg
    {
        public float Width { get; set; } = 1.0f;
        public float Lifetime { get; set; } = 2.0f;
        public List<string> Colors { get; set; } = new();
        public List<ParticleEntry> Particles { get; set; } = new();
    }

    private const float MinSegment = 12f;
    private static readonly Cfg DefaultCfg = new();
    private readonly System.Numerics.Vector3[] _last = new System.Numerics.Vector3[64];
    private readonly Vector _from = new();
    private readonly Vector _to = new();
    private readonly CParticleSystem?[] _active = new CParticleSystem?[64];
    private readonly float[] _carried = new float[64];

    public override string Name => "PlayerTrail";
    public override string DisplayName => Core.Localizer["vip.module.playertrail"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player)
    {
        var cfg = GroupValue<Cfg>(player) ?? new Cfg();
        var options = TrailBeam.ParseColorOptions(cfg.Colors);
        ParticleTrail.AddOptions(options, cfg.Particles, player);
        return options;
    }

    public override void OnLoad()
    {
        EffectHide.Ensure(Core);
        Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) => { Remove(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) => { Remove(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        Core.HookMapStart(_ => Array.Clear(_active));
        Core.HookTick(OnTick, 2);
        Core.HookPrecache(manifest =>
        {
            foreach (var cfg in Core.GetAllGroupValues<Cfg>(Name))
                ParticleTrail.Precache(manifest, cfg.Particles);
        });
    }

    public override void OnUnload() => RemoveAll();

    public override void OnSelect(CCSPlayerController player, string value)
    {
        Remove(player.Slot);
        if (value != "off")
            Apply(player);
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        int slot = player?.Slot ?? -1;
        if (slot >= 0 && slot < 64)
        {
            _last[slot] = System.Numerics.Vector3.Zero;
            Remove(slot);
        }

        Server.NextFrame(() => Apply(player));
        return HookResult.Continue;
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

        var particle = _active[slot];
        _active[slot] = null;
        ParticleTrail.Stop(particle);
    }

    private void Apply(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || !IsAlive(player) || !Active(player))
            return;

        var cfg = GroupValue<Cfg>(player) ?? new Cfg();
        var entry = ParticleTrail.Find(cfg.Particles, Setting(player), player);
        if (entry == null || !entry.Follow)
            return;

        var pawn = PlayerCache.Pawn(player);
        if (pawn == null || !pawn.IsValid)
            return;

        Remove(player.Slot);
        _active[player.Slot] = ParticleTrail.Carry(pawn, entry, entry.Offset, EffectHide.PlayerTrail, player.Slot);
        _carried[player.Slot] = entry.Offset;
    }

    private void OnTick()
    {
        foreach (var player in ActivePlayers())
        {
            int slot = player.Slot;
            if (!PlayerCache.Origin(PlayerCache.Pointer(player), out var origin))
                continue;

            var carried = _active[slot];
            if (carried != null)
            {
                if (!carried.IsValid)
                {
                    _active[slot] = null;
                    continue;
                }

                _from.X = origin.X;
                _from.Y = origin.Y;
                _from.Z = origin.Z + _carried[slot];
                carried.Teleport(_from, TrailBeam.ZeroAngle, TrailBeam.ZeroVector);
                continue;
            }

            ref var last = ref _last[slot];
            if (last == System.Numerics.Vector3.Zero)
            {
                last = origin;
                continue;
            }

            float dist = System.Numerics.Vector3.Distance(last, origin);
            if (dist < MinSegment)
                continue;

            if (dist < 250)
            {
                string setting = Setting(player);
                var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;
                var entry = ParticleTrail.Find(cfg.Particles, setting, player);

                if (entry != null)
                {
                    var from = new Vector(last.X, last.Y, last.Z + entry.Offset);
                    var to = new Vector(origin.X, origin.Y, origin.Z + entry.Offset);
                    ParticleTrail.Tracer(Core, from, to, entry, cfg.Lifetime, EffectHide.PlayerTrail, slot);
                }
                else if (!ParticleTrail.IsParticle(setting))
                {
                    var color = TrailBeam.IsRandom(setting) ? Core.RoundColor(slot) : TrailBeam.Resolve(setting);
                    _from.X = origin.X; _from.Y = origin.Y; _from.Z = origin.Z;
                    _to.X = last.X; _to.Y = last.Y; _to.Z = last.Z;
                    TrailBeam.Create(Core, _from, _to, color, cfg.Width, cfg.Lifetime, EffectHide.PlayerTrail, slot);
                }
            }

            last = origin;
        }
    }
}
