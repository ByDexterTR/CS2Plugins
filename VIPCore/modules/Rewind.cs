using Vector3 = System.Numerics.Vector3;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class Rewind : VipModule
{
    private class Cfg
    {
        public float Delay { get; set; } = 3f;
        public float Cooldown { get; set; } = 20f;
        public int Limit { get; set; } = 0;
        public string Color { get; set; } = "#4FC3F7";
        public float SoundVolume { get; set; } = 0.5f;
    }

    private class Mark
    {
        public Vector3 Origin;
        public float Yaw;
        public int ReturnTick;
        public uint Beam;
    }

    private static readonly Cfg DefaultCfg = new();
    private static readonly Vector3 HullMins = new(-16f, -16f, 0f);
    private static readonly Vector3 HullMaxs = new(16f, 16f, 72f);
    private static readonly float[] Heights = { 2f, 10f, 18f };

    private readonly Dictionary<int, Mark> _marks = new();
    private readonly Dictionary<int, float> _readyAt = new();

    public override string Name => "Rewind";
    public override string DisplayName => Core.Localizer["vip.module.rewind"];

    public override void OnLoad()
    {
        Core.HookPrecache(m => m.AddResource(TrailBeam.Sprite));
        Core.HookTick(OnTick);
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) => { Cancel(ev.Userid?.UserId ?? -1); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) =>
        {
            int id = ev.Userid?.UserId ?? -1;
            Cancel(id);
            _readyAt.Remove(id);
            return HookResult.Continue;
        });
        Core.RegisterEventHandler<EventRoundStart>((_, _) => { CancelAll(); _readyAt.Clear(); return HookResult.Continue; });
        Core.RegisterEventHandler<EventRoundEnd>((_, _) => { CancelAll(); return HookResult.Continue; });
    }

    public override void OnUnload() => CancelAll();

    public override bool OnCommand(CCSPlayerController player)
    {
        int id = player.UserId ?? -1;
        if (id < 0)
            return true;

        if (!Active(player) || !IsAlive(player))
            return true;

        if (_marks.TryGetValue(id, out var pending))
        {
            Return(player, pending);
            Cancel(id);
            return true;
        }

        var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;
        float now = Server.CurrentTime;
        if (_readyAt.TryGetValue(id, out float ready) && ready > now)
        {
            Say(player, Core.Localizer["vip.rewind.cooldown", (int)MathF.Ceiling(ready - now)]);
            return true;
        }

        if (LimitReached(player.Slot, cfg.Limit))
        {
            Say(player, Core.Localizer["vip.rewind.limit"]);
            return true;
        }

        var pawn = PlayerCache.Pawn(player);
        if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
            return true;

        var origin = pawn.AbsOrigin;
        var mark = new Mark
        {
            Origin = new Vector3(origin.X, origin.Y, origin.Z),
            Yaw = pawn.EyeAngles.Y,
            ReturnTick = Server.TickCount + (int)(Math.Max(cfg.Delay, 0.5f) * 64f)
        };

        mark.Beam = TrailBeam.Create(Core, new Vector(origin.X, origin.Y, origin.Z + 2f), new Vector(origin.X, origin.Y, origin.Z + 68f),
            TrailBeam.Resolve(cfg.Color), 1.5f, Math.Max(cfg.Delay, 0.5f) + 0.5f);

        _marks[id] = mark;
        _readyAt[id] = now + Math.Max(cfg.Cooldown, 0f);
        LimitUse(player.Slot);
        SoundUtil.EmitToPlayer(player, "UIPanorama.buttonclick", cfg.SoundVolume);
        Say(player, Core.Localizer["vip.rewind.marked", Math.Max(cfg.Delay, 0.5f)]);
        return true;
    }

    private void OnTick()
    {
        if (_marks.Count == 0)
            return;

        int tick = Server.TickCount;
        foreach (var (id, mark) in _marks.ToList())
        {
            if (mark.ReturnTick > tick)
                continue;

            var player = Utilities.GetPlayerFromUserid(id);
            if (player != null && player.IsValid && IsAlive(player))
                Return(player, mark);

            Cancel(id);
        }
    }

    private void Return(CCSPlayerController player, Mark mark)
    {
        var pawn = PlayerCache.Pawn(player);
        if (pawn == null || !pawn.IsValid)
            return;

        float? lift = Lift(pawn, mark.Origin);
        if (lift == null)
        {
            Say(player, Core.Localizer["vip.rewind.blocked"]);
            return;
        }

        pawn.Teleport(new Vector(mark.Origin.X, mark.Origin.Y, mark.Origin.Z + lift.Value), new QAngle(0f, mark.Yaw, 0f), new Vector(0f, 0f, 0f));
        var cfg = GroupValue<Cfg>(player) ?? DefaultCfg;
        SoundUtil.EmitToPlayer(player, "UIPanorama.buttonclick", cfg.SoundVolume);
    }

    private static float? Lift(CCSPlayerPawn pawn, Vector3 origin)
    {
        foreach (float height in Heights)
        {
            bool? free = NativeTrace.HullFree(pawn, origin + new Vector3(0f, 0f, height), HullMins, HullMaxs);
            if (free == null)
                return 0f;
            if (free == true)
                return height == Heights[0] ? 0f : height;
        }

        return null;
    }

    private void Cancel(int id)
    {
        if (id < 0 || !_marks.Remove(id, out var mark))
            return;

        var beam = new CHandle<CEnvBeam>(mark.Beam).Value;
        if (beam != null && beam.IsValid)
            beam.Remove();
    }

    private void CancelAll()
    {
        foreach (int id in _marks.Keys.ToList())
            Cancel(id);
    }

    private void Say(CCSPlayerController player, string text) =>
        player.PrintToChat($" {CC.Orchid}{Core.ChatPrefix}{CC.Default} {text}");
}
