using System.Collections.Concurrent;
using System.Diagnostics;

namespace VIPCore;

public static class PerfLog
{
    private const double SpikeMs = 0.5;
    private const double SpikeCooldown = 5.0;
    private const int MaxSamples = 8192;
    private const int TopRows = 25;

    private static readonly string SessionId = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
    private static readonly object WriteLock = new();
    private static readonly ConcurrentDictionary<Delegate, string> LabelCache = new();
    private static readonly ConcurrentDictionary<string, Aggregate> Aggregates = new();

    private static StreamWriter? _writer;
    private static string _folder = "";
    private static DateTime _windowStart = DateTime.Now;

    public static bool Enabled;
    public static Func<bool>? Idle;

    private sealed class Aggregate
    {
        public double TotalMs;
        public double MaxMs;
        public int Count;
        public long Bytes;
        public double LastSpike;
        public readonly List<double> Raw = new();

        public void Add(double ms, long bytes)
        {
            TotalMs += ms;
            Bytes += bytes;
            Count++;
            if (ms > MaxMs)
                MaxMs = ms;
            if (Raw.Count < MaxSamples)
                Raw.Add(ms);
        }

        public double Percentile(double fraction)
        {
            if (Raw.Count == 0)
                return 0;

            Raw.Sort();
            int index = (int)Math.Ceiling(Raw.Count * fraction) - 1;
            if (index < 0)
                index = 0;
            if (index >= Raw.Count)
                index = Raw.Count - 1;
            return Raw[index];
        }

        public void Reset()
        {
            TotalMs = 0;
            MaxMs = 0;
            Count = 0;
            Bytes = 0;
            Raw.Clear();
        }
    }

    public static void Install(string moduleDirectory, bool enabled, Func<bool>? idle)
    {
        _folder = Path.Combine(moduleDirectory, "logs");
        Idle = idle;
        Enabled = enabled;
        if (enabled)
            Info($"PerfLog enabled (VIPCore {VIPCore.Current?.ModuleVersion})");
    }

    public static long Start() => Enabled ? Stopwatch.GetTimestamp() : 0L;

    public static long Alloc() => Enabled ? GC.GetAllocatedBytesForCurrentThread() : 0L;

    public static void Info(string message)
    {
        if (!Enabled)
            return;

        Write(message);
    }

    public static void End(string label, long start, double thresholdMs = 1.0)
    {
        if (start == 0 || !Enabled)
            return;

        double ms = Elapsed(start);
        if (ms < thresholdMs)
            return;

        Write($"{label} took {ms:F2}ms");
    }

    public static void Sample(long start, long alloc, string kind, Delegate handler)
    {
        if (start == 0 || !Enabled)
            return;

        Add(Label(kind, handler), Elapsed(start), GC.GetAllocatedBytesForCurrentThread() - alloc);
    }

    public static void Sample(long start, long alloc, string label)
    {
        if (start == 0 || !Enabled)
            return;

        Add(label, Elapsed(start), GC.GetAllocatedBytesForCurrentThread() - alloc);
    }

    internal static List<(string Label, int Count, double TotalMs, double MaxMs, double P95, long Bytes)> Drain()
    {
        var rows = new List<(string, int, double, double, double, long)>();
        foreach (var (label, agg) in Aggregates)
        {
            lock (agg)
            {
                if (agg.Count > 0)
                    rows.Add((label, agg.Count, agg.TotalMs, agg.MaxMs, agg.Percentile(0.95), agg.Bytes));
                agg.Reset();
            }
        }
        return rows;
    }

    public static void Report()
    {
        if (!Enabled || Aggregates.IsEmpty)
            return;

        var rows = new List<(string Label, int Count, double Total, double Avg, double P95, double Max, long Bytes)>();

        foreach (var (label, agg) in Aggregates)
        {
            lock (agg)
            {
                if (agg.Count == 0)
                    continue;

                rows.Add((label, agg.Count, agg.TotalMs, agg.TotalMs / agg.Count, agg.Percentile(0.95), agg.MaxMs, agg.Bytes));
                agg.Reset();
            }
        }

        double window = (DateTime.Now - _windowStart).TotalSeconds;
        _windowStart = DateTime.Now;

        if (rows.Count == 0)
            return;

        rows.Sort((a, b) => b.Total.CompareTo(a.Total));

        double grand = 0;
        foreach (var row in rows)
            grand += row.Total;

        lock (WriteLock)
        {
            Write($"===== summary last {window:F0}s / total {grand:F1}ms =====");
            int shown = Math.Min(rows.Count, TopRows);
            for (int i = 0; i < shown; i++)
            {
                var row = rows[i];
                Write($"{row.Label,-44} calls={row.Count,-7} avg={row.Avg:F3}ms p95={row.P95:F3}ms max={row.Max:F2}ms total={row.Total:F1}ms alloc={row.Bytes / Math.Max(row.Count, 1)}B");
            }
        }
    }

    private static string Label(string kind, Delegate handler) =>
        LabelCache.GetOrAdd(handler, static (h, k) => $"{k} {Describe(h)}", kind);

    public static string Describe(Delegate handler)
    {
        var type = handler.Method.DeclaringType;
        while (type is { DeclaringType: not null } && type.Name.StartsWith('<'))
            type = type.DeclaringType;
        return $"{type?.Name}.{handler.Method.Name}";
    }

    private static double Elapsed(long start) =>
        (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;

    private static void Add(string label, double ms, long bytes)
    {
        if (Idle?.Invoke() == true)
            return;

        var agg = Aggregates.GetOrAdd(label, static _ => new Aggregate());

        lock (agg)
        {
            agg.Add(ms, bytes);

            if (ms < SpikeMs)
                return;

            double now = (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency;
            if (now - agg.LastSpike < SpikeCooldown)
                return;

            agg.LastSpike = now;
            Write($"spike {label} {ms:F2}ms");
        }
    }

    private static void Write(string message)
    {
        lock (WriteLock)
        {
            try
            {
                if (_writer == null)
                {
                    Directory.CreateDirectory(_folder);
                    _writer = new StreamWriter(Path.Combine(_folder, $"perf_{SessionId}.txt"), append: true, System.Text.Encoding.UTF8) { AutoFlush = true };
                }

                _writer.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [PERF] {message}");
            }
            catch
            {
            }
        }
    }

    public static void Close()
    {
        lock (WriteLock)
        {
            if (_writer == null)
                return;

            try { _writer.Dispose(); }
            catch { }
            _writer = null;
        }

        Aggregates.Clear();
        LabelCache.Clear();
    }
}
