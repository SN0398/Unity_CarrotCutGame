using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Schema;
using UniRx;
using UnityEditor;
using UnityEngine;
using static MeshCut;

// Refference:
// unity���b�V��
// https://qiita.com/keito_takaishi/items/8e56d5117ee90502e864
// ���b�V���J�b�g�T���v��
// https://qiita.com/edo_m18/items/31961cd19fd19e09b675
// https://medium.com/@hesmeron/mesh-slicing-in-unity-740b21ffdf84
// ���b�V���J�b�g������
// https://qiita.com/suga__/items/87e952e46e205c18e95f

public class MeshCut : MonoBehaviour
{
    // ��
    private static Vector3  planePosition;
    private static Vector3  planeNormal;
    
    // ����������
    private static Plane plane;

    // ���b�V��
    private static MeshFilter _meshFilter;
    private static List<Vector3> _vertices;
    private static List<Vector3> _normals;
    private static List<Vector2> _uvs;
    private static List<int> _triangles;

    public static void ClearAll()
    {
        _vertices.Clear();
        _normals.Clear();
        _uvs.Clear();
        _triangles.Clear();
    }

    // ���b�V�����擾
    public static void GetMeshInfo(GameObject subject)
    {
        _meshFilter = subject.GetComponent<MeshFilter>();
        _vertices = new List<Vector3>(_meshFilter.sharedMesh.vertices);
        _normals = new List<Vector3>(_meshFilter.sharedMesh.normals);
        _uvs = new List<Vector2>(_meshFilter.sharedMesh.uv);
        _triangles = new List<int>(_meshFilter.sharedMesh.triangles);
        // ���_�̈ʒu�����[���h��Ԃɕϊ�
        for(int i = 0; i < _vertices.Count;i++)
        {
            _vertices[i] = subject.transform.TransformPoint(_vertices[i]);
        }
    }

    public static VertexData GetMyVertexData(int index)
    {
        VertexData vert = new VertexData();
        if(_vertices.Count <= index) { Debug.LogError("index(" + index + ")��vertices(" + _vertices.Count + ")�͈̔͊O�Ȃ��ߖ����Ȓl���Q�Ƃ���܂�"); }
        if(_uvs.Count <= index) { Debug.LogError("index(" + index + ")��uvs(" + _uvs.Count + ")�͈̔͊O�Ȃ��ߖ����Ȓl���Q�Ƃ���܂�"); }
        if(_normals.Count <= index) { Debug.LogError("index(" + index + ")��normals(" + _normals.Count + ")�͈̔͊O�Ȃ��ߖ����Ȓl���Q�Ƃ���܂�"); }
        vert.Position = _vertices[index];
        vert.Uv = _uvs[index];
        vert.Normal = _normals[index];
        return vert;
    }

    // ��_�ւ̋����̎Z�o
    public static float ComputeIntersection(Vector3 p1, Vector3 p2)
    {
        Ray ray = new Ray(p1, p2 - p1);
        float enter;
        if (plane.Raycast(ray, out enter))
        {
            var normalizedDistance = enter / (p2 - p1).magnitude;
            return normalizedDistance;
        }
        return 0f;
    }

    // ��_�ɒ��_�𐶐�
    public static VertexData ComputeIntersectionVertex(int indexA, int indexB)
    {
        VertexData vert = new VertexData();
        var pos1 = _vertices[indexA];
        var uv1 =  _uvs[indexA];
        var nrm1 = _normals[indexA];
        var pos2 = _vertices[indexB];
        var uv2 =  _uvs[indexB];
        var nrm2 = _normals[indexB];
        var normalizedDistance = ComputeIntersection(pos1, pos2);
        vert.Position = Vector3.Lerp(pos1, pos2, normalizedDistance);
        vert.Normal = Vector3.Lerp(nrm1, nrm2, normalizedDistance);
        vert.Uv = Vector2.Lerp(uv1, uv2, normalizedDistance);
        return vert;

    }

    // �ʂ̕\���ǂ���Ɉʒu���邩�擾����
    public static bool ComputeSide(Vector3 p)
    {
        return plane.GetSide(p);
    }

