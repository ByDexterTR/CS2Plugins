using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Encoders;

namespace ShowPlayerClips.Source2;

public abstract class Kv3Visitor
{
  public virtual void BeginObject(string name) { }
  public virtual void EndObject() { }
  public virtual void BeginArray(string name, int count) { }
  public virtual void EndArray() { }
  public virtual void Int(string name, long value) { }
  public virtual void Real(string name, double value) { }
  public virtual void Text(string name, string value) { }
  public virtual void Bool(string name, bool value) { }
  public virtual bool WantBlob(string name) => false;
  public virtual void Blob(string name, ReadOnlySpan<byte> data) { }
}

public sealed class Kv3Binary
{
  private const uint Magic0 = 0x03564B56;
  private const uint MagicBase = 0x4B563300;
  private const int CompressionFrameSize = 16384;
  private const uint Trailer = 0xFFEEDD00;

  private enum NodeType : byte
  {
    Null = 1,
    Boolean = 2,
    Int64 = 3,
    UInt64 = 4,
    Double = 5,
    String = 6,
    BinaryBlob = 7,
    Array = 8,
    Object = 9,
    ArrayTyped = 10,
    Int32 = 11,
    UInt32 = 12,
    BooleanTrue = 13,
    BooleanFalse = 14,
    Int64Zero = 15,
    Int64One = 16,
    DoubleZero = 17,
    DoubleOne = 18,
    Float = 19,
    Int16 = 20,
    UInt16 = 21,
    Unknown22 = 22,
    Int32AsByte = 23,
    ArrayTypeByteLength = 24,
    ArrayTypeAuxiliaryBuffer = 25,
  }

  private struct Cursor(ArraySegment<byte> segment)
  {
    private readonly byte[] _array = segment.Array ?? [];
    private int _position = segment.Offset;

    public byte U8() => _array[_position++];

    public short I16()
    {
      short value = BitConverter.ToInt16(_array, _position);
      _position += 2;
      return value;
    }

    public ushort U16()
    {
      ushort value = BitConverter.ToUInt16(_array, _position);
      _position += 2;
      return value;
    }

    public int I32()
    {
      int value = BitConverter.ToInt32(_array, _position);
      _position += 4;
      return value;
    }

    public uint U32()
    {
      uint value = BitConverter.ToUInt32(_array, _position);
      _position += 4;
      return value;
    }

    public long I64()
    {
      long value = BitConverter.ToInt64(_array, _position);
      _position += 8;
      return value;
    }

    public ulong U64()
    {
      ulong value = BitConverter.ToUInt64(_array, _position);
      _position += 8;
      return value;
    }

    public float F32()
    {
      float value = BitConverter.ToSingle(_array, _position);
      _position += 4;
      return value;
    }

    public double F64()
    {
      double value = BitConverter.ToDouble(_array, _position);
      _position += 8;
      return value;
    }

    public ReadOnlySpan<byte> Take(int count)
    {
      var span = new ReadOnlySpan<byte>(_array, _position, count);
      _position += count;
      return span;
    }

    public void Skip(int count) => _position += count;
  }

  private sealed class Buffers
  {
    public Cursor Bytes1;
    public Cursor Bytes2;
    public Cursor Bytes4;
    public Cursor Bytes8;
  }

  private sealed class State
  {
    public required int Version;
    public required string[] Strings;
    public required Cursor Types;
    public required Cursor ObjectLengths;
    public required Cursor BlobLengths;
    public required Cursor Blobs;
    public required Buffers Primary;
    public required Buffers Auxiliary;
  }

  private readonly int _version;
  private readonly string[] _strings;
  private readonly ArraySegment<byte> _types;
  private readonly ArraySegment<byte> _objectLengths;
  private readonly ArraySegment<byte> _blobLengths;
  private readonly ArraySegment<byte> _blobs;
  private readonly ArraySegment<byte>[] _primary;
  private readonly ArraySegment<byte>[] _auxiliary;

