using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using static CounterStrikeSharp.API.Core.Listeners;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using CS2TraceRay.Class;
using CS2TraceRay.Struct;
using CS2TraceRay.Enum;

public class Cit : BasePlugin
{
  public override string ModuleName => "Cit";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "Çit";

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

  public override void Load(bool hotReload)
  {
    RegisterListener<OnServerPrecacheResources>(OnServerPrecacheResources);
  }

  public static void OnServerPrecacheResources(ResourceManifest resource)
  {
    foreach (var model in FenceOptions.Values)
    {
      resource.AddResource(model.FenceModel);
      resource.AddResource(model.CoverModel);
    }
  }

  [ConsoleCommand("css_cit", "css_cit")]
  [RequiresPermissionsOr("@css/root", "@jailbreak/warden")]
  public void OnCitCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid || !IsAlive(player))
      return;
    
    ShowFenceMenu(player);
  }

  private void ShowFenceMenu(CCSPlayerController? player)
  {
    if (player == null || !player.IsValid || !IsAlive(player))
      return;

    var keys = FenceOptions.Keys.ToList();
    CenterHtmlMenu menu = new("Çit Menüsü", this);

    menu.AddMenuOption($"Oluştur: [{FenceOptions[SelectedFenceSize].Label}] [{(SelectedFenceType == FenceType.Fence ? "Çit" : "Barikat")}]", (player, option) =>
    {
      var modelPath = SelectedFenceType == FenceType.Fence ? FenceOptions[SelectedFenceSize].FenceModel : FenceOptions[SelectedFenceSize].CoverModel;
      CreateModel(player, modelPath);
    });

    menu.AddMenuOption("Türü Değiştir", (player, option) =>
    {
      SelectedFenceType = SelectedFenceType == FenceType.Fence ? FenceType.Cover : FenceType.Fence;
      ShowFenceMenu(player);
    });

    menu.AddMenuOption("Boyutu Değiştir", (player, option) =>
    {
      int idx = keys.IndexOf(SelectedFenceSize);
      idx = (idx + 1) % keys.Count;
      SelectedFenceSize = keys[idx];
      ShowFenceMenu(player);
    });

    menu.AddMenuOption("Çiti Sil", (player, option) =>
    {
      RemoveAimModel(player);
    });

    menu.AddMenuOption("Çitleri Sil", (player, option) =>
    {
      RemoveAllModel(player);
    });

    MenuManager.OpenCenterHtmlMenu(this, player, menu);
  }

  private void CreateModel(CCSPlayerController? player, string modelpath)
  {
    if (player == null || !player.IsValid)
      return;

    var pawn = player.PlayerPawn.Value as CCSPlayerPawn;
    if (pawn == null)
      return;

    CGameTrace? trace = TraceRay.TraceShape(
        player.GetEyePosition()!,
        pawn.EyeAngles,
        TraceMask.MaskShot,
        player
    );

    if (trace == null || !trace.HasValue || trace.Value.Position.Length() == 0)
    {
      return;
    }

    var spawnPos = trace.Value.Position;
    var angles = pawn.EyeAngles;
    angles.X = 0;
    angles.Z = 0;

    double yawRad = (angles.Y + 90) * Math.PI / 180.0;
    var right = new Vector((float)Math.Cos(yawRad), (float)Math.Sin(yawRad), 0);

    float offset = -FenceOptions[SelectedFenceSize].Offset;
    spawnPos += new System.Numerics.Vector3(right.X * offset, right.Y * offset, 0);

    var model = Utilities.CreateEntityByName<CPhysicsPropOverride>("prop_physics_override");
    if (model == null)
    {
      return;
    }

    model.DispatchSpawn();
    model.SetModel(modelpath);
    model.Entity!.Name = FenceName;
    model.AcceptInput("DisableMotion");
    model.Teleport(new Vector(spawnPos.X, spawnPos.Y, spawnPos.Z), angles, Vector.Zero);
  }

  private static void RemoveAllModel(CCSPlayerController? player)
  {
    int count = 0;
    var allProps = Utilities.FindAllEntitiesByDesignerName<CPhysicsPropOverride>("prop_physics_override");
    foreach (var prop in allProps)
    {
      if (prop?.Entity != null && string.Equals(prop.Entity.Name, FenceName, StringComparison.Ordinal))
      {
        prop.Remove();
        count++;
      }
    }
  }

  private static void RemoveAimModel(CCSPlayerController? player)
  {
    if (player == null || !player.IsValid)
      return;

    var pawn = player.PlayerPawn.Value as CCSPlayerPawn;
    if (pawn == null)
      return;

    CGameTrace? trace = TraceRay.TraceShape(
        player.GetEyePosition()!,
        pawn.EyeAngles,
        TraceMask.MaskShot,
        player
    );

    if (trace == null || !trace.HasValue)
    {
      return;
    }

    var hitPos = trace.Value.Position;

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