    public static (MeshConstructionHelper positive, MeshConstructionHelper negative) Cut
        (GameObject subject, Vector3 planeOrigin, Vector3 planeNormal)
    {
        GetMeshInfo(subject);
        plane = new Plane(planeNormal, planeOrigin); 
        // �ؒf�ʂɉ������_�̃��X�g
        List<VertexData> pointsAlongPlane = new List<VertexData>();
        MeshConstructionHelper positiveMesh = new MeshConstructionHelper();
        MeshConstructionHelper negativeMesh = new MeshConstructionHelper();

        bool[] sides = new bool[3];
        // �|���S���͎O�̒��_�ō\�������̂łR�Â������đ���
        for (int i = 0; i < _triangles.Count; i += 3)
        {
            // �O�p�`���ۗL���钸�_�̃C���f�b�N�X���擾
            int indexA = _triangles[i];
            int indexB = _triangles[i+1];
            int indexC = _triangles[i+2];

            // ���_�f�[�^�擾
            var vA = GetMyVertexData(indexA);
            var vB = GetMyVertexData(indexB);
            var vC = GetMyVertexData(indexC);

            // ���_���ꂼ��̖ʂƂ̍��E����
            sides[0] = ComputeSide(vA.Position);
            sides[1] = ComputeSide(vB.Position);
            sides[2] = ComputeSide(vC.Position);

            bool isABSameSide = sides[0] == sides[1];
            bool isBCSameSide = sides[1] == sides[2];

            // �S�Ă̒��_���Б��Ɋ���Ă���΂��̂܂܂ǂ��炩�̃��b�V���Ƀ|���S���Ƃ��ēo�^
            if (isABSameSide && isBCSameSide)
            {
                MeshConstructionHelper helper = sides[0] ? positiveMesh : negativeMesh;
                helper.AddMeshSection(vA, vB, vC);
            }
            else
            {
                VertexData intersectionD;
                VertexData intersectionE;
                MeshConstructionHelper helperA = sides[0] ? positiveMesh : negativeMesh;
                MeshConstructionHelper helperB = sides[1] ? positiveMesh : negativeMesh;
                MeshConstructionHelper helperC = sides[2] ? positiveMesh : negativeMesh;
                // C���Б��ɂ���
                if (isABSameSide)
                {
                    // ��_�̎Z�o
                    intersectionD = ComputeIntersectionVertex(indexA, indexC);  // A��C
                    intersectionE = ComputeIntersectionVertex(indexB, indexC);  // B��C
                    // ��_����V��������������̒��_�ŐV�����|���S�����`������
                    // �|���S����AB���͓�AC���͈��
                    // �|���S���ɓo�^���钸�_�͔����v���ɏ���
                    helperA.AddMeshSection(vA, vB, intersectionE);              // A -> B -> E
                    helperA.AddMeshSection(vA, intersectionE, intersectionD);   // A -> E -> D
                    helperC.AddMeshSection(intersectionE, vC, intersectionD);   // E -> C -> D
                }
                // A���Б��ɂ���
                else if (isBCSameSide)
                {
                    intersectionD = ComputeIntersectionVertex(indexB, indexA);
                    intersectionE = ComputeIntersectionVertex(indexC, indexA);
                    helperB.AddMeshSection(vB, vC, intersectionE);
                    helperB.AddMeshSection(vB, intersectionE, intersectionD);
                    helperA.AddMeshSection(intersectionE, vA, intersectionD);
                }
                // B���Б��ɂ���
                else
                {
                    intersectionD = ComputeIntersectionVertex(indexA, indexB);
                    intersectionE = ComputeIntersectionVertex(indexC, indexB);
                    helperA.AddMeshSection(vA, intersectionE, vC);
                    helperA.AddMeshSection(intersectionD, intersectionE, vA);
                    helperB.AddMeshSection(vB, intersectionE, intersectionD);
                }
                // �ؒf�ʂ��\�����钸�_�����X�g�ɒǉ�
                pointsAlongPlane.Add(intersectionD);
                pointsAlongPlane.Add(intersectionE);
            }
        }

        // �ؒf�ʂ̍\�z
        FillCutSurface(subject, ref positiveMesh, planeNormal, pointsAlongPlane);
        FillCutSurface(subject, ref negativeMesh, -planeNormal, pointsAlongPlane);

        return (positiveMesh, negativeMesh);
    }

