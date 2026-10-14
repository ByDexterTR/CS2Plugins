using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class Spy : VipModule
{
    private const string DefaultCtModel = "agents/models/ctm_sas/ctm_sas.vmdl";
    private const string DefaultTModel = "agents/models/tm_phoenix/tm_phoenix.vmdl";

    private static Spy? _instance;
    private readonly string?[] _original = new string?[64];

    public static bool Disguised(int slot) =>
        slot >= 0 && slot < 64 && _instance?._original[slot] != null;

    public override string Name => "Spy";
    public override string DisplayName => Core.Localizer["vip.module.spy"];

    public override void OnLoad()
    {
        _instance = this;
        DeathClock.Ensure(Core);
        Core.RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) =>
        {
            int slot = ev.Userid?.Slot ?? -1;
            if (slot >= 0 && slot < 64)
                _original[slot] = null;
            return HookResult.Continue;
        });
    }

    public override void OnUnload()
    {
        for (int slot = 0; slot < 64; slot++)
        {
            if (_original[slot] == null)
                continue;

            var pawn = PlayerCache.Pawn(Utilities.GetPlayerFromSlot(slot));
            if (pawn != null && pawn.IsValid)
                pawn.SetModel(_original[slot]!);
        }
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!Active(player))
            return HookResult.Continue;

        Server.NextFrame(() => Disguise(player));
        return HookResult.Continue;
    }

    private void Disguise(CCSPlayerController? player)
    {
        if (!Active(player) || !IsAlive(player))
            return;

        float wait = DeathClock.Wait(player!);
        if (wait > 0f)
        {
            Core.AddTimer(wait, () => Disguise(player), TimerFlags.STOP_ON_MAPCHANGE);
            return;
        }

        var pawn = PlayerCache.Pawn(player);
        if (pawn == null || !pawn.IsValid || pawn.CBodyComponent?.SceneNode == null)
            return;

        int slot = player.Slot;
        _original[slot] ??= pawn.CBodyComponent.SceneNode.GetSkeletonInstance().ModelState.ModelName;

        var enemies = Utilities.GetPlayers()
            .Where(p => p != null && p.IsValid && p.Team != player.Team && IsAlive(p))
            .ToList();

        string? model = null;
        if (enemies.Count > 0)
        {
            var target = enemies[Random.Shared.Next(enemies.Count)];
            var targetPawn = PlayerCache.Pawn(target);
            if (targetPawn?.CBodyComponent?.SceneNode != null)
                model = targetPawn.CBodyComponent.SceneNode.GetSkeletonInstance().ModelState.ModelName;
        }

        if (string.IsNullOrEmpty(model))
            model = player.Team == CsTeam.CounterTerrorist ? DefaultTModel : DefaultCtModel;

        pawn.SetModel(model);
    }
}
