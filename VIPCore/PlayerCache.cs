using System.Numerics;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;

namespace VIPCore;

public static unsafe class PlayerCache
{
    private static readonly int PawnHandle = Schema.GetSchemaOffset("CCSPlayerController", "m_hPlayerPawn");
    private static readonly int LifeStateOffset = Schema.GetSchemaOffset("CBaseEntity", "m_lifeState");
    private static readonly int HealthOffset = Schema.GetSchemaOffset("CBaseEntity", "m_iHealth");
    private static readonly int TeamOffset = Schema.GetSchemaOffset("CBaseEntity", "m_iTeamNum");
    private static readonly int FlagsOffset = Schema.GetSchemaOffset("CBaseEntity", "m_fFlags");
    private static readonly int VelocityOffset = Schema.GetSchemaOffset("CBaseEntity", "m_vecAbsVelocity");
    private static readonly int RenderOffset = Schema.GetSchemaOffset("CBaseModelEntity", "m_clrRender");
    private static readonly int MoveTypeOffset = Schema.GetSchemaOffset("CBaseEntity", "m_MoveType");
    private static readonly int GroundOffset = Schema.GetSchemaOffset("CBaseEntity", "m_hGroundEntity");
    private static readonly int BodyOffset = Schema.GetSchemaOffset("CBaseEntity", "m_CBodyComponent");
    private static readonly int SceneNodeOffset = Schema.GetSchemaOffset("CBodyComponent", "m_pSceneNode");
    private static readonly int OriginOffset = Schema.GetSchemaOffset("CGameSceneNode", "m_vecAbsOrigin");
    private static readonly int EyeAnglesOffset = Schema.GetSchemaOffset("CCSPlayerPawn", "m_angEyeAngles");
    private static readonly int MovementOffset = Schema.GetSchemaOffset("CBasePlayerPawn", "m_pMovementServices");
    private static readonly int ButtonsOffset = Schema.GetSchemaOffset("CPlayer_MovementServices", "m_nButtons");
    private static readonly int ButtonStatesOffset = Schema.GetSchemaOffset("CInButtonState", "m_pButtonStates");
    private static readonly int DuckAmountOffset = Schema.GetSchemaOffset("CCSPlayer_MovementServices", "m_flDuckAmount");
    private static readonly int DuckSpeedOffset = Schema.GetSchemaOffset("CCSPlayer_MovementServices", "m_flDuckSpeed");
    private static readonly int LastDuckTimeOffset = Schema.GetSchemaOffset("CCSPlayer_MovementServices", "m_flLastDuckTime");
    private static readonly int QueuedMaskOffset = Schema.GetSchemaOffset("CPlayer_MovementServices", "m_nQueuedButtonChangeMask");
    private static readonly int SpottedOffset = Schema.GetSchemaOffset("CCSPlayerPawn", "m_entitySpottedState");
    private static readonly int SpottedMaskOffset = Schema.GetSchemaOffset("EntitySpottedState_t", "m_bSpottedByMask");

    private static readonly int WeaponServicesOffset = Schema.GetSchemaOffset("CBasePlayerPawn", "m_pWeaponServices");
    private static readonly int ActiveWeaponOffset = Schema.GetSchemaOffset("CPlayer_WeaponServices", "m_hActiveWeapon");
    private static readonly int ItemDefinitionOffset = Schema.GetSchemaOffset("CEconEntity", "m_AttributeManager")
        + Schema.GetSchemaOffset("CAttributeContainer", "m_Item")
        + Schema.GetSchemaOffset("CEconItemView", "m_iItemDefinitionIndex");

    private const int Slots = 64;

    private static readonly CBasePlayerWeapon?[] Weapons = new CBasePlayerWeapon?[Slots];
    private static readonly nint[] WeaponPointers = new nint[Slots];
    private static readonly string?[] WeaponDesigners = new string?[Slots];
    private static readonly string?[] WeaponNames = new string?[Slots];
    private static readonly int[] WeaponStamps = new int[Slots];
    private static readonly uint[] WeaponHandles = new uint[Slots];
    private static readonly uint[] WeaponWrapped = new uint[Slots];

