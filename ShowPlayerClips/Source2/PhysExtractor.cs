using System.Numerics;

namespace ShowPlayerClips.Source2;

public sealed class ClipAttribute
{
  public string Group = string.Empty;
  public List<string> InteractAs = [];
  public List<string> InteractExclude = [];
}

public sealed class ClipHull
{
  public int AttributeIndex;
  public Vector3[] Positions = [];
  public byte[] Edges = [];
}

public sealed class ClipMesh
{
  public int AttributeIndex;
  public Vector3[] Vertices = [];
  public int[] Triangles = [];
}

public sealed class AttributeScanner : Kv3Visitor
{
  public readonly List<ClipAttribute> Attributes = [];

  private int _depth;
  private int _arrayDepth = -1;
  private ClipAttribute? _current;
  private List<string>? _stringTarget;

  public override void BeginArray(string name, int count)
  {
    _depth++;

    if (name == "m_collisionAttributes")
      _arrayDepth = _depth;
    else if (_current != null && name == "m_InteractAsStrings")
      _stringTarget = _current.InteractAs;
    else if (_current != null && name == "m_InteractExcludeStrings")
      _stringTarget = _current.InteractExclude;
  }

  public override void EndArray()
  {
    if (_arrayDepth == _depth)
      _arrayDepth = -1;

    _stringTarget = null;
    _depth--;
  }

  public override void BeginObject(string name)
  {
    _depth++;

    if (_arrayDepth >= 0 && _depth == _arrayDepth + 1)
    {
      _current = new ClipAttribute();
      Attributes.Add(_current);
    }
  }

  public override void EndObject()
  {
    if (_arrayDepth >= 0 && _depth == _arrayDepth + 1)
      _current = null;

    _depth--;
  }

  public override void Text(string name, string value)
  {
    if (_current == null)
      return;

    if (name == "m_CollisionGroupString")
      _current.Group = value;
    else if (name.Length == 0)
      _stringTarget?.Add(value);
  }
}

public sealed class GeometryScanner(HashSet<int> wanted, int maxTriangles = int.MaxValue) : Kv3Visitor
{
  public readonly List<ClipHull> Hulls = [];
  public readonly List<ClipMesh> Meshes = [];

  public int SkippedMeshes { get; private set; }

  private readonly HashSet<int> _wanted = wanted;
  private readonly int _maxTriangles = maxTriangles;

  private int _depth;
  private int _sectionDepth = -1;
  private bool _sectionIsHull;
  private int _entryDepth = -1;
  private int _attributeIndex = -1;

  private Vector3[]? _positions;
  private byte[]? _edges;
  private Vector3[]? _vertices;
  private int[]? _triangles;

  public override void BeginArray(string name, int count)
  {
    _depth++;

    if (name == "m_hulls")
    {
      _sectionDepth = _depth;
      _sectionIsHull = true;
    }
    else if (name == "m_meshes")
    {
      _sectionDepth = _depth;
      _sectionIsHull = false;
    }
  }

  public override void EndArray()
  {
    if (_sectionDepth == _depth)
      _sectionDepth = -1;

    _depth--;
  }

  public override void BeginObject(string name)
  {
    _depth++;

    if (_sectionDepth >= 0 && _depth == _sectionDepth + 1)
    {
      _entryDepth = _depth;
      _attributeIndex = -1;
      _positions = null;
      _edges = null;
      _vertices = null;
      _triangles = null;
    }
  }

  public override void EndObject()
  {
    if (_entryDepth == _depth)
    {
      if (_wanted.Contains(_attributeIndex))
      {
        if (_sectionIsHull && _positions != null && _edges != null)
          Hulls.Add(new ClipHull { AttributeIndex = _attributeIndex, Positions = _positions, Edges = _edges });
        else if (!_sectionIsHull && _vertices != null && _triangles != null)
          Meshes.Add(new ClipMesh { AttributeIndex = _attributeIndex, Vertices = _vertices, Triangles = _triangles });
      }

      _entryDepth = -1;
      _positions = null;
      _edges = null;
      _vertices = null;
      _triangles = null;
    }

    _depth--;
  }

  public override void Int(string name, long value)
  {
    if (name == "m_nCollisionAttributeIndex" && _entryDepth >= 0)
      _attributeIndex = (int)value;
  }

  public override bool WantBlob(string name)
  {
    if (_entryDepth < 0 || !_wanted.Contains(_attributeIndex))
      return false;

    if (_sectionIsHull)
      return name is "m_VertexPositions" or "m_Edges";

    return name is "m_Vertices" or "m_Triangles";
  }

  public override void Blob(string name, ReadOnlySpan<byte> data)
  {
    if (_entryDepth < 0 || data.Length == 0)
      return;

    switch (name)
    {
      case "m_VertexPositions" when _sectionIsHull:
        _positions = ReadVectors(data);
        break;
      case "m_Edges" when _sectionIsHull:
        _edges = data.ToArray();
        break;
      case "m_Vertices" when !_sectionIsHull:
        _vertices = ReadVectors(data);
        break;
      case "m_Triangles" when !_sectionIsHull:
        if (data.Length / 12 > _maxTriangles)
        {
          SkippedMeshes++;
          _vertices = null;
          break;
        }

        _triangles = ReadIndices(data);
        break;
    }
  }

  private static Vector3[] ReadVectors(ReadOnlySpan<byte> data)
  {
    int count = data.Length / 12;
    var result = new Vector3[count];

    for (int i = 0; i < count; i++)
    {
      int offset = i * 12;
      result[i] = new Vector3(
        BitConverter.ToSingle(data[offset..]),
        BitConverter.ToSingle(data[(offset + 4)..]),
        BitConverter.ToSingle(data[(offset + 8)..]));
    }

    return result;
  }

  private static int[] ReadIndices(ReadOnlySpan<byte> data)
  {
    int count = data.Length / 4;
    var result = new int[count];

    for (int i = 0; i < count; i++)
      result[i] = BitConverter.ToInt32(data[(i * 4)..]);

    return result;
  }
}
