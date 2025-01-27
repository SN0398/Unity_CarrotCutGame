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
        // ���_�d�����
        // ���ɑ��݂��钸�_�Ȃ�C���f�b�N�X�����̂܂ܕԂ�
        if (_vertexDictionary.TryGetValue(vertex, out int index))
        {
			return index;
        }
        // ���_��ǉ����ăC���f�b�N�X��Ԃ�
        _vertices.Add(vertex.Position);
        _uvs.Add(vertex.Uv);
        _normals.Add(vertex.Normal);
        int newIndex = _vertices.Count - 1;
        _vertexDictionary.Add(vertex, newIndex);
        return newIndex;
    }

    /// <summary>
    /// �|���S���̕����֐��@
    /// ���b�V���̌q�����Ă��Ȃ�������ʁX�̃��b�V���ɍ\�����ĕԂ�
    /// </summary>
    /// <param name="mesh">�Ώۃ��b�V��</param>
    /// <returns>�������ꂽ���b�V�����X�g</returns>
    public static List<MeshConstructionHelper> MeshDivision(MeshConstructionHelper mesh)
    {
        List<MeshConstructionHelper> meshGroup = new List<MeshConstructionHelper>();

        // �|���S���͎O�̒��_�ō\�������̂łR�Â������đ���
        for (int i = 0; i < mesh._triangles.Count; i += 3)
        {
            // �O�p�`���ۗL���钸�_�̃C���f�b�N�X���擾
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