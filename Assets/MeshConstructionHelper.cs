using System.Collections.Generic;
using UnityEngine;
using static MeshCut;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

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

    public static void ClearMesh()
    {
        positiveMesh._vertices.Clear();
        positiveMesh._normals.Clear();
        positiveMesh._uvs.Clear();
        positiveMesh._triangles.Clear();
        negativeMesh._vertices.Clear();
        negativeMesh._normals.Clear();
        negativeMesh._uvs.Clear();
        negativeMesh._triangles.Clear();
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

    public static MeshConstructionHelper positiveMesh = new MeshConstructionHelper();
    public static MeshConstructionHelper negativeMesh = new MeshConstructionHelper();
}
