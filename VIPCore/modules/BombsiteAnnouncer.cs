using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class BombsiteAnnouncer : VipModule
{
    private class Cfg
    {
        public string ImgA { get; set; } = "https://raw.githubusercontent.com/itsAudioo/CS2BombsiteAnnouncer/refs/heads/main/img/Site-A.png";
        public string ImgB { get; set; } = "https://raw.githubusercontent.com/itsAudioo/CS2BombsiteAnnouncer/refs/heads/main/img/Site-B.png";
        public float Duration { get; set; } = 5f;
    }

    private string? _site;
    private float _plantTime;
    private float _hideAt;

    public override string Name => "BombsiteAnnouncer";
    public override string DisplayName => Core.Localizer["vip.module.bombsiteannouncer"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventBombPlanted>(OnPlanted);
        Core.RegisterEventHandler<EventRoundStart>((_, __) =>
        {
            _site = null;
            return HookResult.Continue;
        });
        Core.RegisterListener<OnTick>(OnTick);
    }

    private HookResult OnPlanted(EventBombPlanted ev, GameEventInfo info)
    {
        Server.NextFrame(() =>
        {
            var c4 = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault(c => c != null && c.IsValid);
            if (c4 == null)
                return;

            _site = c4.BombSite == 1 ? "B" : "A";
            _plantTime = Server.CurrentTime;
            _hideAt = _plantTime;

            foreach (var player in Utilities.GetPlayers())
            {
                if (player == null || !player.IsValid || player.IsBot || player.Team != CsTeam.CounterTerrorist || !Active(player))
                    continue;

                float duration = (GroupValue<Cfg>(player) ?? new Cfg()).Duration;
                _hideAt = Math.Max(_hideAt, _plantTime + duration);
                player.PrintToChat($" {CC.Orchid}{Core.ChatPrefix}{CC.Default} {Core.Localizer["vip.bombsite.planted", _site]}");
            }
        });

        return HookResult.Continue;
    }

    private void OnTick()
    {
        if (_site == null)
            return;

        if (Server.CurrentTime > _hideAt)
        {
            _site = null;
            return;
        }

        foreach (var player in Core.Players)
        {
            if (player == null || !player.IsValid || player.IsBot || player.Team != CsTeam.CounterTerrorist || !Active(player))
                continue;

            var cfg = GroupValue<Cfg>(player) ?? new Cfg();
            if (Server.CurrentTime - _plantTime > cfg.Duration)
                continue;

            string img = _site == "B" ? cfg.ImgB : cfg.ImgA;
            player.PrintToCenterHtml($"<img src='{img}'>");
        }
    }
}