  private Kv3Binary(int version, string[] strings, ArraySegment<byte> types, ArraySegment<byte> objectLengths,
    ArraySegment<byte> blobLengths, ArraySegment<byte> blobs, ArraySegment<byte>[] primary, ArraySegment<byte>[] auxiliary)
  {
    _version = version;
    _strings = strings;
    _types = types;
    _objectLengths = objectLengths;
    _blobLengths = blobLengths;
    _blobs = blobs;
    _primary = primary;
    _auxiliary = auxiliary;
  }

  public void Walk(Kv3Visitor visitor)
  {
    var state = new State
    {
      Version = _version,
      Strings = _strings,
      Types = new Cursor(_types),
      ObjectLengths = new Cursor(_objectLengths),
      BlobLengths = new Cursor(_blobLengths),
      Blobs = new Cursor(_blobs),
      Primary = MakeBuffers(_primary),
      Auxiliary = MakeBuffers(_auxiliary),
    };

    var type = ReadType(state);
    ReadValue(state, visitor, string.Empty, type);
  }

  private static Buffers MakeBuffers(ArraySegment<byte>[] segments) => new()
  {
    Bytes1 = new Cursor(segments[0]),
    Bytes2 = new Cursor(segments[1]),
    Bytes4 = new Cursor(segments[2]),
    Bytes8 = new Cursor(segments[3]),
  };

  private static NodeType ReadType(State state)
  {
    byte databyte = state.Types.U8();

    if (state.Version >= 3)
    {
      if ((databyte & 0x80) > 0)
      {
        databyte &= 0x3F;
        state.Types.U8();
      }
    }
    else if ((databyte & 0x80) > 0)
    {
      databyte &= 0x7F;
      state.Types.U8();
    }

    return (NodeType)databyte;
  }

  private static void ReadNode(State state, Kv3Visitor visitor, bool insideArray)
  {
    var type = ReadType(state);

    if (insideArray)
    {
      ReadValue(state, visitor, string.Empty, type);
      return;
    }

    int stringId = state.Primary.Bytes4.I32();
    string name = stringId == -1 ? string.Empty : state.Strings[stringId];
    ReadValue(state, visitor, name, type);
  }

