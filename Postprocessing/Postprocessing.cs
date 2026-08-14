using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;
using Microsoft.Extensions.Logging;
using ByDexter.Shared;

namespace Postprocessing;

public class PostprocessingPreset
{
  [JsonPropertyName("name")]
  public string Name { get; set; } = "";

  [JsonPropertyName("file")]
  public string File { get; set; } = "";

  [JsonPropertyName("category")]
  public string Category { get; set; } = "";

  [JsonPropertyName("flag")]
  public string Flag { get; set; } = "";

  [JsonPropertyName("fade")]
  public float Fade { get; set; } = 0.25f;

  [JsonPropertyName("exposure")]
  public bool Exposure { get; set; } = true;

  [JsonPropertyName("min_exposure")]
  public float MinExposure { get; set; } = 0.5f;

  [JsonPropertyName("max_exposure")]
  public float MaxExposure { get; set; } = 2f;

  [JsonPropertyName("exposure_speed_up")]
  public float ExposureSpeedUp { get; set; } = 1f;

  [JsonPropertyName("exposure_speed_down")]
  public float ExposureSpeedDown { get; set; } = 1f;

  [JsonPropertyName("fov")]
  public int Fov { get; set; }
}

public class PostprocessingConfig : BasePluginConfig
{
  [JsonPropertyName("pp_cmd")]
  public string Commands { get; set; } = "css_pp,css_postprocessing";

  [JsonPropertyName("pp_flag")]
  public string Flag { get; set; } = "";

  [JsonPropertyName("pp_give_cmd")]
  public string GiveCommands { get; set; } = "css_givepp";

  [JsonPropertyName("pp_give_flag")]
  public string GiveFlag { get; set; } = "@css/generic";

  [JsonPropertyName("pp_remember")]
  public bool Remember { get; set; } = true;

  [JsonPropertyName("pp_hide_map_effects")]
  public bool HideMapEffects { get; set; } = true;

