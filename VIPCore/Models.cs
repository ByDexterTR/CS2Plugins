using System.Text.Json.Serialization;

namespace VIPCore;

public enum VipFeatureType
{
    Toggle,
    Select
}

public record VipFeatureOption(string Display, string Value);

public class VipEntry
{
    public string Group { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long Expires { get; set; }
}

public class PeriodConfig
{
    public string Start { get; set; } = "";
    public string End { get; set; } = "";
    public string Timezone { get; set; } = "";
}

public class PeriodWindow
{
    public TimeSpan Start { get; init; }
    public TimeSpan End { get; init; }
    public TimeZoneInfo Zone { get; init; } = TimeZoneInfo.Local;

    public bool IsOpen(DateTimeOffset utcNow)
    {
        if (Start == End)
            return true;

        var local = TimeZoneInfo.ConvertTime(utcNow, Zone).TimeOfDay;
        return Start < End
            ? local >= Start && local < End
            : local >= Start || local < End;
    }

    public static PeriodWindow? Parse(string? start, string? end, string? timezone, out string? issue)
    {
        issue = null;

        if (!TimeSpan.TryParse(start, out var from) || from < TimeSpan.Zero || from >= TimeSpan.FromDays(1))
        {
            issue = $"Period icindeki \"start\" degeri okunamadi: \"{start}\". Ornek: \"23:00\".";
            return null;
        }

        if (!TimeSpan.TryParse(end, out var to) || to < TimeSpan.Zero || to >= TimeSpan.FromDays(1))
        {
            issue = $"Period icindeki \"end\" degeri okunamadi: \"{end}\". Ornek: \"07:00\".";
            return null;
        }

        var zone = TimeZoneInfo.Local;
        if (!string.IsNullOrWhiteSpace(timezone))
        {
            try { zone = TimeZoneInfo.FindSystemTimeZoneById(timezone); }
            catch
            {
                issue = $"Period icindeki \"timezone\" taninmadi: \"{timezone}\". Sunucu saati kullanilacak.";
            }
        }

        return new PeriodWindow { Start = from, End = to, Zone = zone };
    }
}
