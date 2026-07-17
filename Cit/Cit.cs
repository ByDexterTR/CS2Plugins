using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using ByDexter.Shared;

public class CitConfig : BasePluginConfig
{
  [JsonPropertyName("menu_cmd")]
  public string MenuCommands { get; set; } = "css_cit,css_barikat";

  [JsonPropertyName("menu_flag")]
  public string MenuFlag { get; set; } = "@jailbreak/warden,@css/generic";
}

public class Cit : BasePlugin, IPluginConfig<CitConfig>
{
  public override string ModuleName => "Cit";
  public override string ModuleVersion => "1.0.8";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  private enum FenceType
  {
    Fence,
    Cover
  }

  private FenceType SelectedFenceType = FenceType.Fence;

  private static readonly Dictionary<string, (string FenceModel, string CoverModel, string Label, float Offset)> FenceOptions = new()
  {
      { "64x128", ("models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_64_capped.vmdl", "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_cover_001_64.vmdl", "Küçük", 32f) },
      { "128x128", ("models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_128_capped.vmdl", "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_cover_001_128.vmdl", "Orta", 64f) },
      { "256x128", ("models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_256_capped.vmdl", "models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_cover_001_256.vmdl", "Büyük", 128f) }
  };

  private string SelectedFenceSize = "128x128";
  private const string FenceName = "bydexter_pluginfence";

  public CitConfig Config { get; set; } = new();

  private WasdMenuManager _menus = null!;

  public void OnConfigParsed(CitConfig config)
  {
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    _menus = new WasdMenuManager(this,
      () => Localizer["menu.scroll"],
      () => Localizer["menu.select"],
      () => Localizer["menu.exit"]);
    RegisterListener<OnServerPrecacheResources>(OnServerPrecacheResources);
    RegisterListener<OnMapEnd>(RemoveAllModel);

    foreach (var name in Util.Split(Config.MenuCommands))
      AddCommand(name, "Cit menusunu acar", OnCitCommand);
  }

  public override void Unload(bool hotReload)
  {
    _menus.Clear();
    RemoveAllModel();
  }

  public static void OnServerPrecacheResources(ResourceManifest resource)
  {
    foreach (var model in FenceOptions.Values)
    {
      resource.AddResource(model.FenceModel);
      resource.AddResource(model.CoverModel);
    }
  }