    public static List<MeshConstructionHelper> CutDivide
        (GameObject subject, Vector3 planeOrigin, Vector3 planeNormal)
    {
        // ������
        GetMeshInfo(subject);   // �������b�V�����擾
        plane = new Plane(planeNormal, planeOrigin);    // �ؒf�ʒ�`

        // �ؒf�ʂɉ������_�̃��X�g
        List<VertexData> pointsAlongPlane = new List<VertexData>();

        // �����̃��b�V���A�E���̃��b�V��
        MeshConstructionHelper positiveMesh = new MeshConstructionHelper();
        MeshConstructionHelper negativeMesh = new MeshConstructionHelper();

        // �|���S���Ōq�����Ă��镔���𓯈ꃁ�b�V���Ƃ��ăO���[�s���O����
        List<MeshConstructionHelper> positiveMeshGroup = new List<MeshConstructionHelper>();
        List<MeshConstructionHelper> negativeMeshGroup = new List<MeshConstructionHelper>();

        bool[] sides = new bool[3];
        // �|���S���͎O�̒��_�ō\�������̂łR�Â������đ���
        for (int i = 0; i < _triangles.Count; i += 3)
        {
            // �O�p�`���ۗL���钸�_�̃C���f�b�N�X���擾
            int indexA = _triangles[i];
            int indexB = _triangles[i+1];
            int indexC = _triangles[i+2];

            // ���_�f�[�^�擾
            var vA = GetMyVertexData(indexA);
            var vB = GetMyVertexData(indexB);
            var vC = GetMyVertexData(indexC);

            // ���_���ꂼ��̖ʂƂ̍��E����
            sides[0] = ComputeSide(vA.Position);
            sides[1] = ComputeSide(vB.Position);
            sides[2] = ComputeSide(vC.Position);

            bool isABSameSide = sides[0] == sides[1];
            bool isBCSameSide = sides[1] == sides[2];

            // �S�Ă̒��_���Б��Ɋ���Ă���΂��̂܂܂ǂ��炩�̃��b�V���Ƀ|���S���Ƃ��ēo�^
            if (isABSameSide && isBCSameSide)
            {
                MeshConstructionHelper helper = sides[0] ? positiveMesh : negativeMesh;
                helper.AddMeshSection(vA, vB, vC);
            }
            else
            {
                VertexData intersectionD;
                VertexData intersectionE;
                MeshConstructionHelper helperA = sides[0] ? positiveMesh : negativeMesh;
                MeshConstructionHelper helperB = sides[1] ? positiveMesh : negativeMesh;
                MeshConstructionHelper helperC = sides[2] ? positiveMesh : negativeMesh;
                // C���Б��ɂ���
                if (isABSameSide)
                {
                    // ��_�̎Z�o
                    intersectionD = ComputeIntersectionVertex(indexA, indexC);  // A��C
                    intersectionE = ComputeIntersectionVertex(indexB, indexC);  // B��C
                    // ��_����V��������������̒��_�ŐV�����|���S�����`������
                    // �|���S����AB���͓�AC���͈��
                    // �|���S���ɓo�^���钸�_�͔����v���ɏ���
                    helperA.AddMeshSection(vA, vB, intersectionE);              // A -> B -> E
                    helperA.AddMeshSection(vA, intersectionE, intersectionD);   // A -> E -> D
                    helperC.AddMeshSection(intersectionE, vC, intersectionD);   // E -> C -> D
                }
                // A���Б��ɂ���
                else if (isBCSameSide)
                {
                    intersectionD = ComputeIntersectionVertex(indexB, indexA);
                    intersectionE = ComputeIntersectionVertex(indexC, indexA);
                    helperB.AddMeshSection(vB, vC, intersectionE);
                    helperB.AddMeshSection(vB, intersectionE, intersectionD);
                    helperA.AddMeshSection(intersectionE, vA, intersectionD);
                }
                // B���Б��ɂ���
                else
                {
                    intersectionD = ComputeIntersectionVertex(indexA, indexB);
                    intersectionE = ComputeIntersectionVertex(indexC, indexB);
                    helperA.AddMeshSection(vA, intersectionE, vC);
                    helperA.AddMeshSection(intersectionD, intersectionE, vA);
                    helperB.AddMeshSection(vB, intersectionE, intersectionD);
                }
                // �ؒf�ʂ��\�����钸�_�����X�g�ɒǉ�
                pointsAlongPlane.Add(intersectionD);
                pointsAlongPlane.Add(intersectionE);
            }
        }
        Debug.Log("���b�V�������J�n");
        var positiveResult = MeshDivision(positiveMesh, pointsAlongPlane);
        Debug.Log("��������");
        var negativeResult = MeshDivision(negativeMesh, pointsAlongPlane);
        Debug.Log("�E������");

        positiveMeshGroup = positiveResult.meshList;
        negativeMeshGroup = negativeResult.meshList;

        // �ؒf�ʂ̍\�z
        for(int i = 0; i < positiveMeshGroup.Count; i++)
        {
            var obj = positiveMeshGroup[i];
            FillCutSurface(subject, ref obj, planeNormal, positiveResult.pointsAlongPlaneList[i]);
        }
        for(int i = 0; i < negativeMeshGroup.Count; i++)
        {
            var obj = negativeMeshGroup[i];
            FillCutSurface(subject, ref obj, -planeNormal, negativeResult.pointsAlongPlaneList[i]);
        }
        
        positiveMeshGroup.AddRange(negativeMeshGroup);
        return positiveMeshGroup;
    }

    #region
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
    #endregion

