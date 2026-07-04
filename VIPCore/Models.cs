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
    public long Expires { get; set; }
}
