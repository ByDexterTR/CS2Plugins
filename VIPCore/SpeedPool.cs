using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public static class SpeedPool
{
    private const int Sources = 9;

    public const int ExtraSpeed = 0;
    public const int Adrenaline = 1;
    public const int Healthshot = 2;
    public const int HealthshotSlow = 3;
    public const int AuraSpeed = 4;
    public const int AuraSlow = 5;
    public const int BulletSlow = 6;
    public const int DecoySlow = 7;
    public const int SmokeSlow = 8;

    private static VIPCore? _owner;
    private static readonly float[,] _factor = new float[64, Sources];
    private static readonly int[,] _until = new int[64, Sources];
    private static readonly int[,] _at = new int[64, Sources];
    private static readonly bool[] _managed = new bool[64];
    private static readonly float[] _written = new float[64];
    private static readonly float[] _floor = new float[64];
    private static readonly int[] _floorUntil = new int[64];

    public static void Ensure(VIPCore core)
    {
        if (ReferenceEquals(_owner, core))
            return;

        _owner = core;
        Array.Clear(_factor);
        Array.Clear(_until);
        Array.Clear(_managed);

        core.HookTick(OnTick);
        core.RegisterEventHandler<EventRoundStart>((_, _) => { Array.Clear(_until); Array.Clear(_floorUntil); return HookResult.Continue; });
        core.HookMapStart(_ => { Array.Clear(_until); Array.Clear(_floorUntil); Array.Clear(_managed); });
    }

    public static int TicksFor(float seconds) => Math.Max((int)MathF.Ceiling(seconds * 64f), 1) + 2;

    public static void Request(int slot, int source, float factor, int ticks = 2)
    {
        if (_owner == null || slot < 0 || slot >= 64 || factor < 0f)
            return;

        int now = Server.TickCount;
        int until = now + Math.Max(ticks, 1);

        if (_at[slot, source] == now && _until[slot, source] >= now)
        {
            float current = _factor[slot, source];
            factor = factor >= 1f && current >= 1f ? MathF.Max(current, factor)
                : factor < 1f && current < 1f ? MathF.Min(current, factor)
                : factor;
        }

        _factor[slot, source] = factor;
        _until[slot, source] = until;
        _at[slot, source] = now;
    }

    public static void Floor(int slot, float value, int ticks = 2)
    {
        if (_owner == null || slot < 0 || slot >= 64)
            return;

        _floor[slot] = value;
        _floorUntil[slot] = Server.TickCount + Math.Max(ticks, 1);
    }

    public static void Clear(int slot, int source)
    {
        if (slot >= 0 && slot < 64)
            _until[slot, source] = 0;
    }

    private static void OnTick()
    {
        int now = Server.TickCount;

        for (int slot = 0; slot < 64; slot++)
        {
            float bonus = 0f;
            float slow = 1f;
            bool any = false;

            for (int source = 0; source < Sources; source++)
            {
                if (_until[slot, source] < now)
                    continue;

                any = true;
                float factor = _factor[slot, source];
                if (factor >= 1f)
                    bonus += factor - 1f;
                else
                    slow = MathF.Min(slow, factor);
            }

            bool floored = _floorUntil[slot] >= now;
            if (floored)
                any = true;

            if (!any && !_managed[slot])
                continue;

            var player = Utilities.GetPlayerFromSlot(slot);
            var pawn = player?.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            {
                _managed[slot] = false;
                continue;
            }

            if (!any)
            {
                _managed[slot] = false;
                if (Math.Abs(pawn.VelocityModifier - _written[slot]) > 0.001f || Math.Abs(pawn.VelocityModifier - 1f) <= 0.001f)
                    continue;

                pawn.VelocityModifier = 1f;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                continue;
            }

            float target = (1f + bonus) * slow;
            if (floored)
                target = MathF.Max(target, _floor[slot]);
            _managed[slot] = true;
            _written[slot] = target;

            if (Math.Abs(pawn.VelocityModifier - target) <= 0.001f)
                continue;

            pawn.VelocityModifier = target;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
        }
    }
}
