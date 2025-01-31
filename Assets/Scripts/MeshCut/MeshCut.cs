using JetBrains.Annotations;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml.Schema;
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
    private static MeshFilter meshFilter;
    private static List<Vector3> vertices;
    private static List<Vector3> normals;
    private static List<Vector2> uvs;
    private static List<int> triangles;

    public static void ClearAll()
    {
        vertices.Clear();
        normals.Clear();
        uvs.Clear();
        triangles.Clear();
    }

    // ���b�V�����擾
    public static void GetMeshInfo(GameObject subject)
    {
        meshFilter = subject.GetComponent<MeshFilter>();
        vertices = new List<Vector3>(meshFilter.sharedMesh.vertices);
        normals = new List<Vector3>(meshFilter.sharedMesh.normals);
        uvs = new List<Vector2>(meshFilter.sharedMesh.uv);
        triangles = new List<int>(meshFilter.sharedMesh.triangles);
        // ���_�̈ʒu�����[���h��Ԃɕϊ�
        for(int i = 0; i < vertices.Count;i++)
        {
            vertices[i] = subject.transform.TransformPoint(vertices[i]);
        }
    }

    public static VertexData GetVertexData(int index)
    {
        VertexData vert = new VertexData();
        vert.Position = vertices[index];
        vert.Uv = uvs[index];
        vert.Normal = normals[index];
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
        var pos1 = vertices[indexA];
        var uv1 = uvs[indexA];
        var nrm1 = normals[indexA];
        var pos2 = vertices[indexB];
        var uv2 = uvs[indexB];
        var nrm2 = normals[indexB];
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
        for (int i = 0; i < triangles.Count; i += 3)
        {
            // �O�p�`���ۗL���钸�_�̃C���f�b�N�X���擾
            int indexA = triangles[i];
            int indexB = triangles[i+1];
            int indexC = triangles[i+2];

            // ���_�f�[�^�擾
            var vA = GetVertexData(indexA);
            var vB = GetVertexData(indexB);
            var vC = GetVertexData(indexC);

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

    public static List<MeshConstructionHelper> CutDevide
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
        for (int i = 0; i < triangles.Count; i += 3)
        {
            // �O�p�`���ۗL���钸�_�̃C���f�b�N�X���擾
            int indexA = triangles[i];
            int indexB = triangles[i+1];
            int indexC = triangles[i+2];

            // ���_�f�[�^�擾
            var vA = GetVertexData(indexA);
            var vB = GetVertexData(indexB);
            var vC = GetVertexData(indexC);

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
        var positiveResult = MeshDivision(positiveMesh, pointsAlongPlane);
        var negativeResult = MeshDivision(negativeMesh, pointsAlongPlane);

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

    public static (List<MeshConstructionHelper> meshList, List<List<VertexData>> pointsAlongPlaneList)
        MeshDivision(MeshConstructionHelper mesh, List<VertexData> pointsAlongPlane)
    {
        List<MeshConstructionHelper> meshGroup = new List<MeshConstructionHelper>();
        Dictionary<Vector3, int> verticesDic = new Dictionary<Vector3, int>();

        // �|���S�����Ƃɑ���
        for (int i = 0; i < mesh.triangles.Count; i += 3)
        {
            // �C���f�b�N�X
            int[] index = new int[3];
            index[0] = mesh.triangles[i];
            index[1] = mesh.triangles[i + 1];
            index[2] = mesh.triangles[i + 2];
            // ���_���W
            Vector3[] v = new Vector3[3];
            v[0] = mesh.vertices[index[0]];
            v[1] = mesh.vertices[index[1]];
            v[2] = mesh.vertices[index[2]];
            // ���b�V���O���[�v�C���f�b�N�X
            int[] containIndex = new int[3];
            containIndex[0] = 0;
            containIndex[1] = 0;
            containIndex[2] = 0;
            // ���b�V���O���[�v�ɏ������Ă��邩
            bool[] hasContain = new bool[3];
            hasContain[0] = verticesDic.TryGetValue(v[0], out containIndex[0]);
            hasContain[1] = verticesDic.TryGetValue(v[1], out containIndex[1]);
            hasContain[2] = verticesDic.TryGetValue(v[2], out containIndex[2]);

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
            // ���ׂĂ̒��_�����b�V���O���[�v�Ɋ���U��ς�
            if (hasContain[0] && hasContain[1] && hasContain[2])
            {
                Debug.Log("�Ă΂�");

            }
            // �����ꂩ�̒��_�����b�V���O���[�v�Ɋ���U��ς�
            else if (hasContain[0] || hasContain[1] || hasContain[2])
            {
                Debug.Log("�Ă΂�1");
                // ��̊���U��ςݒ��_��T��
                if (hasContain[0] && hasContain[1])
                {
                    // �w�����b�V��������Ȃ�P���Ƀ|���S���ǉ�
                    if (containIndex[0] == containIndex[1])
                    {
                        // ������U��̒��_�Ƀ��b�V������U��
                        verticesDic.Add(v[2], containIndex[0]);
                        // �|���S���ǉ�
                        meshGroup[containIndex[0]].AddMeshSection(GetVertexData(index[0]), GetVertexData(index[1]), GetVertexData(index[2]));
                    }
                    else
                    {
                        // ������U��̒��_�Ƀ��b�V������U��
                        verticesDic.Add(v[2], containIndex[0]);
                        // ���b�V��������
                        meshGroup[containIndex[0]].MergeHelper(meshGroup[containIndex[1]]);
                        // �w�����b�V���𓯈�ɂ���
                        verticesDic[v[1]] = containIndex[0];
                        // �|���S���ǉ�
                        meshGroup[containIndex[0]].AddMeshSection(GetVertexData(index[0]), GetVertexData(index[1]), GetVertexData(index[2]));
                    }
                }
                if (hasContain[0] && hasContain[2])
                {
                    if (containIndex[0] == containIndex[2])
                    {
                        verticesDic.Add(v[1], containIndex[0]);
                        meshGroup[containIndex[0]].AddMeshSection(GetVertexData(index[0]), GetVertexData(index[1]), GetVertexData(index[2]));
                    }
                    else
                    {
                        verticesDic.Add(v[1], containIndex[0]);
                        meshGroup[containIndex[0]].MergeHelper(meshGroup[containIndex[2]]);
                        verticesDic[v[2]] = containIndex[0];
                        meshGroup[containIndex[0]].AddMeshSection(GetVertexData(index[0]), GetVertexData(index[1]), GetVertexData(index[2]));
                    }
                }
                if (hasContain[1] && hasContain[2])
                {
                    if (containIndex[1] == containIndex[2])
                    {
                        verticesDic.Add(v[0], containIndex[1]);
                        meshGroup[containIndex[1]].AddMeshSection(GetVertexData(index[0]), GetVertexData(index[1]), GetVertexData(index[2]));
                    }
                    else
                    {
                        verticesDic.Add(v[0], containIndex[1]);
                        meshGroup[containIndex[1]].MergeHelper(meshGroup[containIndex[2]]);
                        verticesDic[v[2]] = containIndex[1];
                        meshGroup[containIndex[1]].AddMeshSection(GetVertexData(index[0]), GetVertexData(index[1]), GetVertexData(index[2]));
                    }
                }
            }
            // �ǂ̒��_�����b�V���O���[�v�Ɋ���U���Ă��Ȃ�
            else
            {
                Debug.Log("�Ă΂�2");
                // �V�������b�V���O���[�v���쐬
                MeshConstructionHelper helper = new MeshConstructionHelper();
                helper.AddMeshSection(GetVertexData(index[0]), GetVertexData(index[1]), GetVertexData(index[2]));
                meshGroup.Add(helper);
                int newGroupIndex = meshGroup.Count - 1; ;
                // ���_�ɑΉ��C���f�b�N�X������U��
                verticesDic.Add(v[0], newGroupIndex);
                verticesDic.Add(v[1], newGroupIndex);
                verticesDic.Add(v[2], newGroupIndex);
            }
        }
        Debug.Log("�Ă΂�3");
        List<MeshConstructionHelper> resultMeshList = new List<MeshConstructionHelper>();
        List<int> closedList = new List<int>();
        foreach(var obj in verticesDic)
        {
            if (!closedList.Contains(obj.Value))
            {
                closedList.Add(obj.Value);
                resultMeshList.Add(meshGroup[obj.Value]);
            }
        }
        List<List<VertexData>> pointsAlongPlaneResult = new List<List<VertexData>>();
        foreach (var v in pointsAlongPlane)
        {
            int index = verticesDic[v.Position];
            pointsAlongPlaneResult[index].Add(v);
        }

        return (resultMeshList, pointsAlongPlaneResult);
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