  [JsonPropertyName("pp_presets")]
  public List<PostprocessingPreset> Presets { get; set; } = new()
  {
    P("death_cam_phase1", "lighting/postprocessing/effects/death_cam_phase1.vpost", "Efektler"),
    P("death_cam_phase1_low_violence", "lighting/postprocessing/effects/death_cam_phase1_low_violence.vpost", "Efektler"),
    P("death_cam_phase2", "lighting/postprocessing/effects/death_cam_phase2.vpost", "Efektler"),
    P("heavyassaultsuit", "lighting/postprocessing/effects/heavyassaultsuit.vpost", "Efektler"),
    P("hltv_replay", "lighting/postprocessing/effects/hltv_replay.vpost", "Efektler"),
    P("hltv_replay_fade", "lighting/postprocessing/effects/hltv_replay_fade.vpost", "Efektler"),
    P("in_buy_menu", "lighting/postprocessing/effects/in_buy_menu.vpost", "Efektler"),
    P("round_end_via_bombing", "lighting/postprocessing/effects/round_end_via_bombing.vpost", "Efektler"),
    P("zoomed_rifle", "lighting/postprocessing/effects/zoomed_rifle.vpost", "Efektler"),
    P("zoomed_sniper", "lighting/postprocessing/effects/zoomed_sniper.vpost", "Efektler"),
    P("zoomed_sniper_moving", "lighting/postprocessing/effects/zoomed_sniper_moving.vpost", "Efektler"),
    P("bloomtest", "lighting/postprocessing/correction/bloomtest.vpost", "Renk"),
    P("cc_freeze_ct", "lighting/postprocessing/correction/cc_freeze_ct.vpost", "Renk"),
    P("cc_freeze_t", "lighting/postprocessing/correction/cc_freeze_t.vpost", "Renk"),
    P("ar_dizzy", "lighting/postprocessing/ar_dizzy.vpost", "Genel"),
    P("basepostprocess", "lighting/postprocessing/basepostprocess.vpost", "Genel"),
    P("basepostprocess_filmic", "lighting/postprocessing/basepostprocess_filmic.vpost", "Genel"),
    P("cs_office_s2", "lighting/postprocessing/cs_office_s2.vpost", "Genel"),
    P("de_mirage_postprocess", "lighting/postprocessing/de_mirage_postprocess.vpost", "Genel"),
    P("filmic_default", "lighting/postprocessing/filmic_default.vpost", "Genel"),
    P("graphics_settings", "lighting/postprocessing/graphics_settings.vpost", "Genel"),
    P("inpsect_weapon", "lighting/postprocessing/inpsect_weapon.vpost", "Genel"),
    P("inspect_laptop", "lighting/postprocessing/inspect_laptop.vpost", "Genel"),
    P("legacy_filmic_default", "lighting/postprocessing/legacy_filmic_default.vpost", "Genel"),
    P("vanity_warehouse", "lighting/postprocessing/vanity_warehouse.vpost", "Genel"),
    P("cache_postprocessv2", "postprocess/cache_postprocessv2.vpost", "Genel"),
    P("cache_postprocessv3", "postprocess/cache_postprocessv3.vpost", "Genel"),
    P("de_cache_s2", "postprocess/de_cache_s2.vpost", "Genel"),
    P("zoom", "", "Genel", 40),
    P("icon_generation_basic", "lighting/postprocessing/ui/icon_generation_basic.vpost", "Arayuz"),
    P("icon_generation_characters", "lighting/postprocessing/ui/icon_generation_characters.vpost", "Arayuz"),
    P("xp_shop_case", "lighting/postprocessing/ui/xp_shop_case.vpost", "Arayuz"),
    P("xp_shop_item", "lighting/postprocessing/ui/xp_shop_item.vpost", "Arayuz"),
    P("ar_baggage_postprocess", "lighting/postprocessing/ar_baggage_prefab/ar_baggage_postprocess.vpost", "Haritalar"),
    P("ar_baggage_prefab", "lighting/postprocessing/ar_baggage_prefab/ar_baggage_prefab.vpost", "Haritalar"),
    P("ar_baggage_vanity_postprocess", "lighting/postprocessing/ar_baggage_prefab/ar_baggage_vanity_postprocess.vpost", "Haritalar"),
    P("ar_shoots_postprocess_v1", "lighting/postprocessing/ar_shoots_prefab/ar_shoots_postprocess_v1.vpost", "Haritalar"),
    P("ar_shoots_postprocess_v2", "lighting/postprocessing/ar_shoots_prefab/ar_shoots_postprocess_v2.vpost", "Haritalar"),
    P("ar_shoots_prefab", "lighting/postprocessing/ar_shoots_prefab/ar_shoots_prefab.vpost", "Haritalar"),
    P("cs_italy_prefab", "lighting/postprocessing/cs_italy_prefab/cs_italy_prefab.vpost", "Haritalar"),
    P("cs_italy_s2_postprocess", "lighting/postprocessing/cs_italy_s2_prefab/cs_italy_s2_postprocess.vpost", "Haritalar"),
    P("cs_italy_s2_postprocess_linear", "lighting/postprocessing/cs_italy_s2_prefab/cs_italy_s2_postprocess_linear.vpost", "Haritalar"),
    P("cs_italy_s2_postprocess_nonlinear", "lighting/postprocessing/cs_italy_s2_prefab/cs_italy_s2_postprocess_nonlinear.vpost", "Haritalar"),
    P("cs_italy_vanity_postprocess", "lighting/postprocessing/cs_italy_s2_prefab/cs_italy_vanity_postprocess.vpost", "Haritalar"),
    P("cs_office_postprocess", "lighting/postprocessing/cs_office_prefab/cs_office_postprocess.vpost", "Haritalar"),
    P("cs_office_prefab", "lighting/postprocessing/cs_office_prefab/cs_office_prefab.vpost", "Haritalar"),
    P("de_ancient_postprocess_v1", "lighting/postprocessing/de_ancient_prefab/de_ancient_postprocess_v1.vpost", "Haritalar"),
    P("de_ancient_postprocess_v2", "lighting/postprocessing/de_ancient_prefab/de_ancient_postprocess_v2.vpost", "Haritalar"),
    P("de_ancient_prefab", "lighting/postprocessing/de_ancient_prefab/de_ancient_prefab.vpost", "Haritalar"),
    P("de_ancient_vanity", "lighting/postprocessing/de_ancient_prefab/de_ancient_vanity.vpost", "Haritalar"),
    P("de_ancient_vanity_postprocess_v2", "lighting/postprocessing/de_ancient_prefab/de_ancient_vanity_postprocess_v2.vpost", "Haritalar"),
    P("de_ancient_visual_update", "lighting/postprocessing/de_ancient_visual_update_05-21_prefab/de_ancient_visual_update_05-21_prefab.vpost", "Haritalar"),
    P("de_ancient_zoo_prefab", "lighting/postprocessing/de_ancient_zoo_prefab/de_ancient_zoo_prefab.vpost", "Haritalar"),
    P("de_anubis_prefab", "lighting/postprocessing/de_anubis_prefab/de_anubis_prefab.vpost", "Haritalar"),
    P("match_mvp", "lighting/postprocessing/de_anubis_prefab/match_mvp.vpost", "Haritalar"),
    P("de_cache_prefab", "lighting/postprocessing/de_cache_prefab/de_cache_prefab.vpost", "Haritalar"),
    P("de_cache_prefab_2", "lighting/postprocessing/de_cache_prefab/de_cache_prefab_2.vpost", "Haritalar"),
    P("de_cache_prefab_vanity", "lighting/postprocessing/de_cache_prefab/de_cache_prefab_vanity.vpost", "Haritalar"),
    P("de_dust2_prefab", "lighting/postprocessing/de_dust2_prefab/de_dust2_prefab.vpost", "Haritalar"),
    P("de_dust2_vanity", "lighting/postprocessing/de_dust2_prefab/de_dust2_vanity.vpost", "Haritalar"),
    P("de_dust2_zoo_prefab", "lighting/postprocessing/de_dust2_zoo_prefab/de_dust2_zoo_prefab.vpost", "Haritalar"),
    P("de_inferno_postprocess", "lighting/postprocessing/de_inferno_prefab/de_inferno_postprocess.vpost", "Haritalar"),
    P("de_inferno_prefab", "lighting/postprocessing/de_inferno_prefab/de_inferno_prefab.vpost", "Haritalar"),
    P("de_inferno_vanity", "lighting/postprocessing/de_inferno_prefab/de_inferno_vanity.vpost", "Haritalar"),
    P("de_inferno_vanity_prefab", "lighting/postprocessing/de_inferno_prefab/de_inferno_vanity_prefab.vpost", "Haritalar"),
    P("de_mirage", "lighting/postprocessing/de_mirage_prefab/de_mirage.vpost", "Haritalar"),
    P("de_mirage_prefab", "lighting/postprocessing/de_mirage_prefab/de_mirage_prefab.vpost", "Haritalar"),
    P("de_mirage_vanity", "lighting/postprocessing/de_mirage_prefab/de_mirage_vanity.vpost", "Haritalar"),
    P("de_nuke_post", "lighting/postprocessing/de_nuke_prefab/de_nuke_post.vpost", "Haritalar"),
    P("de_nuke_prefab", "lighting/postprocessing/de_nuke_prefab/de_nuke_prefab.vpost", "Haritalar"),
    P("de_nuke_zoo_prefab", "lighting/postprocessing/de_nuke_zoo_prefab/de_nuke_zoo_prefab.vpost", "Haritalar"),
    P("de_overpass_prefab", "lighting/postprocessing/de_overpass_prefab/de_overpass_prefab.vpost", "Haritalar"),
    P("de_train_post", "lighting/postprocessing/de_train_prefab/de_train_post.vpost", "Haritalar"),
    P("de_train_post_v2", "lighting/postprocessing/de_train_prefab/de_train_post_v2.vpost", "Haritalar"),
    P("de_train_postprocess", "lighting/postprocessing/de_train_prefab/de_train_postprocess.vpost", "Haritalar"),
    P("de_train_postprocess_robbr", "lighting/postprocessing/de_train_prefab/de_train_postprocess_robbr.vpost", "Haritalar"),
    P("de_train_postprocess_v2", "lighting/postprocessing/de_train_prefab/de_train_postprocess_v2.vpost", "Haritalar"),
    P("de_train_postprocess_v2_hable", "lighting/postprocessing/de_train_prefab/de_train_postprocess_v2_hable.vpost", "Haritalar"),
    P("de_train_prefab", "lighting/postprocessing/de_train_prefab/de_train_prefab.vpost", "Haritalar"),
    P("de_train_vanity_postprocess", "lighting/postprocessing/de_train_prefab/de_train_vanity_postprocess.vpost", "Haritalar"),
    P("de_vertigo_prefab", "lighting/postprocessing/de_vertigo_prefab/de_vertigo_prefab.vpost", "Haritalar"),
    P("lobby_mapveto_prefab", "lighting/postprocessing/lobby_mapveto_prefab/lobby_mapveto_prefab.vpost", "Haritalar"),
    P("bldr_01_ct_spawn", "lighting/postprocessing/bldr_01_ct_spawn/bldr_01_ct_spawn.vpost", "HaritaOzel"),
    P("bldr_04_b_site", "lighting/postprocessing/bldr_04_b_site/bldr_04_b_site.vpost", "HaritaOzel"),
    P("de_boulder_postprocess", "lighting/postprocessing/de_boulder_postprocess.vpost", "HaritaOzel"),
    P("de_boulder_postprocess2", "lighting/postprocessing/de_boulder_postprocess2.vpost", "HaritaOzel"),
    P("de_boulder_postprocess3", "lighting/postprocessing/de_boulder_postprocess3.vpost", "HaritaOzel"),
    P("de_boulder_prefab", "lighting/postprocessing/de_boulder_prefab/de_boulder_prefab.vpost", "HaritaOzel"),
    P("de_inferno_postprocess_boulder", "lighting/postprocessing/de_inferno_postprocess.vpost", "HaritaOzel"),
    P("eldorado_postprocess", "materials/eldorado_postprocess.vpost", "HaritaOzel"),
    P("ar_pool_day", "postprocess/ar_pool_day.vpost", "HaritaOzel"),
    P("basic_linear_post", "postprocess/basic_linear_post.vpost", "HaritaOzel"),
    P("de_boulder_skybox", "postprocess/de_boulder_skybox.vpost", "HaritaOzel"),
    P("de_debris", "postprocess/de_debris.vpost", "HaritaOzel"),
    P("de_fachwerk", "postprocess/de_fachwerk.vpost", "HaritaOzel"),
    P("de_fachwerk2", "postprocess/de_fachwerk2.vpost", "HaritaOzel"),
    P("de_fachwerk3", "postprocess/de_fachwerk3.vpost", "HaritaOzel"),
    P("de_fachwerk3_drunk", "postprocess/de_fachwerk3_drunk.vpost", "HaritaOzel"),
    P("de_fachwerk4", "postprocess/de_fachwerk4.vpost", "HaritaOzel"),
    P("de_fachwerk5", "postprocess/de_fachwerk5.vpost", "HaritaOzel"),
    P("drawbridge", "postprocess/drawbridge.vpost", "HaritaOzel"),
    P("eldorado", "postprocess/eldorado.vpost", "HaritaOzel"),
    P("poseidon", "postprocess/poseidon.vpost", "HaritaOzel"),
    P("postprocess_filmic_pool_day", "postprocess/postprocess_filmic_pool_day.vpost", "HaritaOzel"),
    P("postprocess_filmic_pool_day_cs16", "postprocess/postprocess_filmic_pool_day_cs16.vpost", "HaritaOzel"),
    P("postprocess_filmic_underwater", "postprocess/postprocess_filmic_underwater.vpost", "HaritaOzel")
  };

