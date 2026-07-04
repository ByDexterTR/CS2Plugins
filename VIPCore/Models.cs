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
