using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace ByDexter.Shared;

public static class SoundUtil
{
    public static void EmitToPlayer(CCSPlayerController? listener, string soundEvent, float volume)
    {
        if (listener == null || !listener.IsValid || listener.IsBot || soundEvent.Length == 0 || volume <= 0f)
            return;

        listener.EmitSound(soundEvent, new RecipientFilter(listener), volume);
    }

    public static void EmitToPawn(CCSPlayerPawn? pawn, string soundEvent, float volume)
    {
        if (pawn == null || !pawn.IsValid)
            return;

        EmitToPlayer(pawn.Controller.Value?.As<CCSPlayerController>(), soundEvent, volume);
    }

    public static void PlayFor(CCSPlayerController source, List<CCSPlayerController> listeners, string path, string emit, float volume)
    {
        if (listeners.Count == 0 || volume <= 0f)
            return;

        if (emit.Length > 0)
        {
            if (!source.IsValid)
                return;

            var filter = new RecipientFilter();
            foreach (var listener in listeners)
                filter.Add(listener);

            source.EmitSound(emit, filter, volume);
            return;
        }

        if (path.Length == 0)
            return;

        foreach (var listener in listeners)
            listener.ExecuteClientCommand($"play {path}");
    }
}
