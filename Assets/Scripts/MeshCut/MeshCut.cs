using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static MeshCut;

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
            Uv = new Vector3(0.5f, 0.5f)
        };

        // �Z�o�������S�_�{�Q���_�Ń|���S�����`��
        for (int i = 0; i < pointsAlongPlane.Count; i += 2)
        {
            VertexData v1 = pointsAlongPlane[i];
            VertexData v2 = pointsAlongPlane[(i + 1) % pointsAlongPlane.Count];
            VertexData v3 = halfPoint;

            Vector3 normal = subject.transform.InverseTransformDirection(ComputeNormal(halfPoint, v2, v1));

            float dot = Vector3.Dot(normal, cutNormal);

            if (dot > 0)
            {
				v1.Normal = -normal;
				v2.Normal = -normal;
				v3.Normal = -normal;
				mesh.AddMeshSection(v1, v2, halfPoint);
            }
            else
            {
				v1.Normal = normal;
				v2.Normal = normal;
				v3.Normal = normal;
				mesh.AddMeshSection(v2, v1, halfPoint);
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