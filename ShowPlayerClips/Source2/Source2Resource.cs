using System.Text;

namespace ShowPlayerClips.Source2;

public static class Source2Resource
{
  public readonly record struct Block(string Type, int Offset, int Size);

  public static List<Block> ReadBlocks(byte[] data)
  {
    var blocks = new List<Block>();
    if (data.Length < 16)
      return blocks;

    int blockOffset = BitConverter.ToInt32(data, 8);
    int blockCount = BitConverter.ToInt32(data, 12);

    int position = 8 + blockOffset;

    for (int i = 0; i < blockCount; i++)
    {
      if (position + 12 > data.Length)
        break;

      string type = Encoding.ASCII.GetString(data, position, 4);
      int offset = BitConverter.ToInt32(data, position + 4);
      int size = BitConverter.ToInt32(data, position + 8);

      blocks.Add(new Block(type, position + 4 + offset, size));
      position += 12;
    }

    return blocks;
  }

  public static Block? FindBlock(byte[] data, string type)
  {
    foreach (var block in ReadBlocks(data))
    {
      if (block.Type == type)
        return block;
    }

    return null;
  }
}