    public static (List<MeshConstructionHelper> meshList, List<List<VertexData>> pointsAlongPlaneList)
        MeshDivision(MeshConstructionHelper mesh, List<VertexData> pointsAlongPlane)
    {
        // ���b�V���O���[�v�i�[�i�}�[�W�����ƃ��b�V���O���[�v�͏d������j
        List<MeshConstructionHelper> meshGroup = new List<MeshConstructionHelper>();
        // ���_�̌��������i�[�@��r�͒��_���W�݂̂Ŕ�r
        Dictionary<Vector3, int> verticesDic = new Dictionary<Vector3, int>();

        int[] index;               // ���_���w���C���f�b�N�X
        Vector3[] v;               // �C���f�b�N�X���w�����_��3D���W
        int[] containIndex;        // ���_�̊���U���Ă��郁�b�V���O���[�v�̃C���f�b�N�X
        bool[] hasContain;         // ���b�V���O���[�v�Ɋ���U���Ă��邩�̃t���O

        int counttmp = 0;

        #region
        bool AddMeshSection(int index1)
        {
            int meshGroupIndex = containIndex[index1];
            if (meshGroupIndex < 0 || meshGroupIndex >= meshGroup.Count)
            {
                return false;
            }
            meshGroup[meshGroupIndex].AddMeshSection(mesh.GetVertexData(index[0]), mesh.GetVertexData(index[1]), mesh.GetVertexData(index[2]));
            return true;
        }

        bool TryMergeHelper(int index1, int index2)
        {
            Debug.Log("�}�[�W�����s");

            // �}�[�W���̃��b�V���O���[�v�C���f�b�N�X�擾
            if (!hasContain[index1])
            {
                Debug.LogError("�}�[�W���C���f�b�N�X�����b�V���O���[�v���w���Ă��܂���");
                return false;
            }
            int receiverIndex = verticesDic[v[index1]];

            // �}�[�W�Ώۂ̃��b�V���O���[�v�C���f�b�N�X�擾
            if (!hasContain[index2])
            {
                verticesDic.Add(v[index2], receiverIndex);
                Debug.Log("�}�[�W�ΏۃC���f�b�N�X�����b�V���O���[�v���w���Ă��Ȃ������̂ōX�V���ďI�����܂�");
                return false;
            }
            int targetIndex = verticesDic[v[index2]];

            // ���łɓ����O���[�v�Ȃ牽�����Ȃ�
            if (meshGroup[receiverIndex] == meshGroup[targetIndex])
            {
                Debug.Log("�������b�V���O���[�v���}�[�W���悤�Ƃ��܂���(" + receiverIndex + ", " + targetIndex + ")");
                return false;
            }

            Debug.LogWarning("����Ƀ}�[�W���J�n���܂�");

            counttmp++;
            // ���b�V��������
            meshGroup[receiverIndex].MergeHelper(meshGroup[targetIndex]);
            #region
            /*
                * ��FmeshGroup[0]��[1]���}�[�W������A
                * ���̓�͓���ү����ٰ�߂��w���悤�ɂȂ�B
                * ���̎��_�ł͖��͂Ȃ��B���̂���
                * [1]��[2]���}�[�W�����ꍇ�A[1]��[2]�͓���
                * ү����ٰ�߂��w���悤�ɂȂ邪�A���Ƃ���
                * [0]��[1]�͓���ү����ٰ�߂��w���Ă����̂ɁA
                * [1]����[2]�̐V����ү����ٰ�߂��w���悤�ɂȂ����̂ŁA
                * [0]��[1]�͈قȂ�C���X�^���X���w�����ƂɂȂ�B
                * ���̏�����[0]��[1]��[2]�𓯂�ү����ٰ�߂�
                * �w���悤�ɂ��鏈���B
                */
            #endregion
            // �w�����b�V���O���[�v�X�V
            for (int i = 0; i < meshGroup.Count; i++)
            {
                Debug.Log("�}�[�W���[�v " + i + "/" + meshGroup.Count + "���");
                // �}�[�W�Ɏg�����C���f�b�N�X�ƈقȂ�
                if (targetIndex != i)
                {
                    // �}�[�W�Ɏg�������b�V���O���[�v�Ɠ���ł���
                    if (meshGroup[i] == meshGroup[targetIndex])
                    {
                        Debug.Log("�}�[�W���܂���(" + receiverIndex + ", " + i + ")");
                        // �}�[�W���̃��b�V���O���[�v���w���悤�ɍX�V
                        meshGroup[i] = meshGroup[receiverIndex];
                    }
                }
            }
            // �Q�ƌ^���ۂ�����for���ƂōX�V
            meshGroup[targetIndex] = meshGroup[receiverIndex];
            Debug.Log("�}�[�W���܂���(" + receiverIndex + ", " + targetIndex + ")");
            // �L�[�����X�g�ɕϊ����ĉ�
            foreach (var key in verticesDic.Keys.ToList())
            {
                if (verticesDic[key] == targetIndex)
                {
                    verticesDic[key] = receiverIndex;
                }
            }
            return true;
        }

        //bool Add(int index1, int index2)
        //{
        //    // ���b�V���O���[�v�̃C���f�b�N�X
        //    int meshGroupIndex = containIndex[index2];
        //    verticesDic.Add(v[index1], meshGroupIndex);
        //    return true;
        //}
        #endregion

        Debug.Log("�|���S���� : " + mesh.triangles.Count / 3);
        Debug.Log("���_�� : " + mesh.vertices.Count);
        // �|���S�����Ƃɑ���
        for (int i = 0; i < mesh.triangles.Count; i += 3)
        {
            // �C���f�b�N�X
            index = new int[3];
            index[0] = mesh.triangles[i];
            index[1] = mesh.triangles[i + 1];
            index[2] = mesh.triangles[i + 2];
            // ���_���W
            v = new Vector3[3];
            v[0] = mesh.vertices[index[0]];
            v[1] = mesh.vertices[index[1]];
            v[2] = mesh.vertices[index[2]];
            // ���b�V���O���[�v�C���f�b�N�X
            containIndex = new int[3];
            containIndex[0] = 0;
            containIndex[1] = 0;
            containIndex[2] = 0;
            // ���b�V���O���[�v�ɏ������Ă��邩
            hasContain = new bool[3];
            hasContain[0] = verticesDic.TryGetValue(v[0], out containIndex[0]);
            hasContain[1] = verticesDic.TryGetValue(v[1], out containIndex[1]);
            hasContain[2] = verticesDic.TryGetValue(v[2], out containIndex[2]);

            //int containNum = 0;

            //// ���b�V���O���[�v�Ɋ���U��ς݂Ȃ�J�E���^���C���N�������g
            //// ����U���Ă��Ȃ�������-1�ɃZ�b�g
            //// ������ԂŕԂ��Ă���O��ү����ٰ�߲��ޯ���O�Əd�����邽��
            //if (!hasContain[0]) { containIndex[0] = -1; } else { containNum++; }
            //if (!hasContain[1]) { containIndex[1] = -1; } else { containNum++; }
            //if (!hasContain[2]) { containIndex[2] = -1; } else { containNum++; }

            // ����U���Ă��Ȃ�������-1�ɃZ�b�g
            // ������ԂŕԂ��Ă���O��ү����ٰ�߲��ޯ���O�Əd�����邽��
            if (!hasContain[0]) { containIndex[0] = -1; }
            if (!hasContain[1]) { containIndex[1] = -1; }
            if (!hasContain[2]) { containIndex[2] = -1; }

            Debug.Log("�|���S������ " + ((i / 3) + 1) + " ���" + " (" + containIndex[0] + ", " + containIndex[1] + ", " + containIndex[2] + ")");

            // ���b�V���O���[�v������U���Ă���C���f�b�N�X�������ď���
            if (hasContain[0])
            {
                TryMergeHelper(0, 1);
                TryMergeHelper(0, 2);
                AddMeshSection(0);
            }
            else if (hasContain[1])
            {
                TryMergeHelper(1, 0);
                TryMergeHelper(1, 2);
                AddMeshSection(1);
            }
            else if (hasContain[2])
            {
                TryMergeHelper(2, 0);
                TryMergeHelper(2, 1);
                AddMeshSection(2);
            }
            // �ǂ̒��_�����b�V���O���[�v������U���Ă��Ȃ�
            else
            {
                // �V�������b�V���O���[�v���쐬
                MeshConstructionHelper helper = new MeshConstructionHelper();
                helper.AddMeshSection(mesh.GetVertexData(index[0]), mesh.GetVertexData(index[1]), mesh.GetVertexData(index[2]));
                meshGroup.Add(helper);
                int newGroupIndex = meshGroup.Count - 1;
                // ���_�ɑΉ��C���f�b�N�X������U��
                verticesDic.Add(v[0], newGroupIndex);
                verticesDic.Add(v[1], newGroupIndex);
                verticesDic.Add(v[2], newGroupIndex);
            }

            #region
            //string Log = "";

            //switch (containNum)
            //{
            //    case 3:     // ���ׂĂ̒��_�����b�V���O���[�v�Ɋ���U��ς�
            //        {
            //            #region
            //            Log += " ���ׂĂ̒��_�����b�V���O���[�v�Ɋ���U��ς�" + "( " + containIndex[0] + ", " + containIndex[1] + ", " + containIndex[2] + " )";

            //            // ���ׂĂ̒��_���������b�V���O���[�v
            //            if (containIndex[0] == containIndex[1] && containIndex[1] == containIndex[2])
            //            {
            //                Debug.Log(Log + " ���ׂĂ̒��_���������b�V���O���[�v");
            //            }
            //            // ���ׂĂ̒��_�����ꂼ��ʂ̃��b�V�����w���Ă���
            //            else if
            //                (containIndex[0] != containIndex[1]
            //                &&
            //                containIndex[1] != containIndex[2]
            //                &&
            //                containIndex[0] != containIndex[2])
            //            {
            //                Debug.Log(Log + " ���ׂĂ̒��_�����ꂼ��قȂ郁�b�V���O���[�v" + "( " + containIndex[0] + ", " + containIndex[1] + ", " + containIndex[2] + " )");
            //            }
            //            // �Q�̒��_���������b�V���O���[�v���w���Ă���
            //            else
            //            {
            //                // ���_�P�ƒ��_�Q���������b�V���O���[�v
            //                if (containIndex[0] == containIndex[1])
            //                {
            //                    Debug.Log(Log + " ���_�P�ƒ��_�Q���������b�V���O���[�v");
            //                }
            //                // ���_�Q�ƒ��_�R���������b�V���O���[�v
            //                else if (containIndex[1] == containIndex[2])
            //                {
            //                    Debug.Log(Log + " ���_�Q�ƒ��_�R���������b�V���O���[�v");
            //                }
            //                // ���_�P�ƒ��_�R���������b�V���O���[�v
            //                else
            //                {
            //                    Debug.Log(Log + " ���_�P�ƒ��_�R���������b�V���O���[�v");
            //                }
            //            }
            //            #endregion
            //            // �璷�ɂ��Ȃ��Ă����ꂾ���œ����H
            //            MergeHelper(0, 1);
            //            MergeHelper(1, 2);
            //            AddMeshSection(0);
            //        }
            //        break;
            //    case 2:     // �����ꂩ�Q�̒��_�����b�V���O���[�v�Ɋ���U��ς�
            //        {
            //            Log +=" �����ꂩ�Q�̒��_�����b�V���O���[�v�Ɋ���U��ς�" + "( " + containIndex[0] + ", " + containIndex[1] + ", " + containIndex[2] + " )";

            //            // ��̊���U��ςݒ��_��T��

            //            // ���_�P�ƒ��_�Q������U��ς�
            //            if (hasContain[0] && hasContain[1])
            //            {
            //                // �w�����b�V���O���[�v������Ȃ�P���Ƀ|���S���ǉ�
            //                if (containIndex[0] == containIndex[1])
            //                {
            //                    Log += " ���_�P�ƒ��_�Q������̃��b�V���O���[�v���w���Ă���";
            //                    Debug.Log(Log + " �P���ɒǉ�");
            //                    // ������U��̒��_�Ƀ��b�V������U��
            //                    Add(2, 0);
            //                    // �|���S���ǉ�
            //                    AddMeshSection(0);
            //                }
            //                // ����łȂ���ΕЕ��̃��b�V���O���[�v�ƌ������ă|���S���ǉ�
            //                else
            //                {
            //                    Log += " ���_�P�ƒ��_�Q���ʁX�̃��b�V���O���[�v���w���Ă���";
            //                    Debug.Log(Log + " �������Ēǉ�");
            //                    // ������U��̒��_�Ƀ��b�V������U��
            //                    Add(2, 0);
            //                    // ���b�V��������
            //                    MergeHelper(0, 1);
            //                    // �|���S���ǉ�
            //                    AddMeshSection(0);
            //                }
            //            }
            //            // ���_�P�ƒ��_�Q������U��ς�
            //            else if (hasContain[0] && hasContain[2])
            //            {
            //                if (containIndex[0] == containIndex[2])
            //                {
            //                    Log += " ���_�P�ƒ��_�R������̃��b�V���O���[�v���w���Ă���";
            //                    Debug.Log(Log + "�P���ɒǉ�");
            //                    Add(1, 0);
            //                    AddMeshSection(0);
            //                }
            //                else
            //                {
            //                    Log += " ���_�P�ƒ��_�R���ʁX�̃��b�V���O���[�v���w���Ă���";
            //                    Debug.Log(Log + " �������Ēǉ�");
            //                    Add(1, 0);
            //                    MergeHelper(0, 2);
            //                    AddMeshSection(0);
            //                }
            //            }
            //            // ���_�Q�ƒ��_�R������U��ς�
            //            else
            //            {
            //                if (containIndex[1] == containIndex[2])
            //                {
            //                    Log += " ���_�Q�ƒ��_�R������̃��b�V���O���[�v���w���Ă���";
            //                    Debug.Log(Log + " �P���ɒǉ�");
            //                    Add(0, 1);
            //                    AddMeshSection(1);
            //                }
            //                else
            //                {
            //                    Log += " ���_�Q�ƒ��_�R���ʁX�̃��b�V���O���[�v���w���Ă���";
            //                    Debug.Log(Log + " �������Ēǉ�");
            //                    Add(0, 1);
            //                    MergeHelper(1, 2);
            //                    AddMeshSection(1);
            //                }
            //            }
            //        }
            //        break;
            //    case 1:     // �����ꂩ�P�̒��_�����b�V���O���[�v�Ɋ���U��ς�
            //        {
            //            Log += " �����ꂩ�P�̒��_�����b�V���O���[�v�Ɋ���U��ς�" + "( " + containIndex[0] + ", " + containIndex[1] + ", " + containIndex[2] + " )";

            //            if (hasContain[0])
            //            {
            //                Log += " ���_�P�����b�V���O���[�v�Ɋ���U���Ă���";
            //                Add(1, 0);
            //                Add(2, 0);
            //                AddMeshSection(0);
            //                Debug.Log(Log + " �P���ɒǉ�");
            //            }
            //            else if (hasContain[1])
            //            {
            //                Log += " ���_�Q�����b�V���O���[�v�Ɋ���U���Ă���";
            //                Add(0, 1);
            //                Add(2, 1);
            //                AddMeshSection(1);
            //                Debug.Log(Log + "�P���ɒǉ�");
            //            }
            //            else if (hasContain[2])
            //            {
            //                Log += " ���_�R�����b�V���O���[�v�Ɋ���U���Ă���";
            //                Add(0, 2);
            //                Add(1, 2);
            //                AddMeshSection(2);
            //                Debug.Log(Log + " �P���ɒǉ�");
            //            }
            //        }
            //        break;
            //    case 0:     // �ǂ̒��_�����b�V���O���[�v�Ɋ���U���Ă��Ȃ�
            //        {
            //            Log += " �ǂ̒��_�����b�V���O���[�v�Ɋ���U���Ă��Ȃ�" + "( " + containIndex[0] + ", " + containIndex[1] + ", " + containIndex[2] + " )";
            //            // �V�������b�V���O���[�v���쐬
            //            MeshConstructionHelper helper = new MeshConstructionHelper();
            //            helper.AddMeshSection(mesh.GetVertexData(index[0]), mesh.GetVertexData(index[1]), mesh.GetVertexData(index[2]));
            //            meshGroup.Add(helper);
            //            int newGroupIndex = meshGroup.Count - 1;
            //            // ���_�ɑΉ��C���f�b�N�X������U��
            //            verticesDic.Add(v[0], newGroupIndex);
            //            verticesDic.Add(v[1], newGroupIndex);
            //            verticesDic.Add(v[2], newGroupIndex);
            //            Debug.Log(Log + " ���b�V���O���[�v�ǉ�");
            //        }
            //        break;
            //}
            #endregion

            if (!verticesDic.ContainsKey(v[0]))
            {
                Debug.LogWarning("���_�P���ǉ����ꂸ�Ɋ���U�肪�I�����܂���");
            }
            if (!verticesDic.ContainsKey(v[1]))
            {
                Debug.LogWarning("���_�Q���ǉ����ꂸ�Ɋ���U�肪�I�����܂���");
            }
            if (!verticesDic.ContainsKey(v[2]))
            {
                Debug.LogWarning("���_�R���ǉ����ꂸ�Ɋ���U�肪�I�����܂���");
            }
        }

        List<MeshConstructionHelper> resultMeshList = new List<MeshConstructionHelper>();
        List<MeshConstructionHelper> closedList = new List<MeshConstructionHelper>();

        // ���b�V���O���[�v�̒ǉ�
        foreach(var obj in meshGroup)
        {
            // �܂��ǉ����ĂȂ����b�V���O���[�v
            if (!closedList.Contains(obj))
            {
                closedList.Add(obj);
                resultMeshList.Add(obj);
                continue;
            }
            counttmp++;
        }
        Debug.Log("���b�V���O���[�v�̐� : " + resultMeshList.Count);
        Debug.Log("�������ꂽ���b�V���O���[�v�̐� : " + (meshGroup.Count - resultMeshList.Count));

        // �ؒf�ʂɐڂ������_�̃��b�V���O���[�v�z��
        List<List<VertexData>> pointsAlongPlaneList = new List<List<VertexData>>(resultMeshList.Count);
        for (int i = 0; i < resultMeshList.Count; i++)
        {
            pointsAlongPlaneList.Add(new List<VertexData>());
        }
        Debug.Log(pointsAlongPlaneList.Count);
        // �����Ώۂ̒��_�ƈ�v����A����U��ꂽ���_�̎w�����b�V���O���[�v��o�^
        foreach (var obj in pointsAlongPlane)
        {
            // meshGroup[meshGroupIndex]���w���C���X�^���X��resultMeshList�̂ǂ��ɂ��邩�������A���̃C���f�b�N�X��pointsAlongPlaneList�ɓn��
            int meshGroupIndex;
            if(verticesDic.TryGetValue(obj.Position, out meshGroupIndex))
            {
                // �w�����b�V���O���[�v���擾
                var targetMeshGroup = meshGroup[meshGroupIndex];
                // ���b�V���O���[�v������C���f�b�N�X���擾
                int resultMeshIndex = resultMeshList.IndexOf(targetMeshGroup);
                if (resultMeshIndex >= 0)
                {
                    pointsAlongPlaneList[resultMeshIndex].Add(obj);
                }
                else
                {
                    Debug.LogError("�w�肳�ꂽ���b�V���O���[�v�����݂��܂���");
                }
            }
            else
            {
                Debug.LogError("���_���ǂ̃��b�V���O���[�v�ɂ����݂��܂���");
                if(mesh.vertices.Contains(obj.Position))
                {
                    Debug.LogError("���_�͏����O�̃��b�V���ɑ��݂��Ă��܂���");
                }
            }
        }
        Debug.Log("�I�����܂���");
        return (resultMeshList, pointsAlongPlaneList);
    }

