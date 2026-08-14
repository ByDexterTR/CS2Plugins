using System.Text;

namespace ShowPlayerClips.Source2;

public sealed class VpkArchive : IDisposable
{
  private const uint Signature = 0x55AA1234;
  private const ushort InlineArchive = 0x7FFF;

  private readonly record struct Entry(ushort ArchiveIndex, uint Offset, uint Length, byte[] Preload);

  private readonly string _path;
  private readonly Stream _stream;
  private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);
  private readonly Dictionary<ushort, FileStream> _archives = [];
  private readonly long _dataStart;

  public VpkArchive(string path)
    : this(path, File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
  {
  }

  public VpkArchive(byte[] data)
    : this(string.Empty, new MemoryStream(data, writable: false))
  {
  }

  private VpkArchive(string path, Stream stream)
  {
    _path = path;
    _stream = stream;

    using var reader = new BinaryReader(_stream, Encoding.UTF8, leaveOpen: true);

    uint signature = reader.ReadUInt32();
    if (signature != Signature)
      throw new InvalidDataException($"'{Path.GetFileName(path)}' bir VPK dosyasi degil.");

    uint version = reader.ReadUInt32();
    uint treeSize = reader.ReadUInt32();
    long headerSize = 12;

    if (version == 2)
    {
      reader.ReadUInt32();
      reader.ReadUInt32();
      reader.ReadUInt32();
      reader.ReadUInt32();
      headerSize = 28;
    }
    else if (version != 1)
    {
      throw new InvalidDataException($"Desteklenmeyen VPK surumu: {version}");
    }

    _dataStart = headerSize + treeSize;

    while (true)
    {
      string extension = ReadString(reader);
      if (extension.Length == 0)
        break;

      while (true)
      {
        string directory = ReadString(reader);
        if (directory.Length == 0)
          break;

        while (true)
        {
          string name = ReadString(reader);
          if (name.Length == 0)
            break;

          reader.ReadUInt32();
          ushort preloadBytes = reader.ReadUInt16();
          ushort archiveIndex = reader.ReadUInt16();
          uint entryOffset = reader.ReadUInt32();
          uint entryLength = reader.ReadUInt32();
          reader.ReadUInt16();

          byte[] preload = preloadBytes > 0 ? reader.ReadBytes(preloadBytes) : [];

          string full = directory == " " ? $"{name}.{extension}" : $"{directory}/{name}.{extension}";
          _entries[full] = new Entry(archiveIndex, entryOffset, entryLength, preload);
        }
      }
    }
  }

  public IEnumerable<string> Files => _entries.Keys;

  public bool Contains(string file) => _entries.ContainsKey(file);

  public string? Find(string suffix)
  {
    foreach (var file in _entries.Keys)
    {
      if (file.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        return file;
    }

    return null;
  }

  public byte[]? Read(string file)
  {
    if (!_entries.TryGetValue(file, out var entry))
      return null;

    byte[] data = new byte[entry.Preload.Length + entry.Length];
    entry.Preload.CopyTo(data, 0);

    if (entry.Length > 0)
    {
      var source = GetStream(entry.ArchiveIndex, out long baseOffset);
      source.Seek(baseOffset + entry.Offset, SeekOrigin.Begin);
      source.ReadExactly(data, entry.Preload.Length, (int)entry.Length);
    }

    return data;
  }

  private Stream GetStream(ushort archiveIndex, out long baseOffset)
  {
    if (archiveIndex == InlineArchive || _path.Length == 0)
    {
      baseOffset = _dataStart;
      return _stream;
    }

    baseOffset = 0;

    if (_archives.TryGetValue(archiveIndex, out var cached))
      return cached;

    string directory = Path.GetDirectoryName(_path) ?? ".";
    string name = Path.GetFileNameWithoutExtension(_path);
    if (name.EndsWith("_dir", StringComparison.OrdinalIgnoreCase))
      name = name[..^4];

    string archivePath = Path.Combine(directory, $"{name}_{archiveIndex:D3}.vpk");
    var stream = File.Open(archivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    _archives[archiveIndex] = stream;
    return stream;
  }

  private static string ReadString(BinaryReader reader)
  {
    var bytes = new List<byte>(32);

    while (true)
    {
      byte b = reader.ReadByte();
      if (b == 0)
        break;
      bytes.Add(b);
    }

    return Encoding.UTF8.GetString(bytes.ToArray());
  }

  public void Dispose()
  {
    _stream.Dispose();
    foreach (var archive in _archives.Values)
      archive.Dispose();
    _archives.Clear();
  }
}
