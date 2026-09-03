using System.Drawing;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class Soul : VipModule
{
    private class Cfg
    {
        [JsonPropertyName("respawn_time")]
        public float RespawnTime { get; set; } = 5f;
        [JsonPropertyName("steal_time")]
        public float StealTime { get; set; } = 10f;
        public bool Steal { get; set; } = true;
        public int Limit { get; set; } = 1;
        public float Radius { get; set; } = 100f;
        public float Duration { get; set; } = 25f;
        public float Size { get; set; } = 22f;
        public float Speed { get; set; } = 45f;
        public float Height { get; set; } = 45f;
        [JsonPropertyName("color_t")]
        public string ColorT { get; set; } = "#FF8000";
        [JsonPropertyName("color_ct")]
        public string ColorCt { get; set; } = "#00A0FF";
        [JsonPropertyName("color_steal")]
        public string ColorSteal { get; set; } = "#FF0033";
    }

    private class Ghost
    {
        public int UserId;
        public CsTeam Team;
        public Vector Origin = new();
        public Vector Ground = new();
        public string ColorName = "";
        public float ExpireAt;
        public float HoldTime;
        public int HolderUserId = -1;
        public bool Stealing;
        public Color Applied;
        public readonly List<CBeam> Beams = new();
    }

    private const int MaxSouls = 12;

    private static readonly Cfg DefaultCfg = new();
    private static readonly float[][] Vertices = BuildVertices();
    private static readonly (int A, int B)[] Edges = BuildEdges(Vertices);

    private readonly Dictionary<int, Ghost> _souls = new();
    private readonly Dictionary<int, int> _used = new();
    private readonly HashSet<int> _blocked = new();
    private readonly List<int> _finished = new();

    public override string Name => "Soul";
    public override string DisplayName => Core.Localizer["vip.module.soul"];

    public override void OnLoad()
    {
        Core.RegisterEventHandler<EventPlayerDeath>(OnDeath);
        Core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) =>
        {
            RemoveSoul(ev.Userid?.UserId ?? -1);
            return HookResult.Continue;
        });
        Core.RegisterEventHandler<EventRoundStart>((_, __) =>
        {
            ClearSouls();
            _used.Clear();
            _blocked.Clear();
            return HookResult.Continue;
        });
        Core.RegisterEventHandler<EventRoundEnd>((_, __) => { ClearSouls(); return HookResult.Continue; });
        Core.HookMapEnd(ClearSouls);
        Core.HookTick(OnTick, 2);
    }

    public override void OnUnload() => ClearSouls();

    private HookResult OnDeath(EventPlayerDeath ev, GameEventInfo info)
    {
        var victim = ev.Userid;
        if (victim == null || !victim.IsValid)
            return HookResult.Continue;

        if (victim.IsBot || !Active(victim))
            return HookResult.Continue;

        if (victim.Team != CsTeam.Terrorist && victim.Team != CsTeam.CounterTerrorist)
            return HookResult.Continue;

        var cfg = GroupValue<Cfg>(victim) ?? DefaultCfg;
        int userId = victim.UserId ?? -1;
        if (userId < 0 || _blocked.Contains(userId))
            return HookResult.Continue;

        if (cfg.Limit > 0 && _used.GetValueOrDefault(userId) >= cfg.Limit)
            return HookResult.Continue;

        var origin = victim.PlayerPawn.Value?.AbsOrigin;
        if (origin == null)
            return HookResult.Continue;

        if (!Create(victim, origin, cfg))
            return HookResult.Continue;

        if (!victim.IsBot)
            victim.PrintToChat($" {CC.Orchid}{Core.ChatPrefix}{CC.Default} {Core.Localizer["vip.soul.hint"]}");

        return HookResult.Continue;
    }

    private bool Create(CCSPlayerController player, Vector origin, Cfg cfg)
    {
        int userId = player.UserId ?? -1;
        if (userId < 0 || _souls.Count >= MaxSouls)
            return false;

        RemoveSoul(userId);

        var soul = new Ghost
        {
            UserId = userId,
            Team = player.Team,
            ColorName = player.Team == CsTeam.CounterTerrorist ? cfg.ColorCt : cfg.ColorT,
            ExpireAt = cfg.Duration > 0f ? Server.CurrentTime + cfg.Duration : 0f
        };

        soul.Ground.X = origin.X;
        soul.Ground.Y = origin.Y;
        soul.Ground.Z = origin.Z;

        soul.Origin.X = origin.X;
        soul.Origin.Y = origin.Y;
        soul.Origin.Z = origin.Z + cfg.Height;

        _souls[userId] = soul;
        return true;
    }

    private void OnTick()
    {
        if (_souls.Count == 0)
            return;

        _finished.Clear();

        foreach (var (userId, soul) in _souls)
        {
            var owner = Utilities.GetPlayerFromUserid(userId);
            if (owner == null || !owner.IsValid || IsAlive(owner) || !Active(owner))
            {
                _finished.Add(userId);
                continue;
            }

            if (soul.ExpireAt > 0f && Server.CurrentTime >= soul.ExpireAt)
            {
                _finished.Add(userId);
                continue;
            }

            var cfg = GroupValue<Cfg>(owner) ?? DefaultCfg;
            var holder = FindHolder(soul, cfg, out bool stealing);

            if (holder == null)
            {
                StopBar(soul);
                soul.Stealing = false;
                Draw(soul, cfg);
                continue;
            }

            float need = Math.Max(stealing ? cfg.StealTime : cfg.RespawnTime, 0.1f);
            int holderId = holder.UserId ?? -1;

            if (soul.HolderUserId != holderId || soul.Stealing != stealing)
            {
                StopBar(soul);
                soul.HolderUserId = holderId;
                soul.Stealing = stealing;
                soul.HoldTime = 0f;
                StartBar(holder, need);
            }

            soul.HoldTime += Server.TickInterval * 2f;
            Draw(soul, cfg);

            if (soul.HoldTime < need)
                continue;

            StopBar(soul);

            if (stealing)
                Steal(owner, holder, soul);
            else
                Revive(owner, holder, soul);

            _finished.Add(userId);
        }

        foreach (int userId in _finished)
            RemoveSoul(userId);
    }

    private CCSPlayerController? FindHolder(Ghost soul, Cfg cfg, out bool stealing)
    {
        stealing = false;

        var mate = Nearest(soul, cfg, true);
        if (mate != null)
            return mate;

        if (!cfg.Steal)
            return null;

        var thief = Nearest(soul, cfg, false);
        stealing = thief != null;
        return thief;
    }

    private CCSPlayerController? Nearest(Ghost soul, Cfg cfg, bool teammates)
    {
        CCSPlayerController? best = null;
        float bestDistance = float.MaxValue;

        foreach (var player in Core.Players)
        {
            if (player == null || !player.IsValid || player.IsBot || !IsAlive(player))
                continue;

            if (player.UserId == soul.UserId)
                continue;

            if (teammates != (player.Team == soul.Team))
                continue;

            if (!TryGetButtons(player, out var buttons) || (buttons & PlayerButtons.Use) == 0)
                continue;

            var origin = player.PlayerPawn.Value?.AbsOrigin;
            if (origin == null)
                continue;

            float distance = TrailBeam.Distance(soul.Ground, origin);
            if (distance > cfg.Radius || distance >= bestDistance)
                continue;

            best = player;
            bestDistance = distance;
        }

        return best;
    }

    private static void StartBar(CCSPlayerController reviver, float seconds)
    {
        var pawn = reviver.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        pawn.ProgressBarStartTime = Server.CurrentTime;
        pawn.ProgressBarDuration = Math.Max((int)MathF.Ceiling(seconds), 1);
        pawn.BlockingUseActionInProgress = CSPlayerBlockingUseAction_t.k_CSPlayerBlockingUseAction_MapLongUseEntity_Pickup;

        Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_flProgressBarStartTime");
        Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_iProgressBarDuration");
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_iBlockingUseActionInProgress");
    }

    private void StopBar(Ghost soul)
    {
        if (soul.HolderUserId < 0)
            return;

        var reviver = Utilities.GetPlayerFromUserid(soul.HolderUserId);
        soul.HolderUserId = -1;
        soul.HoldTime = 0f;

        var pawn = reviver?.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        pawn.ProgressBarStartTime = 0f;
        pawn.ProgressBarDuration = 0;
        pawn.BlockingUseActionInProgress = CSPlayerBlockingUseAction_t.k_CSPlayerBlockingUseAction_None;

        Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_flProgressBarStartTime");
        Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_iProgressBarDuration");
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_iBlockingUseActionInProgress");
    }

    private void Revive(CCSPlayerController owner, CCSPlayerController reviver, Ghost soul)
    {
        _used[soul.UserId] = _used.GetValueOrDefault(soul.UserId) + 1;

        int userId = soul.UserId;
        float x = soul.Ground.X, y = soul.Ground.Y, z = soul.Ground.Z;

        owner.Respawn();

        Server.NextFrame(() =>
        {
            var player = Utilities.GetPlayerFromUserid(userId);
            var pawn = player?.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                return;

            pawn.Teleport(new Vector(x, y, z), pawn.AbsRotation ?? new QAngle(), new Vector());
        });

        if (!owner.IsBot)
            owner.PrintToChat($" {CC.Orchid}{Core.ChatPrefix}{CC.Default} {Core.Localizer["vip.soul.revived", reviver.PlayerName]}");

        reviver.PrintToChat($" {CC.Orchid}{Core.ChatPrefix}{CC.Default} {Core.Localizer["vip.soul.saved", owner.PlayerName]}");
    }

    private void Steal(CCSPlayerController owner, CCSPlayerController thief, Ghost soul)
    {
        _blocked.Add(soul.UserId);

        if (!owner.IsBot)
            owner.PrintToChat($" {CC.Orchid}{Core.ChatPrefix}{CC.Default} {Core.Localizer["vip.soul.stolen", thief.PlayerName]}");

        thief.PrintToChat($" {CC.Orchid}{Core.ChatPrefix}{CC.Default} {Core.Localizer["vip.soul.stole", owner.PlayerName]}");
    }

    private void Draw(Ghost soul, Cfg cfg)
    {
        var color = TrailBeam.Resolve(soul.Stealing ? cfg.ColorSteal : soul.ColorName);

        if (soul.Beams.Count == 0 || soul.Beams[0] == null || !soul.Beams[0].IsValid)
        {
            DestroyBeams(soul);
            for (int i = 0; i < Edges.Length; i++)
            {
                var beam = Utilities.CreateEntityByName<CBeam>("beam");
                if (beam == null || !beam.IsValid)
                    continue;

                beam.Width = 1.5f;
                beam.Render = color;
                beam.DispatchSpawn();
                soul.Beams.Add(beam);
            }

            if (soul.Beams.Count == 0)
                return;

            soul.Applied = color;
        }

        float time = Server.CurrentTime;
        float angle = time * cfg.Speed * (MathF.PI / 180f);
        float sin = MathF.Sin(angle), cos = MathF.Cos(angle);
        float scale = Math.Max(cfg.Size, 4f);
        float bob = MathF.Sin(time * 2f) * 4f;

        Span<float> px = stackalloc float[12];
        Span<float> py = stackalloc float[12];
        Span<float> pz = stackalloc float[12];

        for (int i = 0; i < Vertices.Length; i++)
        {
            float vx = Vertices[i][0] * scale;
            float vy = Vertices[i][1] * scale;
            float vz = Vertices[i][2] * scale;

            px[i] = soul.Origin.X + vx * cos - vy * sin;
            py[i] = soul.Origin.Y + vx * sin + vy * cos;
            pz[i] = soul.Origin.Z + vz + bob;
        }

        for (int i = 0; i < soul.Beams.Count && i < Edges.Length; i++)
        {
            var beam = soul.Beams[i];
            if (beam == null || !beam.IsValid)
                continue;

            if (soul.Applied != color)
            {
                beam.Render = color;
                Utilities.SetStateChanged(beam, "CBaseModelEntity", "m_clrRender");
            }

            var (a, b) = Edges[i];
            beam.Teleport(new Vector(px[a], py[a], pz[a]), new QAngle(), new Vector());

            beam.EndPos.X = px[b];
            beam.EndPos.Y = py[b];
            beam.EndPos.Z = pz[b];
            Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");
        }

        soul.Applied = color;
    }

    private void ClearSouls()
    {
        foreach (int userId in _souls.Keys.ToList())
            RemoveSoul(userId);
    }

    private void RemoveSoul(int userId)
    {
        if (userId < 0 || !_souls.Remove(userId, out var soul))
            return;

        StopBar(soul);
        DestroyBeams(soul);
    }

    private static void DestroyBeams(Ghost soul)
    {
        foreach (var beam in soul.Beams)
            if (beam != null && beam.IsValid)
                beam.Remove();

        soul.Beams.Clear();
    }

    private static float[][] BuildVertices()
    {
        float phi = (1f + MathF.Sqrt(5f)) / 2f;
        float length = MathF.Sqrt(1f + phi * phi);

        var raw = new float[12][]
        {
            new[] { 0f, 1f, phi }, new[] { 0f, 1f, -phi }, new[] { 0f, -1f, phi }, new[] { 0f, -1f, -phi },
            new[] { 1f, phi, 0f }, new[] { 1f, -phi, 0f }, new[] { -1f, phi, 0f }, new[] { -1f, -phi, 0f },
            new[] { phi, 0f, 1f }, new[] { -phi, 0f, 1f }, new[] { phi, 0f, -1f }, new[] { -phi, 0f, -1f }
        };

        foreach (var vertex in raw)
            for (int i = 0; i < 3; i++)
                vertex[i] /= length;

        return raw;
    }

    private static (int, int)[] BuildEdges(float[][] vertices)
    {
        var edges = new List<(int, int)>();
        float shortest = float.MaxValue;

        for (int a = 0; a < vertices.Length; a++)
            for (int b = a + 1; b < vertices.Length; b++)
            {
                float distance = Squared(vertices[a], vertices[b]);
                if (distance < shortest)
                    shortest = distance;
            }

        for (int a = 0; a < vertices.Length; a++)
            for (int b = a + 1; b < vertices.Length; b++)
                if (Squared(vertices[a], vertices[b]) <= shortest * 1.1f)
                    edges.Add((a, b));

        return edges.ToArray();
    }

    private static float Squared(float[] a, float[] b)
    {
        float dx = a[0] - b[0], dy = a[1] - b[1], dz = a[2] - b[2];
        return dx * dx + dy * dy + dz * dz;
    }
}
