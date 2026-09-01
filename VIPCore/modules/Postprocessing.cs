using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace VIPCore;

public class Postprocessing : VipModule
{
    private class Entry
    {
        public string Name { get; set; } = "";
        public string File { get; set; } = "";
        public float Fade { get; set; } = 0.25f;
    }

    private const string VolumeClass = "post_processing_volume";
    private const string VolumeName = "vipcore_postprocessing";

    private readonly CPostProcessingVolume?[] _volumes = new CPostProcessingVolume?[64];
    private readonly List<CPostProcessingVolume> _mapVolumes = new();

    public override string Name => "Postprocessing";
    public override string DisplayName => Core.Localizer["vip.module.postprocessing"];
    public override VipFeatureType MenuType => VipFeatureType.Select;

    public override List<VipFeatureOption> SelectOptions(CCSPlayerController player)
    {
        var entries = GroupValue<List<Entry>>(player) ?? new();
        return entries.Where(e => e.Name.Length > 0 && e.File.Length > 0)
            .Select(e => new VipFeatureOption(e.Name, e.Name)).ToList();
    }

    public override void OnLoad()
    {
        Core.HookPrecache(manifest =>
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entries in Core.GetAllGroupValues<List<Entry>>(Name))
            {
                foreach (var entry in entries)
                {
                    if (entry.File.Length == 0 || !seen.Add(entry.File))
                        continue;

                    try { manifest.AddResource(entry.File); }
                    catch { }
                }
            }
        });
        Core.HookTransmit(OnCheckTransmit);
        Core.HookMapEnd(() => { Array.Clear(_volumes); _mapVolumes.Clear(); });

        Core.RegisterEventHandler<EventPlayerSpawn>((ev, _) =>
        {
            var player = ev.Userid;
            Server.NextFrame(() => Apply(player));
            return HookResult.Continue;
        });
        Core.RegisterEventHandler<EventPlayerDeath>((ev, _) => { Remove(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        Core.RegisterEventHandler<EventPlayerDisconnect>((ev, _) => { Remove(ev.Userid?.Slot ?? -1); return HookResult.Continue; });
        Core.RegisterEventHandler<EventRoundEnd>((_, __) => { RemoveAll(); return HookResult.Continue; });
        Core.RegisterEventHandler<EventRoundStart>((_, __) =>
        {
            RemoveAll();
            RefreshMapVolumes();
            return HookResult.Continue;
        });
    }

    public override void OnUnload() => RemoveAll();

    public override void OnSelect(CCSPlayerController player, string value)
    {
        Remove(player.Slot);
        if (value != "off")
            Apply(player);
    }

    private void RemoveAll()
    {
        for (int slot = 0; slot < 64; slot++)
            Remove(slot);
    }

    private void Remove(int slot)
    {
        if (slot < 0 || slot >= 64)
            return;

        var volume = _volumes[slot];
        _volumes[slot] = null;

        if (volume != null && volume.IsValid && volume.DesignerName == VolumeClass)
            volume.Remove();
    }

    private void Apply(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.IsBot || !IsAlive(player) || !Active(player))
            return;

        int slot = player.Slot;
        Remove(slot);

        var entries = GroupValue<List<Entry>>(player) ?? new();
        var entry = entries.FirstOrDefault(e => e.Name == Setting(player));
        if (entry == null || entry.File.Length == 0)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
            return;

        var volume = Utilities.CreateEntityByName<CPostProcessingVolume>(VolumeClass);
        if (volume == null || !volume.IsValid || volume.Entity == null)
            return;

        var keys = new CEntityKeyValues();
        keys.SetString("targetname", VolumeName);
        keys.SetString("postprocessing", entry.File);
        keys.SetBool("master", true);
        keys.SetBool("enableexposure", true);
        keys.SetFloat("fadetime", Math.Max(entry.Fade, 0f));
        keys.SetFloat("minexposure", 0.5f);
        keys.SetFloat("maxexposure", 2f);
        keys.SetFloat("exposurespeedup", 1f);
        keys.SetFloat("exposurespeeddown", 1f);
        keys.SetBool("startdisabled", false);
        keys.SetInt("spawnflags", 4097);
        keys.SetVector("origin", pawn.AbsOrigin);

        volume.DispatchSpawn(keys);
        keys.Dispose();

        if (!volume.IsValid)
            return;

        volume.AcceptInput("SetParent", pawn, null, "!activator");
        _volumes[slot] = volume;
    }

    private void RefreshMapVolumes()
    {
        _mapVolumes.Clear();
        foreach (var volume in Utilities.FindAllEntitiesByDesignerName<CPostProcessingVolume>(VolumeClass))
        {
            if (volume.IsValid && !string.Equals(volume.Entity?.Name, VolumeName, StringComparison.Ordinal))
                _mapVolumes.Add(volume);
        }
    }

    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        int active = 0;
        for (int slot = 0; slot < 64; slot++)
            if (_volumes[slot] != null)
                active++;

        if (active == 0)
            return;

        foreach (var (info, viewer) in infoList)
        {
            if (viewer == null || !viewer.IsValid || viewer.Slot >= 64)
                continue;

            int shown = viewer.Slot;
            if (!IsAlive(viewer))
            {
                var observed = PawnController(viewer.Pawn.Value?.ObserverServices?.ObserverTarget.Value);
                if (observed != null && observed.Slot >= 0 && observed.Slot < 64)
                    shown = observed.Slot;
            }

            for (int slot = 0; slot < 64; slot++)
            {
                if (slot == shown)
                    continue;

                var volume = _volumes[slot];
                if (volume != null && volume.IsValid)
                    info.TransmitEntities.Remove(volume);
            }

            if (_volumes[shown] == null)
                continue;

            foreach (var mapVolume in _mapVolumes)
                if (mapVolume.IsValid)
                    info.TransmitEntities.Remove(mapVolume);
        }
    }
}
