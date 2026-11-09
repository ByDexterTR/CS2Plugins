using CounterStrikeSharp.API.Modules.Cvars;

namespace VIPCore;

public static class ServerConVar
{
    private static readonly Dictionary<string, ConVar?> Cache = new();

    public static bool Enabled(string name)
    {
        if (!Cache.TryGetValue(name, out var convar))
            Cache[name] = convar = ConVar.Find(name);

        return convar?.GetPrimitiveValue<bool>() == true;
    }

    public static string Value(string name) => Enabled(name) ? "1" : "0";
}