  private static void ReadValue(State state, Kv3Visitor visitor, string name, NodeType type)
  {
    var buffer = state.Primary;

    switch (type)
    {
      case NodeType.Null:
        return;
      case NodeType.BooleanTrue:
        visitor.Bool(name, true);
        return;
      case NodeType.BooleanFalse:
        visitor.Bool(name, false);
        return;
      case NodeType.Int64Zero:
        visitor.Int(name, 0);
        return;
      case NodeType.Int64One:
        visitor.Int(name, 1);
        return;
      case NodeType.DoubleZero:
        visitor.Real(name, 0d);
        return;
      case NodeType.DoubleOne:
        visitor.Real(name, 1d);
        return;
      case NodeType.Boolean:
        visitor.Bool(name, buffer.Bytes1.U8() == 1);
        return;
      case NodeType.Int32AsByte:
        visitor.Int(name, buffer.Bytes1.U8());
        return;
      case NodeType.Int16:
        visitor.Int(name, buffer.Bytes2.I16());
        return;
      case NodeType.UInt16:
        visitor.Int(name, buffer.Bytes2.U16());
        return;
      case NodeType.Int32:
        visitor.Int(name, buffer.Bytes4.I32());
        return;
      case NodeType.UInt32:
        visitor.Int(name, buffer.Bytes4.U32());
        return;
      case NodeType.Float:
        visitor.Real(name, buffer.Bytes4.F32());
        return;
      case NodeType.Int64:
        visitor.Int(name, buffer.Bytes8.I64());
        return;
      case NodeType.UInt64:
        visitor.Int(name, (long)buffer.Bytes8.U64());
        return;
      case NodeType.Double:
        visitor.Real(name, buffer.Bytes8.F64());
        return;
      case NodeType.String:
      {
        int id = buffer.Bytes4.I32();
        visitor.Text(name, id == -1 ? string.Empty : state.Strings[id]);
        return;
      }
      case NodeType.BinaryBlob when state.Version < 2:
      {
        int length = buffer.Bytes4.I32();
        if (length <= 0)
        {
          visitor.Blob(name, ReadOnlySpan<byte>.Empty);
          return;
        }

        if (visitor.WantBlob(name))
          visitor.Blob(name, buffer.Bytes1.Take(length));
        else
          buffer.Bytes1.Skip(length);

        return;
      }
      case NodeType.BinaryBlob:
      {
        int length = state.BlobLengths.I32();
        if (length <= 0)
        {
          visitor.Blob(name, ReadOnlySpan<byte>.Empty);
          return;
        }

        if (visitor.WantBlob(name))
          visitor.Blob(name, state.Blobs.Take(length));
        else
          state.Blobs.Skip(length);

        return;
      }
      case NodeType.Array:
      {
        int count = buffer.Bytes4.I32();
        visitor.BeginArray(name, count);

        for (int i = 0; i < count; i++)
          ReadNode(state, visitor, insideArray: true);

        visitor.EndArray();
        return;
      }
      case NodeType.ArrayTyped:
      case NodeType.ArrayTypeByteLength:
      {
        int count = type == NodeType.ArrayTypeByteLength ? buffer.Bytes1.U8() : buffer.Bytes4.I32();
        var subType = ReadType(state);

        visitor.BeginArray(name, count);

        for (int i = 0; i < count; i++)
          ReadValue(state, visitor, string.Empty, subType);

        visitor.EndArray();
        return;
      }
      case NodeType.ArrayTypeAuxiliaryBuffer:
      {
        int count = buffer.Bytes1.U8();
        var subType = ReadType(state);

        visitor.BeginArray(name, count);

        (state.Auxiliary, state.Primary) = (state.Primary, state.Auxiliary);

        for (int i = 0; i < count; i++)
          ReadValue(state, visitor, string.Empty, subType);

        (state.Auxiliary, state.Primary) = (state.Primary, state.Auxiliary);

        visitor.EndArray();
        return;
      }
      case NodeType.Object:
      {
        int count = state.Version >= 5 ? state.ObjectLengths.I32() : buffer.Bytes4.I32();
        visitor.BeginObject(name);

        for (int i = 0; i < count; i++)
          ReadNode(state, visitor, insideArray: false);

        visitor.EndObject();
        return;
      }
      default:
        throw new InvalidDataException($"Bilinmeyen KV3 tipi: {(int)type}");
    }
  }

