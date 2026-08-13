using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace Ads;

public partial class Ads
{
  private class PlacedAd
  {
    public CBaseEntity? Entity;
    public string Flag = "";
    public string IgnoreFlag = "";
    public int Index;
  }

  private readonly List<PlacedAd> _entities = new();

  private const byte FSolidNotSolid = 4;
  private const byte CollisionGroupDebris = 2;

  private void SpawnWorldAds()
  {
    RemoveWorldAds();

    string map = string.IsNullOrEmpty(_mapName) ? Server.MapName : _mapName;

    for (int i = 0; i < _data.Props.Count; i++)
    {
      var ad = _data.Props[i];
      if (string.IsNullOrWhiteSpace(ad.Path) || !MapMatches(ad.Map, map))
        continue;

      var entity = CreateProp(ad);
      if (entity != null)
        _entities.Add(new PlacedAd { Entity = entity, Flag = ad.Flag ?? "", IgnoreFlag = ad.IgnoreFlag ?? "", Index = i });
    }

  }

  private void RemoveWorldAds()
  {
    foreach (var placed in _entities)
    {
      if (placed.Entity != null && placed.Entity.IsValid)
        placed.Entity.Remove();
    }
    _entities.Clear();

    foreach (var prop in Utilities.FindAllEntitiesByDesignerName<CDynamicProp>("prop_dynamic_override"))
    {
      if (prop.Entity != null && prop.IsValid && string.Equals(prop.Entity.Name, EntityName, StringComparison.Ordinal))
        prop.Remove();
    }

  }

  private CDynamicProp? CreateProp(PropAd ad)
  {
    try
    {
      var entity = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
      if (entity == null || !entity.IsValid || entity.Entity == null)
        return null;

      using var keyValues = new CEntityKeyValues();
      keyValues.SetString("targetname", EntityName);
      keyValues.SetString("model", ad.Path);
      keyValues.SetInt("solid", ad.Solid ? 6 : 0);
      keyValues.SetFloat("modelscale", ad.Scale <= 0f ? 1f : ad.Scale);

      entity.Entity.Name = EntityName;
      entity.DispatchSpawn(keyValues);
      entity.SetModel(ad.Path);

      if (ad.Skin != 0)
        entity.AcceptInput("Skin", entity, entity, ad.Skin.ToString());

      if (!ad.Solid)
        MakeNonSolid(entity);

      entity.Teleport(ParseVector(ad.Pos), ParseAngle(ad.Angle), Vector.Zero);

      return entity;
    }
    catch (Exception ex)
    {
      Logger.LogError("Prop olusturulamadi ({path}): {message}", ad.Path, ex.Message);
      return null;
    }
  }

  private static void MakeNonSolid(CBaseEntity entity)
  {
    var collision = entity.Collision;
    if (collision == null)
      return;

    collision.SolidType = SolidType_t.SOLID_NONE;
    collision.SolidFlags = FSolidNotSolid;
    collision.CollisionGroup = CollisionGroupDebris;
    collision.CollisionAttribute.CollisionGroup = CollisionGroupDebris;
    collision.CollisionAttribute.CollisionFunctionMask = 0;
  }

  private static Vector ParseVector(string value)
  {
    var parts = Split3(value);
    return new Vector(parts[0], parts[1], parts[2]);
  }

  private static QAngle ParseAngle(string value)
  {
    var parts = Split3(value);
    return new QAngle(parts[0], parts[1], parts[2]);
  }

  private static float[] Split3(string value)
  {
    var result = new float[3];
    if (string.IsNullOrWhiteSpace(value))
      return result;

    var parts = value.Replace(',', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    for (int i = 0; i < 3 && i < parts.Length; i++)
      float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out result[i]);

    return result;
  }

  private static string RotateAxis(string angle, int axis, float delta)
  {
    var parts = Split3(angle);
    parts[axis] = Step(parts[axis], delta);
    return FormatVector(parts[0], parts[1], parts[2]);
  }

  private static string MoveAxis(string pos, int axis, float delta)
  {
    var parts = Split3(pos);
    parts[axis] += delta;
    return FormatVector(parts[0], parts[1], parts[2]);
  }

  private static float Step(float value, float delta)
  {
    float step = MathF.Abs(delta);
    if (step <= 0f)
      return Wrap(value);

    float slot = value / step;
    float target = delta > 0f
      ? (MathF.Floor(slot + 0.001f) + 1f) * step
      : (MathF.Ceiling(slot - 0.001f) - 1f) * step;

    return Wrap(target);
  }

  private static float Wrap(float value)
  {
    value %= 360f;
    return value < 0f ? value + 360f : value;
  }

  private static string FormatVector(float x, float y, float z) =>
    string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1:0.##} {2:0.##}", x, y, z);
}
