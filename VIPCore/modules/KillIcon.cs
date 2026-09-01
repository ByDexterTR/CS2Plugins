using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class KillIcon : VipModule
{
    private class Cfg
    {
        public string Headshot { get; set; } = "";
        public string Penetrated { get; set; } = "";

        [JsonPropertyName("noscope")]
        public string NoScope { get; set; } = "";

        [JsonPropertyName("throughsmoke")]
        public string ThroughSmoke { get; set; } = "";

        [JsonPropertyName("blindkill")]
        public string BlindKill { get; set; } = "";

        [JsonPropertyName("assistflash")]
        public string AssistFlash { get; set; } = "";

        [JsonPropertyName("jumpkill")]
        public string JumpKill { get; set; } = "";

        public string Dominated { get; set; } = "";

        [JsonPropertyName("squadwipe")]
        public string SquadWipe { get; set; } = "";

        public Dictionary<string, string> Weapons { get; set; } = new();
    }

    public override string Name => "KillIcon";
    public override string DisplayName => Core.Localizer["vip.module.killicon"];

    public override void OnLoad() =>
        Core.RegisterEventHandler<EventPlayerDeath>(OnDeath, HookMode.Pre);

    private HookResult OnDeath(EventPlayerDeath ev, GameEventInfo info)
    {
        var attacker = ev.Attacker;
        var victim = ev.Userid;

        if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid)
            return HookResult.Continue;
        if (attacker.Slot == victim.Slot || !Active(attacker))
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(attacker);
        if (cfg == null)
            return HookResult.Continue;

        string icon = Pick(cfg, ev, attacker, victim);
        if (icon.Length > 0)
            ev.Weapon = icon;

        return HookResult.Continue;
    }

    private string Pick(Cfg cfg, EventPlayerDeath ev, CCSPlayerController attacker, CCSPlayerController victim)
    {
        if (cfg.SquadWipe.Length > 0 && SquadWiped(victim))
            return cfg.SquadWipe;

        if (cfg.Dominated.Length > 0 && ev.Dominated > 0)
            return cfg.Dominated;

        if (cfg.JumpKill.Length > 0 && InAir(attacker))
            return cfg.JumpKill;

        if (cfg.BlindKill.Length > 0 && ev.Attackerblind)
            return cfg.BlindKill;

        if (cfg.AssistFlash.Length > 0 && ev.Assistedflash)
            return cfg.AssistFlash;

        if (cfg.NoScope.Length > 0 && ev.Noscope)
            return cfg.NoScope;

        if (cfg.ThroughSmoke.Length > 0 && ev.Thrusmoke)
            return cfg.ThroughSmoke;

        if (cfg.Penetrated.Length > 0 && ev.Penetrated > 0)
            return cfg.Penetrated;

        if (cfg.Headshot.Length > 0 && ev.Headshot)
            return cfg.Headshot;

        if (cfg.Weapons.Count == 0)
            return "";

        string weapon = ev.Weapon;
        if (weapon.Length == 0)
            return "";

        if (cfg.Weapons.TryGetValue(weapon, out var byName) && byName.Length > 0)
            return byName;

        return cfg.Weapons.TryGetValue("weapon_" + weapon, out var prefixed) ? prefixed : "";
    }

    private static bool InAir(CCSPlayerController attacker)
    {
        var pawn = attacker.PlayerPawn.Value;
        return pawn != null && pawn.IsValid && (pawn.Flags & (1u << 0)) == 0;
    }

    private bool SquadWiped(CCSPlayerController victim)
    {
        foreach (var mate in Core.Players)
        {
            if (mate == null || !mate.IsValid || mate.Slot == victim.Slot)
                continue;
            if (mate.Team == victim.Team && IsAlive(mate))
                return false;
        }

        return true;
    }
}
