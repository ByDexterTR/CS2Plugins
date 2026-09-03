using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class Adrenaline : VipModule
{
    private class Cfg
    {
        public float Spk { get; set; } = 0.05f;
        [JsonPropertyName("maxspk")]
        public float MaxSpk { get; set; } = 0.5f;
        public float Duration { get; set; } = 0f;
    }

    private readonly float[] _bonus = new float[64];
    private readonly float[] _until = new float[64];
    private int _active;

    public override string Name => "Adrenaline";
    public override string DisplayName => Core.Localizer["vip.module.adrenaline"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerDeath>(OnDeath);
        Core.RegisterEventHandler<EventPlayerSpawn>((ev, _) =>
        {
            Reset(ev.Userid?.Slot ?? -1);
            return HookResult.Continue;
        });
        Core.RegisterEventHandler<EventRoundStart>((_, __) =>
        {
            for (int slot = 0; slot < 64; slot++)
                Reset(slot);
            return HookResult.Continue;
        });
        Core.HookTick(OnTick);
    }

    private void Reset(int slot)
    {
        if (slot < 0 || slot >= 64 || _bonus[slot] <= 0f)
            return;

        _bonus[slot] = 0f;
        _until[slot] = 0f;
        _active--;
    }

    private HookResult OnDeath(EventPlayerDeath ev, GameEventInfo info)
    {
        var attacker = ev.Attacker;
        var victim = ev.Userid;
        if (attacker == null || !attacker.IsValid || attacker.IsBot || victim == null
            || attacker.Slot == victim.Slot || !Active(attacker))
            return HookResult.Continue;

        int slot = attacker.Slot;
        if (slot < 0 || slot >= 64)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(attacker) ?? new Cfg();
        if (cfg.Spk <= 0f)
            return HookResult.Continue;

        if (_bonus[slot] <= 0f)
            _active++;

        _bonus[slot] = Math.Min(_bonus[slot] + cfg.Spk, Math.Max(cfg.MaxSpk, 0f));
        _until[slot] = cfg.Duration > 0f ? Server.CurrentTime + cfg.Duration : 0f;
        return HookResult.Continue;
    }

    private void OnTick()
    {
        if (_active == 0)
            return;

        for (int slot = 0; slot < 64; slot++)
        {
            float bonus = _bonus[slot];
            if (bonus <= 0f)
                continue;

            var player = Utilities.GetPlayerFromSlot(slot);
            if (!IsAlive(player) || !Active(player))
            {
                Restore(player);
                Reset(slot);
                continue;
            }

            if (_until[slot] > 0f && Server.CurrentTime >= _until[slot])
            {
                Restore(player);
                Reset(slot);
                continue;
            }

            var pawn = player!.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            float target = 1f + bonus;
            if (Math.Abs(pawn.VelocityModifier - target) > 0.001f)
            {
                pawn.VelocityModifier = target;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }
    }

    private static void Restore(CCSPlayerController? player)
    {
        var pawn = player?.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || Math.Abs(pawn.VelocityModifier - 1f) <= 0.001f)
            return;

        pawn.VelocityModifier = 1f;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
    }
}
