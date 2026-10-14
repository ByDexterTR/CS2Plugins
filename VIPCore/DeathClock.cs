using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public static class DeathClock
{
    public const float SafeDelay = 5f;

    private static VIPCore? _owner;
    private static readonly Dictionary<int, float> _died = new();

    public static void Ensure(VIPCore core)
    {
        if (ReferenceEquals(_owner, core))
            return;

        _owner = core;
        _died.Clear();

        core.RegisterEventHandler<EventPlayerDeath>((ev, _) =>
        {
            if (ev.Userid?.UserId is int userId && !ev.Get<bool>("infected"))
                _died[userId] = Server.CurrentTime;
            return HookResult.Continue;
        });
        core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) =>
        {
            if (ev.Userid?.UserId is int userId)
                _died.Remove(userId);
            return HookResult.Continue;
        });
        core.HookMapStart(_ => _died.Clear());
    }

    public static float Wait(CCSPlayerController player, float delay = SafeDelay)
    {
        if (player.UserId is not int userId || !_died.TryGetValue(userId, out float died))
            return 0f;

        return Math.Max(0f, delay - (Server.CurrentTime - died));
    }
}