    public static Vector3 ComputeHalfPoint(List<VertexData> vertices)
    {
        if (vertices.Count > 0)
        {
            Vector3 center = Vector3.zero;

            foreach(VertexData point in vertices)
            {
                center += point.Position;
            }

            // ����𒸓_���̍��v�Ŋ���A���S�Ƃ���
            return center / vertices.Count;
        }
        else
        {
            return Vector3.zero;
        }
    }

    public static Vector3 ComputeNormal(VertexData vertexA, VertexData vertexB, VertexData vertexC)
    {
        Vector3 sideL = vertexB.Position - vertexA.Position;
        Vector3 sideR = vertexC.Position - vertexA.Position;

        Vector3 normal = Vector3.Cross(sideL, sideR);

        return normal.normalized;
    }


    // �ؒf�ʂ̍\�z
    private static void FillCutSurface(GameObject subject, ref MeshConstructionHelper mesh, Vector3 cutNormal, List<VertexData> pointsAlongPlane)
    {
        // �ؒf�ʂ̒��S�_���Z�o
        VertexData halfPoint = new VertexData()
        {
            Position = ComputeHalfPoint(pointsAlongPlane),
            Normal = cutNormal,
            Uv = new Vector3(0.5f, 0.5f)
        };

        // �Z�o�������S�_�{�Q���_�Ń|���S�����`��
        for (int i = 0; i < pointsAlongPlane.Count; i += 2)
        {
            VertexData v1 = pointsAlongPlane[i];
            VertexData v2 = pointsAlongPlane[(i + 1) % pointsAlongPlane.Count];
            VertexData v3 = halfPoint;

            Vector3 normal = subject.transform.InverseTransformDirection(ComputeNormal(v3, v2, v1));
            Vector3 localCutNormal = subject.transform.InverseTransformDirection(cutNormal); // cutNormal �����[�J���ɕϊ�
            float dot = Vector3.Dot(normal, localCutNormal);
            // ���_�̏��Ԃ𔽎��v���ɂȂ�悤�Ƀ|���S���ǉ�
            if (dot > 0)
            {
				v1.Normal = -normal;
				v2.Normal = -normal;
				v3.Normal = -normal;
				mesh.AddMeshSection(v1, v2, v3);
            }
            else
            {
				v1.Normal = normal;
				v2.Normal = normal;
				v3.Normal = normal;
				mesh.AddMeshSection(v2, v1, v3);
            }
        }
    }

