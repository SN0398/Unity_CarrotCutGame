using System.Collections.Generic;
using UnityEngine;
using static MeshCut;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public struct VertexData
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 Uv;
}

public class MeshConstructionHelper
{
    private List<Vector3> _vertices;
    private List<Vector3> _normals;
    private List<Vector2> _uvs;
    private List<int> _triangles;

    private Dictionary<VertexData, int> _vertexDictionary;

    public MeshConstructionHelper()
    {
        _triangles = new List<int>();
        _vertices = new List<Vector3>();
        _uvs = new List<Vector2>();
        _normals = new List<Vector3>();
        _vertexDictionary = new Dictionary<VertexData, int>();
    }

    public Mesh ConstructMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = _vertices.ToArray();
        mesh.triangles = _triangles.ToArray();
        mesh.normals = _normals.ToArray();
        mesh.uv = _uvs.ToArray();
        return mesh;
    }

    public void InverseTransformPoints(GameObject gameObject)
    {
        for (int i = 0; i < _vertices.Count; i++)
        {
            _vertices[i] = gameObject.transform.InverseTransformPoint(_vertices[i]);
        }
    }

    public void TransformPoints(GameObject gameObject)
    {
        for (int i = 0; i < _vertices.Count; i++)
        {
            _vertices[i] = gameObject.transform.TransformPoint(_vertices[i]);
        }
    }

    public void AddMeshSection(VertexData vertexA, VertexData vertexB, VertexData vertexC)
    {
        int indexA = TryAddVertex(vertexA);
        int indexB = TryAddVertex(vertexB);
        int indexC = TryAddVertex(vertexC);

        AddTriangle(indexA, indexB, indexC);
    }

    private void AddTriangle(int indexA, int indexB, int indexC)
    {
        _triangles.Add(indexA);
        _triangles.Add(indexB);
        _triangles.Add(indexC);
    }

    private int TryAddVertex(VertexData vertex)
    {
        // 頂点重複回避
        // 既に存在する頂点ならインデックスをそのまま返す
        if (_vertexDictionary.TryGetValue(vertex, out int index))
        {
			return index;
        }
        // 頂点を追加してインデックスを返す
        _vertices.Add(vertex.Position);
        _uvs.Add(vertex.Uv);
        _normals.Add(vertex.Normal);
        int newIndex = _vertices.Count - 1;
        _vertexDictionary.Add(vertex, newIndex);
        return newIndex;
    }

    /// <summary>
    /// ポリゴンの分割関数　
    /// メッシュの繋がっていない部分を別々のメッシュに構成して返す
    /// </summary>
    /// <param name="mesh">対象メッシュ</param>
    /// <returns>分割されたメッシュリスト</returns>
    public static List<MeshConstructionHelper> MeshDivision(MeshConstructionHelper mesh)
    {
        List<MeshConstructionHelper> meshGroup = new List<MeshConstructionHelper>();

        // ポリゴンは三つの頂点で構成されるので３づつ増分して走査
        for (int i = 0; i < mesh._triangles.Count; i += 3)
        {
            // 三角形が保有する頂点のインデックスを取得
            int indexA = mesh._triangles[i];
            int indexB = mesh._triangles[i + 1];
            int indexC = mesh._triangles[i + 2];

            var v1 = mesh._vertices[indexA];
            var v2 = mesh._vertices[indexB];
            var v3 = mesh._vertices[indexC];


        }

        return meshGroup;
    }
}