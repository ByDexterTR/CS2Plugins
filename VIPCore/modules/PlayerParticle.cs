using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class PlayerParticle : VipModule
{
    private class Entry
    {
        public string Name { get; set; } = "";
        public string Particle { get; set; } = "";
        public float Offset { get; set; } = 15f;
    }

    private readonly CParticleSystem?[] _active = new CParticleSystem?[64];

    public override string Name => "PlayerParticle";
    public override string DisplayName => Core.Localizer["vip.module.playerparticle"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player)
    {
        var entries = GroupValue<List<Entry>>(player) ?? new();
        return entries.Where(e => e.Name.Length > 0 && e.Particle.Length > 0)
            .Select(e => new VipFeatureOption(e.Name, e.Name)).ToList();
    }

    public override void OnLoad()
    {
        EffectHide.Ensure(Core);

        Core.RegisterEventHandler<EventPlayerSpawn>((ev, _) =>
        {
            var player = ev.Userid;
            Server.NextFrame(() => Apply(player));
            return HookResult.Continue;
        });
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) => { Remove(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) => { Remove(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        Core.RegisterEventHandler<EventRoundEnd>((_, __) => { RemoveAll(); return HookResult.Continue; });
        Core.RegisterEventHandler<EventRoundStart>((_, __) => { RemoveAll(); return HookResult.Continue; });
        Core.HookMapStart(_ => Array.Clear(_active));
    }

    public override void OnUnload() => RemoveAll();

    public override void OnSelect(CCSPlayerController player, string value)
    {
        Remove(player.Slot);
        if (value != "off")
            Apply(player);
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

        if (particle == null || !particle.IsValid || particle.DesignerName != "info_particle_system")
            return;

        particle.AcceptInput("Stop");
        particle.Remove();
    }

    private void Apply(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.IsBot || !IsAlive(player) || !Active(player))
            return;

        int slot = player.Slot;
        Remove(slot);

        var entries = GroupValue<List<Entry>>(player) ?? new();
        var entry = entries.FirstOrDefault(e => e.Name == Setting(player));
        if (entry == null || entry.Particle.Length == 0)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        _active[slot] = ParticleUtil.SpawnAttached(entry.Particle, pawn, entry.Offset, EffectHide.PlayerParticle, slot);
    }
}
