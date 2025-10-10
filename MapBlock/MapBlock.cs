using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using static CounterStrikeSharp.API.Core.Listeners;
using CssVector = CounterStrikeSharp.API.Modules.Utils.Vector;

public class MapBlockConfig : BasePluginConfig
{
	[JsonPropertyName("mapblock_mode")]
	public int MapBlockMode { get; set; } = 1;

	[JsonPropertyName("mapblock_count")]
	public int MapBlockCount { get; set; } = 5;
}

public class MapBlock : BasePlugin, IPluginConfig<MapBlockConfig>
{
	public override string ModuleName => "MapBlock";
	public override string ModuleVersion => "1.0.0";
	public override string ModuleAuthor => "ByDexter";
	public override string ModuleDescription => "B Blok";

	private const string FenceName = "bydexter_mapblock";
	private const string SaveFileName = "MapBlock.json";

	private static readonly string[] PrecacheModels =
	{
				"models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_64_capped.vmdl",
				"models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_128_capped.vmdl",
				"models/props/de_nuke/hr_nuke/chainlink_fence_001/chainlink_fence_001_256_capped.vmdl"
		};

	private Dictionary<string, List<FencePlacementRecord>> _persistedPlacements = new(StringComparer.OrdinalIgnoreCase);

	private string SaveFilePath => Path.Combine(ModuleDirectory, SaveFileName);

	public MapBlockConfig Config { get; set; } = new();

	public void OnConfigParsed(MapBlockConfig config)
	{
		Config = config;
	}

	private class FencePlacementRecord
	{
		public string Model { get; set; } = string.Empty;
		public float[] Origin { get; set; } = Array.Empty<float>();
		public float[] Angles { get; set; } = Array.Empty<float>();
	}

	private class FencePlacement
	{
		public FencePlacement(string modelPath, Vector3 position, QAngle angles)
		{
			ModelPath = modelPath;
			Position = position;
			Angles = angles;
		}

		public string ModelPath { get; }
		public Vector3 Position { get; }
		public QAngle Angles { get; }
	}

	public override void Load(bool hotReload)
	{
		LoadPersistedPlacements();
		RegisterListener<OnServerPrecacheResources>(OnServerPrecacheResources);
		RegisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Post);
		RegisterEventHandler<EventRoundEnd>(OnRoundEnd, HookMode.Post);
	}

	public static void OnServerPrecacheResources(ResourceManifest resource)
	{
		foreach (var model in PrecacheModels)
		{
			resource.AddResource(model);
		}
	}

	private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
	{
		LoadPersistedPlacements();
		RemoveActiveEntities();
		if (ShouldSpawnThisRound()) SpawnSavedPlacementsForCurrentMap();
		return HookResult.Continue;
	}

	private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
	{
		RemoveActiveEntities();
		return HookResult.Continue;
	}

	private bool ShouldSpawnThisRound()
	{
		int mode = Config.MapBlockMode;
		if (mode < 0 || mode > 2)
		{
			Logger.LogWarning("[MapBlock] Geçersiz mapblock_mode: {Mode}. 1 olarak varsayıldı.", mode);
			mode = 1;
		}

		if (mode == 0) return false;

		int threshold = Math.Max(0, Config.MapBlockCount);
		if (threshold == 0) return true;

		var players = Utilities.GetPlayers();
		int count = mode == 1 ? players.Count(p => p != null && p.IsValid && p.Team == CsTeam.CounterTerrorist) : players.Count(p => p != null && p.IsValid && (p.Team == CsTeam.CounterTerrorist || p.Team == CsTeam.Terrorist));

		if (count >= threshold) return false;

		return true;
	}

	private void SpawnSavedPlacementsForCurrentMap()
	{
		var mapName = GetCurrentMapName();
		if (string.IsNullOrWhiteSpace(mapName)) return;

		if (!_persistedPlacements.TryGetValue(mapName, out var records) || records == null || records.Count == 0) return;

		foreach (var record in records)
		{
			var placement = FromRecord(record);
			if (placement == null) continue;
			TrySpawnPlacement(placement);
		}
	}

	private bool TrySpawnPlacement(FencePlacement placement)
	{
		var model = Utilities.CreateEntityByName<CPhysicsPropOverride>("prop_physics_override");
		if (model == null || model.Entity == null || !model.IsValid)
		{
			Logger.LogWarning("[MapBlock] {Model} modeli için prop oluşturulamadı.", placement.ModelPath);
			return false;
		}

		model.Entity.Name = FenceName;
		model.DispatchSpawn();
		model.SetModel(placement.ModelPath);
		model.AcceptInput("DisableMotion");

		var position = placement.Position;
		model.Teleport(new CssVector(position.X, position.Y, position.Z), placement.Angles, CssVector.Zero);
		return true;
	}

	private void RemoveActiveEntities()
	{
		foreach (var prop in Utilities.FindAllEntitiesByDesignerName<CPhysicsPropOverride>("prop_physics_override"))
		{
			if (prop?.Entity == null || !prop.IsValid) continue;
			if (!string.Equals(prop.Entity.Name, FenceName, StringComparison.Ordinal)) continue;
			prop.Remove();
		}
	}

	private void LoadPersistedPlacements()
	{
		try
		{
			if (!File.Exists(SaveFilePath))
			{
				_persistedPlacements = new Dictionary<string, List<FencePlacementRecord>>(StringComparer.OrdinalIgnoreCase);
				return;
			}

			var json = File.ReadAllText(SaveFilePath);
			var data = JsonSerializer.Deserialize<Dictionary<string, List<FencePlacementRecord>>>(json);

			_persistedPlacements = data != null ? new Dictionary<string, List<FencePlacementRecord>>(data, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, List<FencePlacementRecord>>(StringComparer.OrdinalIgnoreCase);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "[MapBlock] {Path} dosyasından kayıtlar yüklenemedi.", SaveFilePath);
			_persistedPlacements = new Dictionary<string, List<FencePlacementRecord>>(StringComparer.OrdinalIgnoreCase);
		}
	}

	private FencePlacement? FromRecord(FencePlacementRecord record)
	{
		if (record == null || string.IsNullOrWhiteSpace(record.Model)) return null;

		if (record.Origin.Length < 3 || record.Angles.Length < 3) return null;

		var position = new Vector3(record.Origin[0], record.Origin[1], record.Origin[2]);
		var angles = new QAngle(record.Angles[0], record.Angles[1], record.Angles[2]);

		return new FencePlacement(record.Model, position, angles);
	}

	private string GetCurrentMapName()
	{
		return Server.MapName ?? string.Empty;
	}
}