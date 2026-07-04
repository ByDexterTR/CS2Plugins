namespace VIPCore;

public static class WeaponUtil
{
    public static bool Matches(string configName, string? actual)
    {
        if (string.IsNullOrEmpty(actual))
            return false;

        if (configName.Equals("weapon_knife", StringComparison.OrdinalIgnoreCase))
            return actual.StartsWith("weapon_knife", StringComparison.OrdinalIgnoreCase);

        return configName.Equals(actual, StringComparison.OrdinalIgnoreCase);
    }

    public static bool MatchesAny(IEnumerable<string> names, string? actual) =>
        names.Any(n => Matches(n, actual));

    public static List<string> ParseCsv(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? new()
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