    private static readonly CCSPlayerPawn?[] Pawns = new CCSPlayerPawn?[Slots];
    private static readonly nint[] Pointers = new nint[Slots];
    private static readonly int[] Stamps = new int[Slots];
    private static readonly uint[] PawnHandles = new uint[Slots];
    private static int _generation = 1;

    public struct Living
    {
        public CCSPlayerController Player;
        public nint Pawn;
        public int Slot;
        public byte Team;
    }

    private static readonly Living[] LivingBuffer = new Living[Slots];
    private static int _livingCount;
    private static int _livingGeneration;

    public static void Invalidate() => _generation++;

    public static ReadOnlySpan<Living> AlivePlayers(IReadOnlyList<CCSPlayerController> players)
    {
        if (_livingGeneration == _generation)
            return LivingBuffer.AsSpan(0, _livingCount);

        int count = 0;
        for (int i = 0; i < players.Count && count < Slots; i++)
        {
            var player = players[i];
            if (player == null || (uint)player.Slot >= Slots || !Alive(player))
                continue;

            nint pawn = Pointers[player.Slot];
            LivingBuffer[count++] = new Living { Player = player, Pawn = pawn, Slot = player.Slot, Team = Team(pawn) };
        }

        _livingCount = count;
        _livingGeneration = _generation;
        return LivingBuffer.AsSpan(0, count);
    }

    public static CCSPlayerPawn? Pawn(CCSPlayerController? player)
    {
        if (player == null)
            return null;

        int slot = player.Slot;
        if ((uint)slot >= Slots)
            return player.IsValid ? player.PlayerPawn.Value : null;

        if (Stamps[slot] == _generation && Pointers[slot] != 0)
            return Pawns[slot];

        if (!player.IsValid)
            return null;

        uint raw = *(uint*)(player.Handle + PawnHandle);
        nint pointer = EntitySystem.GetEntityByHandle(raw) ?? 0;
        nint previous = Pointers[slot];
        Stamps[slot] = _generation;
        Pointers[slot] = pointer;
        if (pointer == 0)
            return Pawns[slot] = null;

        var cached = Pawns[slot];
        if (cached == null || previous != pointer || PawnHandles[slot] != raw)
        {
            Pawns[slot] = cached = new CCSPlayerPawn(pointer);
            PawnHandles[slot] = raw;
        }
        return cached;
    }

    public static nint Pointer(CCSPlayerController? player)
    {
        var pawn = Pawn(player);
        if (pawn == null)
            return 0;
        return (uint)player!.Slot < Slots ? Pointers[player.Slot] : pawn.Handle;
    }

    public static CBasePlayerWeapon? ActiveWeapon(CCSPlayerController? player)
    {
        nint pawn = Pointer(player);
        if (pawn == 0)
            return null;

        int slot = player!.Slot;
        if ((uint)slot >= Slots)
            return Pawn(player)?.WeaponServices?.ActiveWeapon.Value;

        nint services = *(nint*)(pawn + WeaponServicesOffset);
        uint raw = services == 0 ? uint.MaxValue : *(uint*)(services + ActiveWeaponOffset);
        if (WeaponStamps[slot] == _generation && WeaponHandles[slot] == raw)
            return Weapons[slot];

        WeaponStamps[slot] = _generation;
        WeaponHandles[slot] = raw;
        nint pointer = services == 0 ? 0 : EntitySystem.GetEntityByHandle(raw) ?? 0;
        if (pointer == 0)
        {
            WeaponPointers[slot] = 0;
            return Weapons[slot] = null;
        }

        if (WeaponPointers[slot] != pointer || Weapons[slot] == null || WeaponWrapped[slot] != raw)
        {
            WeaponPointers[slot] = pointer;
            WeaponWrapped[slot] = raw;
            Weapons[slot] = new CBasePlayerWeapon(pointer);
            WeaponDesigners[slot] = null;
            WeaponNames[slot] = null;
        }

        return Weapons[slot];
    }