  public static Kv3Binary Load(byte[] data, int offset, int size)
  {
    using var stream = new MemoryStream(data, offset, size, writable: false);
    using var reader = new BinaryReader(stream, Encoding.UTF8);

    uint magic = reader.ReadUInt32();
    if (magic == Magic0)
      throw new InvalidDataException("Eski KV3 surumu (VKV3) desteklenmiyor.");

    int version = (int)(magic & 0xFF);
    if ((magic & 0xFFFFFF00) != MagicBase || version < 1 || version > 5)
      throw new InvalidDataException($"Gecersiz KV3 imzasi: 0x{magic:X8}");

    reader.ReadBytes(16);

    int compressionMethod = reader.ReadInt32();

    int compressionFrameSize = 0;
    int countBytes1, countBytes4, countBytes8;
    int countTypes = 0;
    int sizeUncompressedTotal;
    int sizeCompressedTotal;
    int countBlocks = 0;
    int sizeBinaryBlobsBytes = 0;

    if (version == 1)
    {
      countBytes1 = reader.ReadInt32();
      countBytes4 = reader.ReadInt32();
      countBytes8 = reader.ReadInt32();
      sizeUncompressedTotal = reader.ReadInt32();
      sizeCompressedTotal = (int)(size - stream.Position);
    }
    else
    {
      reader.ReadUInt16();
      compressionFrameSize = reader.ReadUInt16();
      countBytes1 = reader.ReadInt32();
      countBytes4 = reader.ReadInt32();
      countBytes8 = reader.ReadInt32();
      countTypes = reader.ReadInt32();
      reader.ReadUInt16();
      reader.ReadUInt16();
      sizeUncompressedTotal = reader.ReadInt32();
      sizeCompressedTotal = reader.ReadInt32();
      countBlocks = reader.ReadInt32();
      sizeBinaryBlobsBytes = reader.ReadInt32();
    }

    int countBytes2 = 0;

    if (version >= 4)
    {
      countBytes2 = reader.ReadInt32();
      reader.ReadInt32();
    }

    int sizeUncompressedBuffer1;
    int sizeCompressedBuffer1;
    int sizeUncompressedBuffer2 = 0;
    int sizeCompressedBuffer2 = 0;
    int countBytes1Buffer2 = 0;
    int countBytes2Buffer2 = 0;
    int countBytes4Buffer2 = 0;
    int countBytes8Buffer2 = 0;
    int countObjectsBuffer2 = 0;

    if (version >= 5)
    {
      sizeUncompressedBuffer1 = reader.ReadInt32();
      sizeCompressedBuffer1 = reader.ReadInt32();
      sizeUncompressedBuffer2 = reader.ReadInt32();
      sizeCompressedBuffer2 = reader.ReadInt32();
      countBytes1Buffer2 = reader.ReadInt32();
      countBytes2Buffer2 = reader.ReadInt32();
      countBytes4Buffer2 = reader.ReadInt32();
      countBytes8Buffer2 = reader.ReadInt32();
      reader.ReadInt32();
      countObjectsBuffer2 = reader.ReadInt32();
      reader.ReadInt32();
      reader.ReadInt32();
    }
    else
    {
      sizeCompressedBuffer1 = sizeCompressedTotal;
      sizeUncompressedBuffer1 = sizeUncompressedTotal;
    }

    int buffer1Length = version < 5 && compressionMethod == 2
      ? sizeUncompressedBuffer1 + sizeBinaryBlobsBytes
      : sizeUncompressedBuffer1;

    byte[] buffer1 = new byte[buffer1Length];
    ZstdSharp.Decompressor? zstd = null;

    try
    {
      if (compressionMethod == 0)
        reader.Read(buffer1.AsSpan(0, sizeUncompressedBuffer1));
      else if (compressionMethod == 1)
        DecompressLz4(reader, buffer1.AsSpan(0, sizeUncompressedBuffer1), sizeCompressedBuffer1);
      else if (compressionMethod == 2)
      {
        zstd = new ZstdSharp.Decompressor();
        DecompressZstd(zstd, reader, buffer1.AsSpan(0, buffer1Length), sizeCompressedBuffer1);
      }
      else
        throw new InvalidDataException($"Bilinmeyen KV3 sikistirmasi: {compressionMethod}");

      var buffer1Segment = new ArraySegment<byte>(buffer1, 0, sizeUncompressedBuffer1);

      int position = 0;
      var slots1 = new ArraySegment<byte>[4];

      if (countBytes1 > 0)
      {
        slots1[0] = buffer1Segment.Slice(position, countBytes1);
        position += countBytes1;
      }

      if (countBytes2 > 0)
      {
        Align(ref position, 2);
        slots1[1] = buffer1Segment.Slice(position, countBytes2 * 2);
        position += countBytes2 * 2;
      }

      if (countBytes4 > 0)
      {
        Align(ref position, 4);
        slots1[2] = buffer1Segment.Slice(position, countBytes4 * 4);
        position += countBytes4 * 4;
      }

      if (countBytes8 > 0)
      {
        Align(ref position, 8);
        slots1[3] = buffer1Segment.Slice(position, countBytes8 * 8);
        position += countBytes8 * 8;
      }
      else if (version < 5)
      {
        Align(ref position, 8);
      }

      int countStrings = BitConverter.ToInt32(slots1[2].Array!, slots1[2].Offset);
      slots1[2] = slots1[2][sizeof(int)..];

      var strings = new string[countStrings];

      ArraySegment<byte> types = default;
      ArraySegment<byte> objectLengths = default;
      ArraySegment<byte> blobSizesBuffer = default;
      ArraySegment<byte>[] slots2 = [default, default, default, default];

      if (version >= 5)
      {
        var stringsCursor = slots1[0];
        for (int i = 0; i < countStrings; i++)
          strings[i] = ReadNullTerminated(ref stringsCursor);
        slots1[0] = stringsCursor;

        byte[] buffer2 = new byte[sizeUncompressedBuffer2];

        if (compressionMethod == 0)
          reader.Read(buffer2.AsSpan());
        else if (compressionMethod == 1)
          DecompressLz4(reader, buffer2.AsSpan(), sizeCompressedBuffer2);
        else
        {
          zstd ??= new ZstdSharp.Decompressor();
          DecompressZstd(zstd, reader, buffer2.AsSpan(), sizeCompressedBuffer2);
        }

        var buffer2Segment = new ArraySegment<byte>(buffer2);

        int position2 = countObjectsBuffer2 * sizeof(int);
        objectLengths = buffer2Segment[..position2];

        if (countBytes1Buffer2 > 0)
        {
          slots2[0] = buffer2Segment.Slice(position2, countBytes1Buffer2);
          position2 += countBytes1Buffer2;
        }

        if (countBytes2Buffer2 > 0)
        {
          Align(ref position2, 2);
          slots2[1] = buffer2Segment.Slice(position2, countBytes2Buffer2 * 2);
          position2 += countBytes2Buffer2 * 2;
        }

        if (countBytes4Buffer2 > 0)
        {
          Align(ref position2, 4);
          slots2[2] = buffer2Segment.Slice(position2, countBytes4Buffer2 * 4);
          position2 += countBytes4Buffer2 * 4;
        }

        if (countBytes8Buffer2 > 0)
        {
          Align(ref position2, 8);
          slots2[3] = buffer2Segment.Slice(position2, countBytes8Buffer2 * 8);
          position2 += countBytes8Buffer2 * 8;
        }

        types = buffer2Segment.Slice(position2, countTypes);
        position2 += countTypes;

        if (countBlocks == 0)
          position2 += 4;
        else
          blobSizesBuffer = buffer2Segment[position2..];
      }
      else
      {
        var stringsCursor = buffer1Segment[position..];
        int stringsStart = position;

        for (int i = 0; i < countStrings; i++)
        {
          int before = stringsCursor.Count;
          strings[i] = ReadNullTerminated(ref stringsCursor);
          position += before - stringsCursor.Count;
        }

        int typesLength = version == 1
          ? sizeUncompressedTotal - position - 4
          : countTypes - position + stringsStart;

        types = buffer1Segment.Slice(position, typesLength);
        position += typesLength;

        if (countBlocks == 0)
          position += 4;
        else
          blobSizesBuffer = buffer1Segment[position..];

        slots2 = slots1;
      }

      ArraySegment<byte> blobLengths = default;
      ArraySegment<byte> blobs = default;

      if (countBlocks > 0)
      {
        blobLengths = blobSizesBuffer[..(countBlocks * sizeof(int))];
        blobSizesBuffer = blobSizesBuffer[(countBlocks * sizeof(int) + sizeof(int))..];

        if (compressionMethod == 0)
        {
          byte[] raw = new byte[sizeBinaryBlobsBytes];
          reader.Read(raw.AsSpan());
          blobs = new ArraySegment<byte>(raw);
        }
        else if (compressionMethod == 1)
        {
          byte[] raw = new byte[sizeBinaryBlobsBytes];
          blobs = new ArraySegment<byte>(raw);

          using var decoder = new LZ4ChainDecoder(compressionFrameSize, 0);
          int decompressedOffset = 0;

          while (blobSizesBuffer.Count > 0)
          {
            ushort compressedBlockLength = MemoryMarshal.Read<ushort>(blobSizesBuffer);
            blobSizesBuffer = blobSizesBuffer[sizeof(ushort)..];

            byte[] input = ArrayPool<byte>.Shared.Rent(compressedBlockLength);

            try
            {
              int frameSize = decompressedOffset + compressionFrameSize > sizeBinaryBlobsBytes
                ? sizeBinaryBlobsBytes - decompressedOffset
                : compressionFrameSize;

              var output = raw.AsSpan(decompressedOffset, frameSize);
              var source = input.AsSpan(0, compressedBlockLength);
              reader.Read(source);

              if (!decoder.DecodeAndDrain(source, output, out int decoded) || decoded < 1)
                throw new InvalidDataException("LZ4 blok cozulemedi.");

              decompressedOffset += decoded;
            }
            finally
            {
              ArrayPool<byte>.Shared.Return(input);
            }
          }
        }
        else
        {
          if (version >= 5)
          {
            byte[] raw = new byte[sizeBinaryBlobsBytes];
            blobs = new ArraySegment<byte>(raw);

            int compressedBlobs = sizeCompressedTotal - sizeCompressedBuffer1 - sizeCompressedBuffer2;
            zstd ??= new ZstdSharp.Decompressor();
            DecompressZstd(zstd, reader, raw.AsSpan(), compressedBlobs);
          }
          else
          {
            blobs = new ArraySegment<byte>(buffer1, sizeUncompressedBuffer1, sizeBinaryBlobsBytes);
          }
        }
      }

      return new Kv3Binary(version, strings, types, objectLengths, blobLengths, blobs, slots2, slots1);
    }
    finally
    {
      zstd?.Dispose();
    }
  }

