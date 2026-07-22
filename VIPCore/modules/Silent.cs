using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;

namespace VIPCore;

public class Silent : VipModule
{
    private class Cfg
    {
        public string OnlyWithWeapon { get; set; } = "";

        private List<string>? _allow;
        public List<string> Allow => _allow ??= WeaponUtil.ParseCsv(OnlyWithWeapon);
    }

    private static readonly Cfg DefaultCfg = new();

    private static readonly HashSet<uint> FootstepHashes = new()
    {
        3109879199, 70939233, 1342713723, 2722081556, 1909915699, 3193435079, 2300993891, 3847761506, 4084367249,
        2026488395, 2745524735, 2684452812, 2265091453, 1269567645, 520432428, 3266483468, 1346129716, 2061955732,
        2240518199, 2829617974, 1194677450, 1803111098, 3749333696, 29217150, 1692050905, 2207486967, 2633527058,
        3342414459, 988265811, 540697918, 1763490157, 3755338324, 3161194970, 3753692454, 3166948458, 3997353267,
        809738584, 3368720745, 3295206520, 3184465677, 123085364, 3123711576, 737696412, 1403457606, 1770765328,
        892882552, 3023174225, 4163677892, 3952104171, 4082928848, 1019414932, 1485322532, 1161855519, 1557420499,
        1163426340, 2708661994, 2479376962, 1404198078, 1194093029, 1253503839, 2189706910, 1218015996, 96240187,
        1116700262, 84876002, 1598540856, 2231399653,
        2551626319, 765706800, 2860219006, 2162652424, 117596568, 740474905, 1661204257, 3009312615, 1506215040,
        115843229, 3299941720, 1016523349, 2067683805, 4160462271, 1543118744, 585390608, 3802757032, 2302139631,
        2546391140, 144629619, 4152012084, 4113422219, 1627020521, 2899365092, 819435812, 3218103073, 961838155,
        1535891875, 1826799645, 3460445620, 1818046345, 3666896632, 3099536373, 1440734007, 1409986305, 1939055066,
        782454593, 4074593561, 1540837791, 3257325156,
        2800858936, 70011614, 3434104102, 1388885460, 413358161, 602548457, 859178236, 3057812547, 135189076,
        2790760284, 2448803175, 1690105992, 515548944, 1517575510, 1248619277, 1395892944, 1183624286, 1855038793,
        2892812682, 721782259, 2133235849, 2804393637, 4222899547, 1664187801, 2714245023, 2638406226, 3008782656,
        2070478448, 1247386781, 58439651, 3172583021, 893108375, 2594927130, 417910549, 931543849, 1543034,
        1664329401, 822973253, 3797950766, 4203793682, 870100484, 935062317, 1635413700, 2333790984, 1165397261,
        3984387113, 4045299578, 4085076160, 2236021746, 757978684, 1448154350, 2053595705, 1761772772, 1424056132,
        3806690332
    };

    public override string Name => "Silent";
    public override string DisplayName => Core.Localizer["vip.module.silent"];

    public override void OnLoad() => Core.HookUserMessage(208, OnSound, HookMode.Pre);

    public override void OnUnload() => Core.UnhookUserMessage(208, OnSound, HookMode.Pre);

    private HookResult OnSound(UserMessage um)
    {
        var hash = um.ReadUInt("soundevent_hash");
        if (!FootstepHashes.Contains(hash))
            return HookResult.Continue;

        int entityIndex = um.ReadInt("source_entity_index");
        var entity = Utilities.GetEntityFromIndex<CBaseEntity>(entityIndex);
        if (entity == null || !entity.IsValid || entity.DesignerName != "player")
            return HookResult.Continue;

        var player = entity.As<CCSPlayerPawn>().Controller.Value?.As<CCSPlayerController>();
        if (!Active(player))
            return HookResult.Continue;

        var allow = (GroupValue<Cfg>(player!) ?? DefaultCfg).Allow;
        if (allow.Count > 0 && !WeaponUtil.MatchesAny(allow, ActiveWeaponName(player!)))
            return HookResult.Continue;

        for (int i = um.Recipients.Count - 1; i >= 0; i--)
        {
            if (um.Recipients[i]?.Slot != player!.Slot)
                um.Recipients.RemoveAt(i);
        }

        return HookResult.Continue;
    }
}
