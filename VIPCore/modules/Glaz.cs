using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VIPCore;

public class Glaz : VipModule
{
    private readonly HashSet<int> _smokes = new();

    public override string Name => "Glaz";
    public override string DisplayName => Core.Localizer["vip.module.glaz"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventSmokegrenadeDetonate>(OnDetonate);
        Core.RegisterEventHandler<EventSmokegrenadeExpired>(OnExpired);
        Core.RegisterEventHandler<EventRoundStart>(OnRound);
        Core.RegisterListener<CheckTransmit>(OnCheckTransmit);
    }

    private HookResult OnDetonate(EventSmokegrenadeDetonate ev, GameEventInfo info)
    {
        _smokes.Add(ev.Entityid);
        return HookResult.Continue;
    }

    private HookResult OnExpired(EventSmokegrenadeExpired ev, GameEventInfo info)
    {
        _smokes.Remove(ev.Entityid);
        return HookResult.Continue;
    }

    private HookResult OnRound(EventRoundStart ev, GameEventInfo info)
    {
        _smokes.Clear();
        return HookResult.Continue;
    }

    private void OnCheckTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList infoList)
    {
        if (_smokes.Count == 0)
            return;

        foreach (var (info, player) in infoList)
        {
            if (player == null || !Active(player))
                continue;

            foreach (var index in _smokes)
            {
                var entity = Utilities.GetEntityFromIndex<CBaseEntity>(index);
                if (entity == null || !entity.IsValid)
                    continue;

                if (info.TransmitEntities.Contains(entity.Index))
                    info.TransmitEntities.Remove(entity.Index);
            }
        }
    }
}
