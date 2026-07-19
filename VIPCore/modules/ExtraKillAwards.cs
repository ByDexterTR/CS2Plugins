using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace VIPCore;

public class ExtraKillAwards : VipModule
{
    public override string Name => "ExtraKillAwards";
    public override string DisplayName => Core.Localizer["vip.module.extrakillawards"];

    public override void OnLoad() => Core.RegisterEventHandler<EventPlayerDeath>(OnDeath);

    private HookResult OnDeath(EventPlayerDeath ev, GameEventInfo info)
    {
        var attacker = ev.Attacker;
        var victim = ev.Userid;
        if (attacker == null || !attacker.IsValid || attacker.IsBot || victim == null || !victim.IsValid)
            return HookResult.Continue;

        bool ffa = ConVar.Find("mp_teammates_are_enemies")?.GetPrimitiveValue<bool>() ?? false;
        if (attacker.Slot == victim.Slot || (!ffa && attacker.Team == victim.Team) || !Active(attacker))
            return HookResult.Continue;

        var cfg = GroupValue<Dictionary<string, JsonElement>>(attacker);
        if (cfg == null || cfg.Count == 0)
            return HookResult.Continue;

        int total = 0;
        var reasons = new List<string>();

        void Try(string key, bool condition)
        {
            if (condition && GetInt(cfg, key) is int money and > 0)
            {
                total += money;
                reasons.Add(key);
            }
        }

        Try("headshot", ev.Headshot);
        Try("noscope", ev.Noscope);
        Try("inair", ev.Attackerinair);
        Try("blind", ev.Attackerblind);

        string weapon = ev.Weapon.Contains("knife") || ev.Weapon.Contains("bayonet") ? "knife" : ev.Weapon;
        string weaponKey = "weapon_" + weapon;
        Try(weaponKey, true);

        if (cfg.TryGetValue("distance", out var dist) && dist.ValueKind == JsonValueKind.Object)
        {
            float unit = dist.TryGetProperty("unit", out var u) && u.TryGetSingle(out float uv) ? uv : 0f;
            int money = dist.TryGetProperty("money", out var m) && m.TryGetInt32(out int mv) ? mv : 0;

            var aPos = attacker.PlayerPawn.Value?.AbsOrigin;
            var vPos = victim.PlayerPawn.Value?.AbsOrigin;
            if (unit > 0 && money > 0 && aPos != null && vPos != null)
            {
                int steps = (int)(TrailBeam.Distance(aPos, vPos) / unit);
                if (steps > 0)
                {
                    total += money * steps;
                    reasons.Add("distance");
                }
            }
        }

        if (total <= 0)
            return HookResult.Continue;

        var moneyServices = attacker.InGameMoneyServices;
        if (moneyServices == null)
            return HookResult.Continue;

        int maxMoney = ConVar.Find("mp_maxmoney")?.GetPrimitiveValue<int>() ?? 16000;
        moneyServices.Account = Math.Min(moneyServices.Account + total, maxMoney);
        Utilities.SetStateChanged(attacker, "CCSPlayerController", "m_pInGameMoneyServices");

        attacker.PrintToChat($" {CC.Orchid}{Core.ChatPrefix}{CC.Default} {Core.Localizer["vip.killaward", total, string.Join(", ", reasons)]}");
        return HookResult.Continue;
    }

    private static int? GetInt(Dictionary<string, JsonElement> cfg, string key) =>
        cfg.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out int v) ? v : null;
}
