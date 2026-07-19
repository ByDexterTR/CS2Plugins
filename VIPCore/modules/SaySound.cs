using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace VIPCore;

public class SaySound : VipModule
{
    private class Entry
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }

    private class Cfg
    {
        public float Cooldown { get; set; } = 2f;
        public List<Entry> Sounds { get; set; } = new();
    }

    private readonly float[] _lastPlay = new float[64];

    private Cfg GetCfg(CCSPlayerController player)
    {
        var cfg = GroupValue<Cfg>(player);
        if (cfg != null)
            return cfg;

        var list = GroupValue<List<Entry>>(player);
        return list != null ? new Cfg { Sounds = list } : new Cfg();
    }

    public override string Name => "SaySound";
    public override string DisplayName => Core.Localizer["vip.module.saysound"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player)
    {
        return GetCfg(player).Sounds.Where(e => e.Name.Length > 0 && e.Path.Length > 0)
            .Select(e => new VipFeatureOption(e.Name, e.Name)).ToList();
    }

    public override void OnLoad()
    {
        Core.AddCommandListener("say", (p, info) => Handle(p, info, false), HookMode.Post);
        Core.AddCommandListener("say_team", (p, info) => Handle(p, info, true), HookMode.Post);
        Core.RegisterListener<CounterStrikeSharp.API.Core.Listeners.OnMapStart>(_ => Array.Clear(_lastPlay));
    }

    private HookResult Handle(CCSPlayerController? player, CommandInfo info, bool teamOnly)
    {
        if (player == null || !player.IsValid || player.IsBot || !Active(player))
            return HookResult.Continue;

        string message = info.ArgString.Trim().Trim('"').Trim();
        if (message.Length == 0 || message.StartsWith('!') || message.StartsWith('/') || message.StartsWith('@'))
            return HookResult.Continue;

        var cfg = GetCfg(player);
        int slot = player.Slot;
        if (cfg.Cooldown > 0 && Server.CurrentTime - _lastPlay[slot] < cfg.Cooldown)
            return HookResult.Continue;

        var entry = cfg.Sounds.FirstOrDefault(e => e.Name == Setting(player));
        if (entry == null || entry.Path.Length == 0)
            return HookResult.Continue;

        _lastPlay[slot] = Server.CurrentTime;

        foreach (var target in Utilities.GetPlayers())
        {
            if (target == null || !target.IsValid || target.IsBot)
                continue;
            if (teamOnly && target.Team != player.Team)
                continue;

            target.ExecuteClientCommand($"play {entry.Path}");
        }

        return HookResult.Continue;
    }
}
