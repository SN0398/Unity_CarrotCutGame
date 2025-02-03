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
        if (vertices.Count <= index) { Debug.LogError("index(" + index + ")��vertices(" + vertices.Count + ")�͈̔͊O�Ȃ��ߖ����Ȓl���Q�Ƃ���܂�"); }
        if (uvs.Count <= index) { Debug.LogError("index(" + index + ")��uvs(" + uvs.Count + ")�͈̔͊O�Ȃ��ߖ����Ȓl���Q�Ƃ���܂�"); }
        if (normals.Count <= index) { Debug.LogError("index(" + index + ")��normals(" + normals.Count + ")�͈̔͊O�Ȃ��ߖ����Ȓl���Q�Ƃ���܂�"); }
        vert.Position = vertices[index];
        vert.Uv = uvs[index];
        vert.Normal = normals[index];
        return vert;
    }

    public MeshConstructionHelper MergeHelper(MeshConstructionHelper helper)
    {
        // �|���S�����Ƃɑ���
        for (int i = 0; i < helper.triangles.Count; i += 3)
        {
            // �C���f�b�N�X
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
    /// �|���S����ǉ�
    /// </summary>
    /// <param name="vertexA">�|���S�����\�����钸�_�P</param>
    /// <param name="vertexB">�|���S�����\�����钸�_�Q</param>
    /// <param name="vertexC">�|���S�����\�����钸�_�R</param>
    public void AddMeshSection(VertexData vertexA, VertexData vertexB, VertexData vertexC)
    {
        int indexA = TryAddVertex(vertexA);
        int indexB = TryAddVertex(vertexB);
        int indexC = TryAddVertex(vertexC);

        AddTriangle(indexA, indexB, indexC);
    }

    /// <summary>
    /// �|���S������ǉ�
    /// </summary>
    /// <param name="indexA">�|���S�����\�����钸�_�̃C���f�b�N�X�P</param>
    /// <param name="indexB">�|���S�����\�����钸�_�̃C���f�b�N�X�Q</param>
    /// <param name="indexC">�|���S�����\�����钸�_�̃C���f�b�N�X�R</param>
    private void AddTriangle(int indexA, int indexB, int indexC)
    {
        triangles.Add(indexA);
        triangles.Add(indexB);
        triangles.Add(indexC);
    }

    /// <summary>
    /// ���_�̒ǉ�
    /// </summary>
    /// <param name="vertex">�o�^���钸�_���</param>
    /// <returns>�ǉ��������_�̃C���f�b�N�X</returns>
    private int TryAddVertex(VertexData vertex)
    {
        // ���_�d�����
        // ���ɑ��݂��钸�_�Ȃ�C���f�b�N�X�����̂܂ܕԂ�
        if (_vertexDictionary.TryGetValue(vertex, out int index))
        {
			return index;
        }
        // ���_��ǉ����ăC���f�b�N�X��Ԃ�
        vertices.Add(vertex.Position);
        uvs.Add(vertex.Uv);
        normals.Add(vertex.Normal);
        int newIndex = vertices.Count - 1;
        _vertexDictionary.Add(vertex, newIndex);
        return newIndex;
    }
}