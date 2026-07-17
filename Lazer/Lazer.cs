using System.Drawing;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;
using ByDexter.Shared;

namespace Lazer;

public class LazerConfig : BasePluginConfig
{
  [JsonPropertyName("lazer_cmd")]
  public string Commands { get; set; } = "css_lazer,css_laser";

  [JsonPropertyName("lazer_default_on")]
  public bool DefaultOn { get; set; } = true;

  [JsonPropertyName("lazer_width")]
  public float Width { get; set; } = 0.4f;

  [JsonPropertyName("lazer_max_distance")]
  public float MaxDistance { get; set; } = 8192f;

  [JsonPropertyName("lazer_t_color")]
  public string TColor { get; set; } = "234 210 139";

  [JsonPropertyName("lazer_ct_color")]
  public string CTColor { get; set; } = "182 212 238";
}

public class Lazer : BasePlugin, IPluginConfig<LazerConfig>
{
  public override string ModuleName => "Lazer";
  public override string ModuleVersion => "1.0.1";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  private string ChatPrefix => Localizer["chat_prefix"];

  public LazerConfig Config { get; set; } = new();

  private const int MaxSlots = 64;
  private const string BeamSprite = "materials/sprites/laserbeam.vmat";

  private readonly CEnvBeam?[] _beams = new CEnvBeam?[MaxSlots];
  private readonly byte[] _beamTeam = new byte[MaxSlots];
  private readonly System.Numerics.Vector3[] _lastStart = new System.Numerics.Vector3[MaxSlots];
  private readonly System.Numerics.Vector3[] _lastEnd = new System.Numerics.Vector3[MaxSlots];
  private readonly bool[] _jitterFlip = new bool[MaxSlots];
  private readonly bool[] _off = new bool[MaxSlots];

  private Color _tColor = Color.Red;
  private Color _ctColor = Color.DeepSkyBlue;

  public void OnConfigParsed(LazerConfig config)
  {
    if (config.Width < 0.1f)
      config.Width = 0.1f;
    if (config.MaxDistance < 256f)
      config.MaxDistance = 256f;
    Config = config;
    _tColor = ParseColor(config.TColor);
    _ctColor = ParseColor(config.CTColor);
  }