    #region
    private void OnDrawGizmos()
    {
        //Gizmos.color = UnityEngine.Color.red;
        //planePosition = planeTransform.position;
        //planeNormal = planeTransform.up;
        //plane = new Plane(planeNormal, planePosition);

        //// Plane�̖@���x�N�g������������`��
        //Gizmos.DrawLine(_planeTransform.position, _planeTransform.position + _planeTransform.up * 5); // �@�������ɐ���`��

        //// Plane�̈ʒu�����������ȋ���`��
        //Gizmos.color = UnityEngine.Color.green;
        //Gizmos.DrawSphere(_planeTransform.position, 0.1f); // Plane�̈ʒu�ɋ���`��

        //Gizmos.matrix = Matrix4x4.TRS(_planePosition, Quaternion.LookRotation(_planeNormal), Vector3.one);
        //Gizmos.color = new Color(0, 1, 0, 0.4f);
        //Gizmos.DrawCube(Vector3.zero, new Vector3(2, 2, 0f)); // Matrix�Ɋ�Â��ʒu�ɕ`��
        //Gizmos.color = new Color(0, 1, 0, 1f);
        //Gizmos.DrawWireCube(Vector3.zero, new Vector3(2, 2, 0f)); // Matrix�Ɋ�Â��ʒu�ɕ`��
        //Gizmos.matrix = transform.localToWorldMatrix;
    }
    #endregion
}