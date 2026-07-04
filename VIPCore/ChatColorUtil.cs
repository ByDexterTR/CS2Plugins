using System.Text;

namespace VIPCore;

public static class ChatColorUtil
{
    private static readonly Dictionary<string, char> Colors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["default"] = '\x01', ["white"] = '\x01', ["darkred"] = '\x02', ["green"] = '\x04',
        ["lightgreen"] = '\x05', ["lime"] = '\x06', ["red"] = '\x07', ["grey"] = '\x08',
        ["yellow"] = '\x09', ["bluegrey"] = '\x0A', ["blue"] = '\x0B', ["darkblue"] = '\x0C',
        ["purple"] = '\x0E', ["orchid"] = '\x0E', ["lightred"] = '\x0F', ["gold"] = '\x10'
    };

    private static readonly Dictionary<char, string> Html = new()
    {
        ['\x01'] = "#FFFFFF", ['\x02'] = "#8B0000", ['\x04'] = "#4CD950", ['\x05'] = "#99FF99",
        ['\x06'] = "#00FF00", ['\x07'] = "#FF4040", ['\x08'] = "#9A9A9A", ['\x09'] = "#FFFF00",
        ['\x0A'] = "#6699CC", ['\x0B'] = "#4488FF", ['\x0C'] = "#3355CD", ['\x0E'] = "#DA70D6",
        ['\x0F'] = "#FF6666", ['\x10'] = "#FFD700"
    };

    public static char Char(string? name) =>
        name != null && Colors.TryGetValue(name, out var c) ? c : '\x01';

    public static string Process(string text)
    {
        foreach (var (name, value) in Colors)
            text = text.Replace("{" + name + "}", value.ToString(), StringComparison.OrdinalIgnoreCase);
        return text;
    }

    public static string ToHtml(string text)
    {
        var builder = new StringBuilder();
        bool open = false;

        foreach (char c in text)
        {
            if (Html.TryGetValue(c, out var hex))
            {
                if (open)
                    builder.Append("</font>");
                builder.Append("<font color='").Append(hex).Append("'>");
                open = true;
            }
            else
            {
                builder.Append(c);
            }
        }

        if (open)
            builder.Append("</font>");

        return builder.ToString();
    }
}