  private static PostprocessingPreset P(string name, string file, string category, int fov = 0) =>
    new() { Name = name, File = file, Category = category, Fov = fov };
}

public class Postprocessing : BasePlugin, IPluginConfig<PostprocessingConfig>
{
  public override string ModuleName => "Postprocessing";
  public override string ModuleVersion => "1.0.1";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public PostprocessingConfig Config { get; set; } = new();

  private const int MaxSlots = 64;
  private const string VolumeClass = "post_processing_volume";
  private const string VolumeName = "bydexter_postprocessing";

  private readonly CPostProcessingVolume?[] _volumes = new CPostProcessingVolume?[MaxSlots];
  private readonly PostprocessingPreset?[] _active = new PostprocessingPreset?[MaxSlots];
  private readonly Dictionary<ulong, string> _saved = new();
  private readonly List<CPostProcessingVolume> _mapVolumes = new();
  private readonly object _saveLock = new();

  private WasdMenuManager _menus = null!;

  private string SavePath => Path.Combine(ModuleDirectory, "Postprocessing.json");

  public void OnConfigParsed(PostprocessingConfig config)
  {
    foreach (var preset in config.Presets)
    {
      if (preset.Fade < 0f)
        preset.Fade = 0f;
      if (preset.MinExposure <= 0f)
        preset.MinExposure = 0.1f;
      if (preset.MaxExposure < preset.MinExposure)
        preset.MaxExposure = preset.MinExposure;
      if (preset.Fov < 0 || preset.Fov > 179)
        preset.Fov = 0;
    }
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    _menus = new WasdMenuManager(this,
      () => Localizer["menu.scroll"],
      () => Localizer["menu.select"],
      () => Localizer["menu.exit"]);

    LoadSaved();

    foreach (var name in Util.Split(Config.Commands))
      AddCommand(name, "Post processing menusunu acar", OnMenuCommand);

    foreach (var name in Util.Split(Config.GiveCommands))
      AddCommand(name, "Oyuncuya post processing verir", OnGiveCommand);

    RegisterListener<OnServerPrecacheResources>(OnPrecache);
    RegisterListener<CheckTransmit>(OnCheckTransmit);
    RegisterListener<OnMapEnd>(OnMapEnd);

    RegisterEventHandler<EventRoundStart>(OnRoundStart);
    RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);

