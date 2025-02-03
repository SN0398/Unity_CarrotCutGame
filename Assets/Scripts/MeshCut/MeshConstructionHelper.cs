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
        if (vertices.Count <= index) { Debug.LogError("index(" + index + ")がvertices(" + vertices.Count + ")の範囲外なため無効な値が参照されます"); }
        if (uvs.Count <= index) { Debug.LogError("index(" + index + ")がuvs(" + uvs.Count + ")の範囲外なため無効な値が参照されます"); }
        if (normals.Count <= index) { Debug.LogError("index(" + index + ")がnormals(" + normals.Count + ")の範囲外なため無効な値が参照されます"); }
        vert.Position = vertices[index];
        vert.Uv = uvs[index];
        vert.Normal = normals[index];
        return vert;
    }

    public MeshConstructionHelper MergeHelper(MeshConstructionHelper helper)
    {
        // ポリゴンごとに走査
        for (int i = 0; i < helper.triangles.Count; i += 3)
        {
            // インデックス
            int[] index = new int[3];
            index[0] = helper.triangles[i];
            index[1] = helper.triangles[i + 1];
            index[2] = helper.triangles[i + 2];

            VertexData[] v = new VertexData[3];
            v[0] = helper.GetVertexData(index[0]);
            v[1] = helper.GetVertexData(index[1]);
            v[2] = helper.GetVertexData(index[2]);

            AddMeshSection(v[0], v[1], v[2]);
        }
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

    /// <summary>
    /// ポリゴンを追加
    /// </summary>
    /// <param name="vertexA">ポリゴンを構成する頂点１</param>
    /// <param name="vertexB">ポリゴンを構成する頂点２</param>
    /// <param name="vertexC">ポリゴンを構成する頂点３</param>
    public void AddMeshSection(VertexData vertexA, VertexData vertexB, VertexData vertexC)
    {
        int indexA = TryAddVertex(vertexA);
        int indexB = TryAddVertex(vertexB);
        int indexC = TryAddVertex(vertexC);

        AddTriangle(indexA, indexB, indexC);
    }

    /// <summary>
    /// ポリゴン情報を追加
    /// </summary>
    /// <param name="indexA">ポリゴンを構成する頂点のインデックス１</param>
    /// <param name="indexB">ポリゴンを構成する頂点のインデックス２</param>
    /// <param name="indexC">ポリゴンを構成する頂点のインデックス３</param>
    private void AddTriangle(int indexA, int indexB, int indexC)
    {
        triangles.Add(indexA);
        triangles.Add(indexB);
        triangles.Add(indexC);
    }

    /// <summary>
    /// 頂点の追加
    /// </summary>
    /// <param name="vertex">登録する頂点情報</param>
    /// <returns>追加した頂点のインデックス</returns>
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