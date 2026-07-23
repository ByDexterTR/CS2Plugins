using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace VIPCore;

public abstract class VipModule
{
    protected VIPCore Core = null!;
    internal void Bind(VIPCore core) => Core = core;

    public abstract string Name { get; }
    public abstract string DisplayName { get; }

    public virtual VipFeatureType MenuType => VipFeatureType.Toggle;
    public virtual bool ShowInMenu => true;
    public virtual List<VipFeatureOption> SelectOptions(CCSPlayerController player) => new();
    public virtual List<VipFeatureOption> SelectCategories(CCSPlayerController player) => new();
    public virtual List<VipFeatureOption> CategoryOptions(CCSPlayerController player, string category) => new();

    public abstract void OnLoad();
    public virtual void OnUnload() { }
    public virtual void OnSelect(CCSPlayerController player, string value) { }

    private int[]? _limitUsed;
    private int _limitRound = int.MinValue;

    private void SyncLimitRound()
    {
        int round = Core.RoundNumber;
        if (_limitRound == round)
            return;
        _limitRound = round;
        (_limitUsed ??= new int[64]).AsSpan().Clear();
    }

    protected bool LimitReached(int slot, int limit)
    {
        if (limit <= 0 || slot < 0 || slot >= 64)
            return false;
        SyncLimitRound();
        return _limitUsed![slot] >= limit;
    }

    protected void LimitUse(int slot)
    {
        if (slot < 0 || slot >= 64)
            return;
        SyncLimitRound();
        _limitUsed![slot]++;
    }

    protected bool IsCurrent => ReferenceEquals(Core, VIPCore.Current);

    protected bool Active(CCSPlayerController? player) => IsCurrent && player != null && Core.IsActive(player, Name);
    protected bool Granted(CCSPlayerController? player) => player != null && Core.IsGranted(player, Name);
    protected string Setting(CCSPlayerController player) => Core.GetSetting(player.SteamID, Name);
    protected string CategorySetting(CCSPlayerController player, string category) => Core.GetSetting(player.SteamID, $"{Name}@{category}");
    protected T? GroupValue<T>(CCSPlayerController player) => Core.GetGroupValue<T>(player, Name);

    public static bool TryGetButtons(CCSPlayerController? p, out PlayerButtons buttons)
    {
        buttons = 0;

        var pawn = p?.Pawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.MovementServices == null)
            return false;

        try
        {
            buttons = (PlayerButtons)pawn.MovementServices.Buttons.ButtonStates[0];
            return true;
        }
        catch
        {
            return false;
        }
    }

    protected static bool IsAlive(CCSPlayerController? p) =>
        p != null && p.IsValid && p.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;

    protected static string? ActiveWeaponName(CCSPlayerController p)
    {
        var weapon = p.PlayerPawn.Value?.WeaponServices?.ActiveWeapon?.Value;
        if (weapon == null || !weapon.IsValid || string.IsNullOrEmpty(weapon.DesignerName))
            return null;

        int itemDef = 0;
        try { itemDef = weapon.AttributeManager.Item.ItemDefinitionIndex; }
        catch { }

        return WeaponUtil.NormalizeWeaponName(weapon.DesignerName, itemDef);
    }

    protected static bool HasWeapon(CCSPlayerController? p, string designerName) =>
        CountWeapon(p, designerName) > 0;

    protected static int CountWeapon(CCSPlayerController? p, string designerName)
    {
        var weapons = p?.PlayerPawn.Value?.WeaponServices?.MyWeapons;
        if (weapons == null)
            return 0;

        int count = 0;
        foreach (var handle in weapons)
        {
            var weapon = handle.Value;
            if (weapon != null && weapon.IsValid && weapon.DesignerName == designerName)
                count++;
        }
        return count;
    }

    protected static CCSPlayerController? PawnController(CEntityInstance? ent)
    {
        if (ent == null || !ent.IsValid || ent.DesignerName != "player")
            return null;
        return ent.As<CCSPlayerPawn>().Controller.Value?.As<CCSPlayerController>();
    }
}
