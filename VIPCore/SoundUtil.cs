using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public static class SoundUtil
{
    public static void PlayFor(CCSPlayerController source, List<CCSPlayerController> listeners, string path, string emit, float volume)
    {
        if (listeners.Count == 0)
            return;

        if (emit.Length > 0)
        {
            if (!source.IsValid)
                return;

            var filter = new RecipientFilter();
            foreach (var listener in listeners)
                filter.Add(listener);

            source.EmitSound(emit, filter, volume <= 0f ? 1f : volume);
            return;
        }

        if (path.Length == 0)
            return;

        foreach (var listener in listeners)
            listener.ExecuteClientCommand($"play {path}");
    }
}
