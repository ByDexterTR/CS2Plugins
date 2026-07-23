using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public static class ParticleUtil
{
    public static CParticleSystem? Spawn(string effectPath, Vector position, CBaseEntity? follow = null,
        int hideModule = -1, int ownerSlot = -1)
    {
        if (string.IsNullOrEmpty(effectPath))
            return null;

        if (hideModule >= 0 && !EffectHide.AnyViewer(hideModule, ownerSlot))
            return null;

        var particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
        if (particle == null || !particle.IsValid)
            return null;

        if (hideModule >= 0)
            EffectHide.Track(hideModule, particle.Index, ownerSlot);

        particle.EffectName = effectPath;
        particle.StartActive = true;
        particle.Teleport(position, new QAngle(), new Vector());
        particle.DispatchSpawn();
        particle.AcceptInput("Start");

        if (follow != null && follow.IsValid)
            particle.AcceptInput("FollowEntity", follow, particle, "!activator");

        return particle;
    }

    public static void Burst(BasePlugin plugin, string effectPath, Vector position, float lifetime,
        int hideModule = -1, int ownerSlot = -1)
    {
        var particle = Spawn(effectPath, position, null, hideModule, ownerSlot);
        if (particle == null)
            return;

        plugin.AddTimer(lifetime, () =>
        {
            if (particle.IsValid)
                particle.Remove();
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }
}
