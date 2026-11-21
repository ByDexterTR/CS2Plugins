using System.Globalization;
using CounterStrikeSharp.API.Modules.Cvars;

namespace VIPCore;

public static class ServerConVar
{
    private static readonly Dictionary<string, Func<string>?> Readers = new();

    public static bool Enabled(string name) => Value(name) != "0";

    public static string Value(string name)
    {
        if (!Readers.TryGetValue(name, out var reader))
            Readers[name] = reader = Create(name);

        return reader?.Invoke() ?? "0";
    }

    private static Func<string>? Create(string name)
    {
        try
        {
            var flag = ConVar<bool>.Find(name);
            if (flag != null)
            {
                _ = flag.Value;
                return () => flag.Value ? "1" : "0";
            }
        }
        catch (InvalidOperationException) { }

        try
        {
            var number = ConVar<int>.Find(name);
            if (number != null)
            {
                _ = number.Value;
                return () => number.Value.ToString(CultureInfo.InvariantCulture);
            }
        }
        catch (InvalidOperationException) { }

        return null;
    }
}