  private static void DecompressLz4(BinaryReader reader, Span<byte> output, int compressedSize)
  {
    byte[] input = ArrayPool<byte>.Shared.Rent(compressedSize);

    try
    {
      var source = input.AsSpan(0, compressedSize);
      reader.Read(source);

      int written = LZ4Codec.Decode(source, output);
      if (written != output.Length)
        throw new InvalidDataException($"LZ4 cozulemedi ({written}/{output.Length}).");
    }
    finally
    {
      ArrayPool<byte>.Shared.Return(input);
    }
  }

  private static void DecompressZstd(ZstdSharp.Decompressor decompressor, BinaryReader reader, Span<byte> output, int compressedSize)
  {
    byte[] input = ArrayPool<byte>.Shared.Rent(compressedSize);

    try
    {
      var source = input.AsSpan(0, compressedSize);
      reader.Read(source);

      if (!decompressor.TryUnwrap(source, output, out int written) || written != output.Length)
        throw new InvalidDataException($"ZSTD cozulemedi ({written}/{output.Length}).");
    }
    finally
    {
      ArrayPool<byte>.Shared.Return(input);
    }
  }

  private static string ReadNullTerminated(ref ArraySegment<byte> buffer)
  {
    int end = buffer.AsSpan().IndexOf((byte)0);
    if (end < 0)
      end = buffer.Count;

    string value = Encoding.UTF8.GetString(buffer.AsSpan()[..end]);
    buffer = buffer[Math.Min(end + 1, buffer.Count)..];
    return value;
  }

  private static void Align(ref int offset, int alignment)
  {
    alignment -= 1;
    offset += alignment;
    offset &= ~alignment;
  }
}
