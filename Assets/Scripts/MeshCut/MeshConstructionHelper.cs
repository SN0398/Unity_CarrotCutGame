using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using TMPro;
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
    public List<Vector3> vertices;
    public List<Vector3> normals;
    public List<Vector2> uvs;
    public List<int> triangles;

    private Dictionary<VertexData, int> _vertexDictionary;

    public MeshConstructionHelper()
    {
        triangles = new List<int>();
        vertices = new List<Vector3>();
        uvs = new List<Vector2>();
        normals = new List<Vector3>();
        _vertexDictionary = new Dictionary<VertexData, int>();
    }

    public Mesh ConstructMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        return mesh;
    }

    public VertexData GetVertexData(int index)
    {
        VertexData vert = new VertexData();
        vert.Position = vertices[index];
        vert.Uv = uvs[index];
        vert.Normal = normals[index];
        return vert;
    }

    public MeshConstructionHelper MergeHelper(MeshConstructionHelper helper)
    {
        vertices.AddRange(helper.vertices);
        normals.AddRange(helper.normals);
        uvs.AddRange(helper.uvs);
        triangles.AddRange(helper.triangles);
        return this;
    }

    public void InverseTransformPoints(GameObject gameObject)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = gameObject.transform.InverseTransformPoint(vertices[i]);
        }
    }

    public void TransformPoints(GameObject gameObject)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = gameObject.transform.TransformPoint(vertices[i]);
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
        triangles.Add(indexA);
        triangles.Add(indexB);
        triangles.Add(indexC);
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
        vertices.Add(vertex.Position);
        uvs.Add(vertex.Uv);
        normals.Add(vertex.Normal);
        int newIndex = vertices.Count - 1;
        _vertexDictionary.Add(vertex, newIndex);
        return newIndex;
    }
}

// 切断面構築用クラス
public class SlicedMeshConstructionHelper
{
    MeshConstructionHelper helper {  get; set; }
    List<int> pointsAlongPlane {  get; set; }

    public SlicedMeshConstructionHelper()
    {
        pointsAlongPlane = new List<int>();

    }
}