  public void OnCitCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid || !IsAlive(player))
      return;

    if (!Util.HasAccess(player, Config.MenuFlag))
      return;

    ShowFenceMenu(player);
  }

  private void ShowFenceMenu(CCSPlayerController? player)
  {
    if (player == null || !player.IsValid || !IsAlive(player))
      return;

    var keys = FenceOptions.Keys.ToList();

    var sizeLabel = SelectedFenceSize == "64x128" ? Localizer["cit.size_small"].ToString()
                    : SelectedFenceSize == "256x128" ? Localizer["cit.size_large"].ToString()
                    : Localizer["cit.size_medium"].ToString();
    var typeLabel = SelectedFenceType == FenceType.Fence ? Localizer["cit.type_fence"].ToString() : Localizer["cit.type_cover"].ToString();

    var items = new List<WasdItem>
    {
      new()
      {
        Text = Localizer["cit.create", sizeLabel, typeLabel],
        OnSelect = p =>
        {
          var modelPath = SelectedFenceType == FenceType.Fence ? FenceOptions[SelectedFenceSize].FenceModel : FenceOptions[SelectedFenceSize].CoverModel;
          CreateModel(p, modelPath);
        }
      },
      new()
      {
        Text = Localizer["cit.change_type"],
        OnSelect = p =>
        {
          SelectedFenceType = SelectedFenceType == FenceType.Fence ? FenceType.Cover : FenceType.Fence;
          ShowFenceMenu(p);
        }
      },
      new()
      {
        Text = Localizer["cit.change_size"],
        OnSelect = p =>
        {
          int idx = keys.IndexOf(SelectedFenceSize);
          idx = (idx + 1) % keys.Count;
          SelectedFenceSize = keys[idx];
          ShowFenceMenu(p);
        }
      },
      new() { Text = Localizer["cit.delete_aimed"], OnSelect = p => RemoveAimModel(p) },
      new() { Text = Localizer["cit.delete_all"], OnSelect = _ => RemoveAllModel() }
    };

    _menus.Open(player, Localizer["cit.menu_title"], items);
  }

  private void CreateModel(CCSPlayerController? player, string modelpath)
  {
    if (player == null || !player.IsValid)
      return;

    var pawn = player.PlayerPawn.Value as CCSPlayerPawn;
    if (pawn == null)
      return;

    var hit = NativeTrace.TraceFromEyes(pawn);
    if (hit == null || hit.Value.Length() == 0)
    {
      player.PrintToChat(NativeTrace.LastError != null
        ? $" \x0E{ChatPrefix}\x01 {Localizer["cit.trace_unavailable", NativeTrace.LastError]}"
        : $" \x0E{ChatPrefix}\x01 {Localizer["cit.no_hit"]}");
      return;
    }

    var spawnPos = hit.Value;
    var angles = pawn.EyeAngles;
    angles.X = 0;
    angles.Z = 0;

    double yawRad = (angles.Y + 90) * Math.PI / 180.0;
    var right = new Vector((float)Math.Cos(yawRad), (float)Math.Sin(yawRad), 0);

    float offset = -FenceOptions[SelectedFenceSize].Offset;
    spawnPos += new System.Numerics.Vector3(right.X * offset, right.Y * offset, 0);

    var model = Utilities.CreateEntityByName<CPhysicsPropOverride>("prop_physics_override");
    if (model == null || model.Entity == null || !model.IsValid)
    {
      return;
    }
    model.Entity.Name = FenceName;
    model.DispatchSpawn();
    model.SetModel(modelpath);

    model.AcceptInput("DisableMotion");
    model.Teleport(new Vector(spawnPos.X, spawnPos.Y, spawnPos.Z), angles, Vector.Zero);
  }

  private static void RemoveAllModel()
  {
    var allProps = Utilities.FindAllEntitiesByDesignerName<CPhysicsPropOverride>("prop_physics_override");
    foreach (var prop in allProps)
    {
      if (prop.Entity != null && prop.IsValid && !string.IsNullOrEmpty(prop.Entity.Name) && string.Equals(prop.Entity.Name, FenceName, StringComparison.Ordinal))
        prop.Remove();
    }
  }

  private static void RemoveAimModel(CCSPlayerController? player)
  {
    if (player == null || !player.IsValid)
      return;

    var pawn = player.PlayerPawn.Value as CCSPlayerPawn;
    if (pawn == null)
      return;

    var hit = NativeTrace.TraceFromEyes(pawn);
    if (hit == null)
    {
      return;
    }

    var hitPos = hit.Value;

    float bestDist2 = float.MaxValue;
    CPhysicsPropOverride? best = null;

    foreach (var prop in Utilities.FindAllEntitiesByDesignerName<CPhysicsPropOverride>("prop_physics_override"))
    {
      if (prop?.Entity == null) continue;
      if (!string.Equals(prop.Entity.Name, FenceName, StringComparison.Ordinal)) continue;

      var p = prop.AbsOrigin;
      if (p == null) continue;
      float dx = p.X - hitPos.X;
      float dy = p.Y - hitPos.Y;
      float dz = p.Z - hitPos.Z;
      float d2 = dx * dx + dy * dy + dz * dz;

      if (d2 < bestDist2)
      {
        bestDist2 = d2;
        best = prop;
      }
    }

    const float maxPickDist2 = 256f * 256f;
    if (best != null && bestDist2 <= maxPickDist2)
    {
      best.Remove();
    }
  }

  static bool IsAlive(CCSPlayerController? player)
  {
    return player?.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
  }
}