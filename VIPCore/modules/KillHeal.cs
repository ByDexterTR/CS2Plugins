using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace VIPCore;

public class KillHeal : VipModule
{
    public override string Name => "KillHeal";
    public override string DisplayName => Core.Localizer["vip.module.killheal"];

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerDeath>(OnDeath);

    private HookResult OnDeath(EventPlayerDeath ev, GameEventInfo info)
    {
        var attacker = ev.Attacker;
        var victim = ev.Userid;
        if (attacker == null || !attacker.IsValid || attacker.IsBot || victim == null || !victim.IsValid)
            return HookResult.Continue;

        bool ffa = ConVar.Find("mp_teammates_are_enemies")?.GetPrimitiveValue<bool>() ?? false;
        if (attacker.Slot == victim.Slot || (!ffa && attacker.Team == victim.Team) || !Active(attacker) || !IsAlive(attacker))
            return HookResult.Continue;

        var cfg = GroupValue<Dictionary<string, JsonElement>>(attacker);
        if (cfg == null || cfg.Count == 0)
            return HookResult.Continue;

        int total = 0;
        var reasons = new List<string>();

        void Try(string key, bool condition)
        {
            if (condition && GetInt(cfg, key) is int hp and > 0)
            {
                total += hp;
                reasons.Add(key);
            }
        }

        Try("headshot", ev.Headshot);
        Try("noscope", ev.Noscope);
        Try("inair", ev.Attackerinair);
        Try("blind", ev.Attackerblind);

        string weapon = ev.Weapon.Contains("knife") || ev.Weapon.Contains("bayonet") ? "knife" : ev.Weapon;
        Try("weapon_" + weapon, true);

        if (cfg.TryGetValue("distance", out var dist) && dist.ValueKind == JsonValueKind.Object)
        {
            float unit = dist.TryGetProperty("unit", out var u) && u.TryGetSingle(out float uv) ? uv : 0f;
            int hp = dist.TryGetProperty("money", out var m) && m.TryGetInt32(out int mv) ? mv
                : dist.TryGetProperty("hp", out var h) && h.TryGetInt32(out int hv) ? hv : 0;

            var aPos = attacker.PlayerPawn.Value?.AbsOrigin;
            var vPos = victim.PlayerPawn.Value?.AbsOrigin;
            if (unit > 0 && hp > 0 && aPos != null && vPos != null)
            {
                int steps = (int)(TrailBeam.Distance(aPos, vPos) / unit);
                if (steps > 0)
                {
                    total += hp * steps;
                    reasons.Add("distance");
                }
            }
        }

        if (total <= 0)
            return HookResult.Continue;

        var pawn = attacker.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        int maxHp = pawn.MaxHealth > 0 ? pawn.MaxHealth : 100;
        if (pawn.Health >= maxHp)
            return HookResult.Continue;

        int healed = Math.Min(pawn.Health + total, maxHp) - pawn.Health;
        pawn.Health += healed;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        attacker.PrintToChat($" {CC.Orchid}{Core.ChatPrefix}{CC.Default} {Core.Localizer["vip.killheal", healed, string.Join(", ", reasons)]}");
        return HookResult.Continue;
    }

    private static int? GetInt(Dictionary<string, JsonElement> cfg, string key) =>
        cfg.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out int v) ? v : null;
}
