using System.Numerics;

namespace FPS.Visibility;

public static class NavMesh
{
  private const uint Magic = 0xFEEDFACE;
  private const uint Kv3Magic = 0x4B563300;
  private const int Kv3HeaderV5 = 120;

  public static List<Vector3[]>? Polygons(byte[]? data)
  {
    if (data == null || data.Length < 24)
      return null;

    try
    {
      using var reader = new BinaryReader(new MemoryStream(data));
      if (reader.ReadUInt32() != Magic)
        return null;

      uint version = reader.ReadUInt32();
      if (version < 31 || version > 40)
        return null;

      reader.ReadUInt32();
      reader.ReadUInt32();

      if ((PeekUInt32(reader) & 0xFFFFFF00) == Kv3Magic && !SkipKv3(reader))
        return null;

      int cornerCount = reader.ReadInt32();
      if (cornerCount <= 0 || cornerCount > data.Length / 12)
        return null;

      var corners = new Vector3[cornerCount];
      for (int i = 0; i < cornerCount; i++)
        corners[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

      int polygonCount = reader.ReadInt32();
      if (polygonCount <= 0 || polygonCount > data.Length / 5)
        return null;

      var polygons = new List<Vector3[]>(polygonCount);
      for (int i = 0; i < polygonCount; i++)
      {
        int count = reader.ReadByte();
        var polygon = new Vector3[count];
        for (int k = 0; k < count; k++)
        {
          int index = reader.ReadInt32();
          if ((uint)index >= (uint)cornerCount)
            return null;

          polygon[k] = corners[index];
        }

        if (version >= 35)
          reader.ReadUInt32();

        if (count >= 3)
          polygons.Add(polygon);
      }

      return polygons;
    }
    catch (EndOfStreamException)
    {
      return null;
    }
  }

  private static uint PeekUInt32(BinaryReader reader)
  {
    long position = reader.BaseStream.Position;
    uint value = reader.ReadUInt32();
    reader.BaseStream.Position = position;
    return value;
  }

  private static bool SkipKv3(BinaryReader reader)
  {
    long start = reader.BaseStream.Position;
    if ((reader.ReadUInt32() & 0xFF) != 5)
      return false;

    reader.BaseStream.Position = start + 52;
    int compressed = reader.ReadInt32();
    long end = start + Kv3HeaderV5 + compressed;
    if (compressed < 0 || end > reader.BaseStream.Length)
      return false;

    reader.BaseStream.Position = end;
    return true;
  }
}