  public override void Load(bool hotReload)
  {
    foreach (var name in Config.Commands.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
      AddCommand(name, "Lazer toggle", OnToggleCommand);

    RegisterListener<OnClientPutInServer>(slot =>
    {
      if (slot >= 0 && slot < MaxSlots)
        _off[slot] = !Config.DefaultOn;
    });

    RegisterListener<OnTick>(OnTick);
    RegisterListener<CheckTransmit>(OnCheckTransmit);

    RegisterEventHandler<EventPlayerDeath>((ev, _) =>
    {
      int slot = ev.Userid?.Slot ?? -1;
      if (slot >= 0 && slot < MaxSlots)
        RemoveBeam(slot);
      return HookResult.Continue;
    });

    RegisterEventHandler<EventPlayerDisconnect>((ev, _) =>
    {
      int slot = ev.Userid?.Slot ?? -1;
      if (slot >= 0 && slot < MaxSlots)
      {
        RemoveBeam(slot);
        _off[slot] = !Config.DefaultOn;
      }
      return HookResult.Continue;
    });

    RegisterEventHandler<EventRoundStart>((_, _) =>
    {
      RemoveAllBeams();
      return HookResult.Continue;
    });

    RegisterEventHandler<EventRoundEnd>((_, _) =>
    {
      RemoveAllBeams();
      return HookResult.Continue;
    });
  }

  public override void Unload(bool hotReload)
  {
    RemoveAllBeams();
  }

  private void OnToggleCommand(CCSPlayerController? player, CommandInfo info)
  {
    if (player == null || !player.IsValid)
      return;

    _off[player.Slot] = !_off[player.Slot];
    string key = _off[player.Slot] ? "lazer.disabled" : "lazer.enabled";
    player.PrintToChat($" {CC.Orchid}{ChatPrefix}{CC.Default} {Localizer[key]}");
  }

  private void OnTick()
  {
    bool anyViewer = false;
    for (int slot = 0; slot < MaxSlots; slot++)
    {
      var viewer = Utilities.GetPlayerFromSlot(slot);
      if (viewer != null && viewer.IsValid && !viewer.IsHLTV && !_off[slot] && !IsAlive(viewer))
      {
        anyViewer = true;
        break;
      }
    }

    for (int slot = 0; slot < MaxSlots; slot++)
    {
      var player = Utilities.GetPlayerFromSlot(slot);
      var pawn = player?.PlayerPawn.Value;
      if (!anyViewer || player == null || pawn == null || !pawn.IsValid || pawn.AbsOrigin == null
          || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
      {
        RemoveBeam(slot);
        continue;
      }

      var angles = pawn.EyeAngles;
      float ry = angles.Y * MathF.PI / 180f;
      float rp = angles.X * MathF.PI / 180f;

      float fx = MathF.Cos(rp) * MathF.Cos(ry);
      float fy = MathF.Cos(rp) * MathF.Sin(ry);
      float fz = -MathF.Sin(rp);

      var origin = pawn.AbsOrigin;
      var eye = new System.Numerics.Vector3(origin.X, origin.Y, origin.Z + pawn.ViewOffset.Z);
      var forward = new System.Numerics.Vector3(fx, fy, fz);
      var traceEnd = eye + forward * Config.MaxDistance;

      var end = NativeTrace.TraceLine(pawn, eye, traceEnd) ?? traceEnd;

      byte team = player.TeamNum;
      var beam = _beams[slot];
      if (beam != null && beam.IsValid && _beamTeam[slot] != team)
      {
        RemoveBeam(slot);
        beam = null;
      }

      if (beam == null || !beam.IsValid)
      {
        var color = team == (byte)CsTeam.CounterTerrorist ? _ctColor : _tColor;
        beam = CreateBeam(color, eye, end);
        _beams[slot] = beam;
        _beamTeam[slot] = team;
        _lastStart[slot] = eye;
        _lastEnd[slot] = end;
        continue;
      }

      if (System.Numerics.Vector3.DistanceSquared(_lastStart[slot], eye) < 0.01f
          && System.Numerics.Vector3.DistanceSquared(_lastEnd[slot], end) < 0.25f)
        continue;

      _jitterFlip[slot] = !_jitterFlip[slot];
      float jitter = _jitterFlip[slot] ? 0.008f : -0.008f;

      beam.Teleport(new Vector(eye.X, eye.Y, eye.Z + jitter), new QAngle(), new Vector());
      beam.EndPos.X = end.X;
      beam.EndPos.Y = end.Y;
      beam.EndPos.Z = end.Z;
      Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");

      _lastStart[slot] = eye;
      _lastEnd[slot] = end;
    }
  }

  private void OnCheckTransmit(CCheckTransmitInfoList infoList)
  {
    foreach ((CCheckTransmitInfo info, CCSPlayerController? viewer) in infoList)
    {
      if (viewer == null || !viewer.IsValid)
        continue;

      if (!viewer.IsHLTV && !_off[viewer.Slot] && !IsAlive(viewer))
        continue;

      for (int slot = 0; slot < MaxSlots; slot++)
      {
        var beam = _beams[slot];
        if (beam != null && beam.IsValid)
          info.TransmitEntities.Remove(beam);
      }
    }
  }

  private CEnvBeam? CreateBeam(Color color, System.Numerics.Vector3 start, System.Numerics.Vector3 end)
  {
    var beam = Utilities.CreateEntityByName<CEnvBeam>("env_beam");
    if (beam == null || !beam.IsValid)
      return null;

    beam.DispatchSpawn();
    beam.AcceptInput("TurnOn");

    beam.SetModel(BeamSprite);
    beam.Width = Config.Width;
    Utilities.SetStateChanged(beam, "CBeam", "m_fWidth");
    beam.Render = color;
    Utilities.SetStateChanged(beam, "CBaseModelEntity", "m_clrRender");
    beam.Teleport(new Vector(start.X, start.Y, start.Z), new QAngle(), new Vector());
    beam.EndPos.X = end.X;
    beam.EndPos.Y = end.Y;
    beam.EndPos.Z = end.Z;
    Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");
    return beam;
  }

  private void RemoveBeam(int slot)
  {
    var beam = _beams[slot];
    _beams[slot] = null;
    if (beam != null && beam.IsValid)
      beam.Remove();
  }

  private void RemoveAllBeams()
  {
    for (int slot = 0; slot < MaxSlots; slot++)
      RemoveBeam(slot);
  }

  private static bool IsAlive(CCSPlayerController player)
  {
    return player.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
  }

  private static Color ParseColor(string value)
  {
    if (!string.IsNullOrWhiteSpace(value))
    {
      if (value.StartsWith('#') && value.Length == 7
          && int.TryParse(value.AsSpan(1, 2), System.Globalization.NumberStyles.HexNumber, null, out var hr)
          && int.TryParse(value.AsSpan(3, 2), System.Globalization.NumberStyles.HexNumber, null, out var hg)
          && int.TryParse(value.AsSpan(5, 2), System.Globalization.NumberStyles.HexNumber, null, out var hb))
        return Color.FromArgb(255, hr, hg, hb);

      var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length == 3 && int.TryParse(parts[0], out var r) && int.TryParse(parts[1], out var g) && int.TryParse(parts[2], out var b))
        return Color.FromArgb(255, Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255));
    }

    return Color.White;
  }
}
