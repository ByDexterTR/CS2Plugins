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

    public static string NormalizeWeaponName(string designerName, int itemDef)
    {
        if (designerName.Contains("bayonet") || designerName.Contains("knife"))
            return "weapon_knife";

        return (designerName, itemDef) switch
        {
            ("weapon_m4a1", 60) => "weapon_m4a1_silencer",
            ("weapon_hkp2000", 61) => "weapon_usp_silencer",
            ("weapon_deagle", 64) => "weapon_revolver",
            ("weapon_mp7", 23) => "weapon_mp5sd",
            _ => designerName
        };
    }
}
