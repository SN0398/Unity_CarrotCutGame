using JetBrains.Annotations;
using System;
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
        List<int> meshGroupIndex = new List<int>(mesh._triangles.Count / 3);
        Dictionary<Vector3, int> verticesDic = new Dictionary<Vector3, int>();
        
        // �|���S���͎O�̒��_�ō\�������̂łR�Â������đ���
        for (int i = 0; i < mesh._triangles.Count; i += 3)
        {
            // �C���f�b�N�X
            int indexA = mesh._triangles[i];
            int indexB = mesh._triangles[i + 1];
            int indexC = mesh._triangles[i + 2];
            // ���_���W
            var v1 = mesh._vertices[indexA];
            var v2 = mesh._vertices[indexB];
            var v3 = mesh._vertices[indexC];
            // ���b�V���O���[�v�C���f�b�N�X
            var containIndex1 = 0;
            var containIndex2 = 0;
            var containIndex3 = 0;
            // ���b�V���O���[�v�ɏ������Ă��邩
            var hasContain1 = verticesDic.TryGetValue(v1, out containIndex1);
            var hasContain2 = verticesDic.TryGetValue(v2, out containIndex2);
            var hasContain3 = verticesDic.TryGetValue(v3, out containIndex3);

            /*
            �R���_�ɑ΂��đ������s���Z�N�V�����ł́A���̃P�[�X���l������B
            �[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[
            �P�F���ׂĂ̒��_���������b�V���O���[�v�A�������͂��ꂼ��ʁX�̃��b�V���O���[�v�Ɋ���U���Ă���B
            �Q�F���_�̂����P�����b�V���O���[�v�ɏ������Ă��āA����ȊO�̓��b�V���O���[�v�Ɋ���U���Ă��Ȃ��B
            �R�F���_�̂����Q������܂��͕ʁX�̃��b�V���O���[�v�ɏ������Ă��āA������̓��b�V���O���[�v�Ɋ���U���Ă��Ȃ��B
            �S�F���ׂĂ̒��_�����b�V���O���[�v�ɏ������Ă��Ȃ��B
            �[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[
            ����ɑ΂��āA���ꂼ��ȉ��̑Ή����s���B
            �[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[
            �P�F���ꂼ��̃��b�V���O���[�v���������āA���_�̃��b�V���O���[�v�C���f�b�N�X���X�V�B
            �Q�F���b�V���O���[�v�ɏ������Ă��Ȃ��Q�̒��_���A���b�V���O���[�v�ɏ������Ă����̒��_�Ɠ������b�V���O���[�v�C���f�b�N�X������U��B
            �R�F�Q�̒��_�̎w�����b�V���O���[�v������ł���ꍇ�͂��̂܂܁A����o�Ȃ��ꍇ��2�̃��b�V���O���[�v���������A���̃��b�V���O���[�v�C���f�b�N�X���R�̒��_�Ɋ���U��B
            �S�F�V�������b�V���O���[�v���쐬���āA���̃C���f�b�N�X���R�̒��_�Ɋ���U��B
            �[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[�[
             */
            if (hasContain1 && hasContain2 && hasContain3)
            {
                // ���ׂĂ̒��_���قȂ郁�b�V���O���[�v�̏ꍇ�A��������
                if (containIndex1 != containIndex2 || containIndex1 != containIndex3)
                {
                    int newIndex = Math.Min(containIndex1, Math.Min(containIndex2, containIndex3));
                    meshGroupIndex[containIndex1] = newIndex;
                    meshGroupIndex[containIndex2] = newIndex;
                    meshGroupIndex[containIndex3] = newIndex;
                }
            }
            else if (hasContain1 || hasContain2 || hasContain3)
            {
                int existingGroup = hasContain1 ? containIndex1 : hasContain2 ? containIndex2 : containIndex3;
                verticesDic[v1] = existingGroup;
                verticesDic[v2] = existingGroup;
                verticesDic[v3] = existingGroup;
            }
            else
            {
                // �V�������b�V���O���[�v���쐬
                int newGroupIndex = meshGroup.Count;
                verticesDic[v1] = newGroupIndex;
                verticesDic[v2] = newGroupIndex;
                verticesDic[v3] = newGroupIndex;
                meshGroup.Add(new MeshConstructionHelper());
            }
        }

        return meshGroup;
    }
}

// �ؒf�ʍ\�z�p�N���X
public class SlicedMeshConstructionHelper
{
    MeshConstructionHelper helper {  get; set; }
    List<int> pointsAlongPlane {  get; set; }

    public SlicedMeshConstructionHelper()
    {
        pointsAlongPlane = new List<int>();

    }
}