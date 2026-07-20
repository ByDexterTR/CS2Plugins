using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public static class ParticleUtil
{
    public static CParticleSystem? Spawn(string effectPath, Vector position, CBaseEntity? follow = null)
    {
        if (string.IsNullOrEmpty(effectPath))
            return null;

        var particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
        if (particle == null || !particle.IsValid)
            return null;

        particle.EffectName = effectPath;
        particle.StartActive = true;
        particle.Teleport(position, new QAngle(), new Vector());
        particle.DispatchSpawn();
        particle.AcceptInput("Start");

        if (follow != null && follow.IsValid)
            particle.AcceptInput("FollowEntity", follow, particle, "!activator");

        return particle;
    }

    public static void Burst(BasePlugin plugin, string effectPath, Vector position, float lifetime)
    {
        var particle = Spawn(effectPath, position);
        if (particle == null)
            return;

        plugin.AddTimer(lifetime, () =>
        {
            if (particle.IsValid)
                particle.Remove();
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }
}
