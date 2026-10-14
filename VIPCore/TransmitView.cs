using CounterStrikeSharp.API.Core;

namespace VIPCore;

public delegate void TransmitHook(ReadOnlySpan<TransmitView> views);

public readonly unsafe struct TransmitView
{
    private const uint MaxEntities = 16384;

    public readonly CCSPlayerController? Viewer;
    public readonly int Slot;
    private readonly uint* _bits;
    private readonly uint* _dont;

    public TransmitView(CCSPlayerController? viewer, int slot, uint* bits, uint* dont)
    {
        Viewer = viewer;
        Slot = slot;
        _bits = bits;
        _dont = dont;
    }

    public void Hide(uint index)
    {
        if (index == 0 || index >= MaxEntities || _bits == null)
            return;

        uint word = index >> 5;
        uint mask = 1u << (int)(index & 31);
        if ((_bits[word] & mask) == 0)
            return;

        _bits[word] &= ~mask;
        if (_dont != null)
            _dont[word] |= mask;
    }

    public void Hide(int index)
    {
        if (index > 0)
            Hide((uint)index);
    }

    public void Hide(CEntityInstance? entity)
    {
        if (entity != null && entity.IsValid)
            Hide(entity.Index);
    }
}