    if (hotReload)
      RefreshMapVolumes();
  }

  public override void Unload(bool hotReload)
  {
    _menus.Clear();
    for (int slot = 0; slot < MaxSlots; slot++)
      Clear(slot, true);
    _mapVolumes.Clear();
  }

  private void OnPrecache(ResourceManifest manifest)
  {
    foreach (var preset in Config.Presets)
    {
      if (string.IsNullOrWhiteSpace(preset.File))
        continue;

      try
      {
        manifest.AddResource(preset.File);
      }
      catch (Exception ex)
      {
        Logger.LogWarning("Post processing dosyasi yuklenemedi: {File} ({Message})", preset.File, ex.Message);
      }
    }
  }

  private List<PostprocessingPreset> AvailableFor(CCSPlayerController player) =>
    Config.Presets.Where(p => Util.HasAccess(player, p.Flag)).ToList();

  private PostprocessingPreset? FindPreset(string name) =>
    Config.Presets.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

  private void OnMenuCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid || player.IsBot)
      return;

    if (!Util.HasAccess(player, Config.Flag))
    {
      Reply(player, Localizer["no_access"]);
      return;
    }

    if (info.ArgCount > 1)
    {
      string arg = info.GetArg(1);

      if (arg.Equals("off", StringComparison.OrdinalIgnoreCase) || arg.Equals("kapat", StringComparison.OrdinalIgnoreCase))
      {
        Clear(player.Slot, true);
        Remember(player, null);
        Reply(player, Localizer["cleared"]);
        return;
      }

      var wanted = FindPreset(arg);
      if (wanted == null || !Util.HasAccess(player, wanted.Flag))
      {
        Reply(player, Localizer["preset_not_found", arg]);
        return;
      }

      Apply(player, wanted);
      Remember(player, wanted);
      Reply(player, Localizer["applied", wanted.Name]);
      return;
    }

    ShowMenu(player);
  }

  private void ShowMenu(CCSPlayerController player)
  {
    var presets = AvailableFor(player);
    if (presets.Count == 0)
    {
      Reply(player, Localizer["no_preset"]);
      return;
    }

    var categories = new List<string>();
    foreach (var preset in presets)
    {
      string category = string.IsNullOrWhiteSpace(preset.Category) ? Localizer["menu.uncategorized"] : preset.Category;
      if (!categories.Contains(category))
        categories.Add(category);
    }

    var items = new List<WasdItem>();
    foreach (var category in categories)
    {
      var captured = category;
      int count = presets.Count(p => CategoryOf(p) == captured);
      items.Add(new WasdItem
      {
        Text = Localizer["menu.category", captured, count],
        OnSelect = p => ShowCategory(p, captured)
      });
    }

    items.Add(new WasdItem
    {
      Text = Localizer["menu.off"],
      OnSelect = p =>
      {
        Clear(p.Slot, true);
        Remember(p, null);
        Reply(p, Localizer["cleared"]);
        ShowMenu(p);
      }
    });

    _menus.Open(player, Localizer["menu.title"], items);
  }

  private void ShowCategory(CCSPlayerController player, string category)
  {
    var presets = AvailableFor(player).Where(p => CategoryOf(p) == category).ToList();
    if (presets.Count == 0)
    {
      ShowMenu(player);
      return;
    }

    var items = new List<WasdItem>
    {
      WasdItem.Back(Localizer["menu.back"], ShowMenu)
    };

    foreach (var preset in presets)
    {
      var captured = preset;
      items.Add(new WasdItem
      {
        Text = _active[player.Slot] == captured ? Localizer["menu.item_active", captured.Name] : captured.Name,
        OnSelect = p =>
        {
          Apply(p, captured);
          Remember(p, captured);
          Reply(p, Localizer["applied", captured.Name]);
          ShowCategory(p, category);
        }
      });
    }

    _menus.Open(player, category, items);
  }

  private string CategoryOf(PostprocessingPreset preset) =>
    string.IsNullOrWhiteSpace(preset.Category) ? Localizer["menu.uncategorized"] : preset.Category;

  private void OnGiveCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player != null && !Util.HasAccess(player, Config.GiveFlag))
    {
      Reply(player, Localizer["no_access"]);
      return;
    }

    if (info.ArgCount < 3)
    {
      Reply(player, Localizer["give.usage"]);
      return;
    }

    var targets = info.GetArgTargetResult(1).Players
      .Where(p => p != null && p.IsValid && !p.IsHLTV && !p.IsBot)
      .ToList();

    if (targets.Count == 0)
    {
      Reply(player, Localizer["not_found"]);
      return;
    }

    string arg = info.GetArg(2);
    bool off = arg.Equals("off", StringComparison.OrdinalIgnoreCase) || arg.Equals("kapat", StringComparison.OrdinalIgnoreCase);
    var preset = off ? null : FindPreset(arg);

    if (!off && preset == null)
    {
      Reply(player, Localizer["preset_not_found", arg]);
      return;
    }

    foreach (var target in targets)
    {
      if (preset == null)
        Clear(target.Slot, true);
      else
        Apply(target, preset);

      Remember(target, preset);
      Reply(target, preset == null ? Localizer["cleared"] : Localizer["applied", preset.Name]);
    }

    Reply(player, preset == null
      ? Localizer["give.cleared", targets.Count]
      : Localizer["give.applied", preset.Name, targets.Count]);
  }

  private void Apply(CCSPlayerController player, PostprocessingPreset preset)
  {
    if (!player.IsValid)
      return;

    int slot = player.Slot;
    Clear(slot, false);
    _active[slot] = preset;

    SetFov(player, preset.Fov);

    if (string.IsNullOrWhiteSpace(preset.File) || !Util.IsAlive(player))
      return;

    _volumes[slot] = CreateVolume(player, preset);
  }

  private CPostProcessingVolume? CreateVolume(CCSPlayerController player, PostprocessingPreset preset)
  {
    var pawn = player.PlayerPawn.Value;
    if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
      return null;

    var volume = Utilities.CreateEntityByName<CPostProcessingVolume>(VolumeClass);
    if (volume == null || !volume.IsValid || volume.Entity == null)
      return null;

    var keys = new CEntityKeyValues();
    keys.SetString("targetname", VolumeName);
    keys.SetString("postprocessing", preset.File);
    keys.SetBool("master", true);
    keys.SetBool("enableexposure", preset.Exposure);
    keys.SetFloat("fadetime", preset.Fade);
    keys.SetFloat("minexposure", preset.MinExposure);
    keys.SetFloat("maxexposure", preset.MaxExposure);
    keys.SetFloat("exposurespeedup", preset.ExposureSpeedUp);
    keys.SetFloat("exposurespeeddown", preset.ExposureSpeedDown);
    keys.SetBool("startdisabled", false);
    keys.SetInt("spawnflags", 4097);
    keys.SetVector("origin", pawn.AbsOrigin);

    volume.DispatchSpawn(keys);
    keys.Dispose();

    if (!volume.IsValid)
      return null;

    volume.AcceptInput("SetParent", pawn, null, "!activator");
    return volume;
  }

  private void Clear(int slot, bool resetFov)
  {
    var volume = _volumes[slot];
    _volumes[slot] = null;
    if (volume != null && volume.IsValid)
      volume.Remove();

    if (!resetFov)
      return;

    _active[slot] = null;
    SetFov(Utilities.GetPlayerFromSlot(slot), 0);
  }

  private static void SetFov(CCSPlayerController? player, int fov)
  {
    if (player == null || !player.IsValid)
      return;

    player.DesiredFOV = (uint)fov;
    Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
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
    foreach ((CCheckTransmitInfo info, CCSPlayerController? viewer) in infoList)
    {
      if (viewer == null || !viewer.IsValid)
        continue;

      int viewerSlot = viewer.Slot;

      for (int slot = 0; slot < MaxSlots; slot++)
      {
        if (slot == viewerSlot)
          continue;

        var volume = _volumes[slot];
        if (volume != null && volume.IsValid)
          info.TransmitEntities.Remove(volume);
      }

      if (!Config.HideMapEffects || _volumes[viewerSlot] == null)
        continue;

      foreach (var volume in _mapVolumes)
      {
        if (volume.IsValid)
          info.TransmitEntities.Remove(volume);
      }
    }
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
  {
    RefreshMapVolumes();
    return HookResult.Continue;
  }

  private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
  {
    var player = @event.Userid;
    if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
      return HookResult.Continue;

    int slot = player.Slot;
    Server.NextFrame(() =>
    {
      var target = Utilities.GetPlayerFromSlot(slot);
      var preset = _active[slot] ?? Restore(target);
      if (target == null || !target.IsValid || preset == null)
        return;

      Apply(target, preset);
    });

    return HookResult.Continue;
  }

  private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
  {
    int slot = @event.Userid?.Slot ?? -1;
    if (slot >= 0 && slot < MaxSlots)
      Clear(slot, false);
    return HookResult.Continue;
  }

  private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    int slot = @event.Userid?.Slot ?? -1;
    if (slot >= 0 && slot < MaxSlots)
      Clear(slot, true);
    return HookResult.Continue;
  }

  private void OnMapEnd()
  {
    _mapVolumes.Clear();
    for (int slot = 0; slot < MaxSlots; slot++)
      _volumes[slot] = null;
  }

  private PostprocessingPreset? Restore(CCSPlayerController? player)
  {
    if (!Config.Remember || player == null || !player.IsValid || player.IsBot)
      return null;

    string? name;
    lock (_saveLock)
    {
      if (!_saved.TryGetValue(player.SteamID, out name))
        return null;
    }

    var preset = FindPreset(name);
    return preset != null && Util.HasAccess(player, preset.Flag) ? preset : null;
  }

  private void Remember(CCSPlayerController player, PostprocessingPreset? preset)
  {
    if (!Config.Remember || !player.IsValid || player.IsBot)
      return;

    lock (_saveLock)
    {
      if (preset == null)
        _saved.Remove(player.SteamID);
      else
        _saved[player.SteamID] = preset.Name;
    }

    SaveAsync();
  }

  private void LoadSaved()
  {
    try
    {
      if (!File.Exists(SavePath))
        return;

      var stored = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(SavePath));
      if (stored == null)
        return;

      foreach (var (key, value) in stored)
      {
        if (ulong.TryParse(key, out var steamId))
          _saved[steamId] = value;
      }
    }
    catch
    {
    }
  }

  private void SaveAsync()
  {
    Dictionary<string, string> snapshot;
    lock (_saveLock)
      snapshot = _saved.ToDictionary(entry => entry.Key.ToString(), entry => entry.Value);

    var path = SavePath;
    Task.Run(() =>
    {
      try
      {
        lock (_saveLock)
          File.WriteAllText(path, JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }));
      }
      catch
      {
      }
    });
  }

  private void Reply(CCSPlayerController? player, string message)
  {
    if (player == null || !player.IsValid)
    {
      Server.PrintToConsole(message);
      return;
    }

    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {message}");
  }
}