    public static string? ActiveWeaponDesigner(CCSPlayerController? player)
    {
        var weapon = ActiveWeapon(player);
        if (weapon == null)
            return null;

        int slot = player!.Slot;
        if ((uint)slot >= Slots)
            return weapon.DesignerName;

        return WeaponDesigners[slot] ??= weapon.DesignerName;
    }

    public static string? ActiveWeaponName(CCSPlayerController? player)
    {
        string? designer = ActiveWeaponDesigner(player);
        if (string.IsNullOrEmpty(designer))
            return null;

        int slot = player!.Slot;
        if ((uint)slot < Slots && WeaponNames[slot] is { } cached)
            return cached;

        nint pointer = (uint)slot < Slots ? WeaponPointers[slot] : ActiveWeapon(player)!.Handle;
        int itemDef = *(ushort*)(pointer + ItemDefinitionOffset);
        string name = WeaponUtil.NormalizeWeaponName(designer, itemDef);
        if ((uint)slot < Slots)
            WeaponNames[slot] = name;
        return name;
    }

    public static bool Alive(CCSPlayerController? player)
    {
        if (player == null || (uint)player.Slot >= Slots)
            return player?.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;

        return Pawn(player) != null && *(byte*)(Pointers[player.Slot] + LifeStateOffset) == (byte)LifeState_t.LIFE_ALIVE;
    }

    public static byte LifeState(nint pawn) => *(byte*)(pawn + LifeStateOffset);
    public static int Health(nint pawn) => *(int*)(pawn + HealthOffset);
    public static byte Team(nint entity) => *(byte*)(entity + TeamOffset);
    public static uint Flags(nint entity) => *(uint*)(entity + FlagsOffset);
    public static ref Vector3 Velocity(nint entity) => ref *(Vector3*)(entity + VelocityOffset);
    public static byte RenderAlpha(nint entity) => *(byte*)(entity + RenderOffset + 3);
    public static byte MoveType(nint entity) => *(byte*)(entity + MoveTypeOffset);
    public static bool OnGroundEntity(nint entity) => *(uint*)(entity + GroundOffset) != uint.MaxValue;
    public static ref Vector3 EyeAngles(nint pawn) => ref *(Vector3*)(pawn + EyeAnglesOffset);

    public static bool Origin(nint entity, out Vector3 origin)
    {
        origin = default;
        nint body = *(nint*)(entity + BodyOffset);
        if (body == 0)
            return false;
        nint node = *(nint*)(body + SceneNodeOffset);
        if (node == 0)
            return false;
        origin = *(Vector3*)(node + OriginOffset);
        return true;
    }

    public static bool Buttons(nint pawn, out ulong buttons, out ulong queued)
    {
        buttons = 0;
        queued = 0;
        nint movement = *(nint*)(pawn + MovementOffset);
        if (movement == 0)
            return false;
        buttons = *(ulong*)(movement + ButtonsOffset + ButtonStatesOffset);
        queued = *(ulong*)(movement + QueuedMaskOffset);
        return true;
    }

    public static float DuckAmount(nint pawn)
    {
        nint movement = *(nint*)(pawn + MovementOffset);
        return movement == 0 ? 0f : *(float*)(movement + DuckAmountOffset);
    }

    public static void KeepDuckFresh(nint pawn, float minSpeed)
    {
        if (pawn == 0)
            return;

        nint movement = *(nint*)(pawn + MovementOffset);
        if (movement == 0)
            return;

        float* speed = (float*)(movement + DuckSpeedOffset);
        if (*speed < minSpeed)
            *speed = minSpeed;

        float* last = (float*)(movement + LastDuckTimeOffset);
        if (*last != 0f)
            *last = 0f;
    }

    public static uint* SpottedByMask(nint pawn) => (uint*)(pawn + SpottedOffset + SpottedMaskOffset);
}
