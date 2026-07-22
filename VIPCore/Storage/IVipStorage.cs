namespace VIPCore;

public interface IVipStorage
{
    bool SupportsLiveRefresh { get; }

    void Init();

    Dictionary<ulong, VipEntry> LoadVips();
    Dictionary<ulong, Dictionary<string, string>> LoadSettings();

    VipEntry? LoadVip(ulong steamId);
    Dictionary<string, string>? LoadSettings(ulong steamId);

    void UpsertVip(ulong steamId, VipEntry entry);
    void DeleteVip(ulong steamId);

    void UpsertSetting(ulong steamId, string feature, string value);
    void DeleteSetting(ulong steamId, string feature);
}
