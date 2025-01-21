using System.Collections.Generic;
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
    [SerializeField]
    private GameObject _subject, cutter;

    public struct VertexData
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Uv;
    }

    // ��
    [SerializeField] private Transform _planeTransform;
    [SerializeField] private Vector3 _planePosition;
    [SerializeField] private Vector3 _planeNormal;
    // ����������
    private Plane _plane;

    // ���b�V��
    private MeshFilter _meshFilter;
    private List<Vector3> _vertices;
    private List<Vector3> _normals;
    private List<Vector2> _uvs;
    private List<int> _triangles;
    private List<List<int>> _subIndices;

    public void ClearAll()
    {
        _vertices.Clear();
        _normals.Clear();
        _uvs.Clear();
        _triangles.Clear();
        _subIndices.Clear();
    }

    // ���b�V�����擾
    public void GetMeshInfo(GameObject subject)
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

    public VertexData GetVertexData(int index)
    {
        VertexData vert = new VertexData();
        vert.Position =  _vertices[index];
        vert.Uv = _uvs[index];
        vert.Normal =  _normals[index];
        return vert;
    }

    // ��_�ւ̋����̎Z�o
    public float ComputeIntersection(Vector3 p1, Vector3 p2)
    {
        Ray ray = new Ray(p1, p2 - p1);
        float enter;
        if (_plane.Raycast(ray, out enter))
        {
            var normalizedDistance = enter / (p2 - p1).magnitude;
            return normalizedDistance;
        }
        return 0f;
    }

    // ��_�ɒ��_�𐶐�
    public VertexData ComputeIntersectionVertex(int indexA, int indexB)
    {
        VertexData vert = new VertexData();
        var pos1 = _vertices[indexA];
        var uv1 = _uvs[indexA];
        var nrm1 = _normals[indexA];
        var pos2 = _vertices[indexB];
        var uv2 = _uvs[indexB];
        var nrm2 = _normals[indexB];
        var normalizedDistance = ComputeIntersection(pos1, pos2);
        vert.Position = Vector3.Lerp(pos1, pos2, normalizedDistance);
        vert.Normal = Vector3.Lerp(nrm1, nrm2, normalizedDistance);
        vert.Uv = Vector2.Lerp(uv1, uv2, normalizedDistance);
        return vert;

    }

    // �ʂ̕\���ǂ���Ɉʒu���邩�擾����
    bool ComputeSide(Vector3 p)
    {
        return _plane.GetSide(p);
    }

    public void Start()
    {
        MeshConstructionHelper.ClearMesh();
        GetMeshInfo(_subject);
        Cut(_subject);

        // ���̃I�u�W�F�N�g�𕡐�
        //GameObject positiveObject = Instantiate(_subject);
        GameObject negativeObject = Instantiate(_subject);

        // ���������I�u�W�F�N�g�ɐV�������b�V����K�p
        var objectName = _subject.name;
        _subject.name = objectName + "_Positive";
        negativeObject.name = objectName + "_Negative";

        MeshConstructionHelper.positiveMesh.InverseTransformPoints(_subject);
        MeshConstructionHelper.negativeMesh.InverseTransformPoints(_subject);

        _subject.GetComponent<MeshFilter>().mesh = MeshConstructionHelper.positiveMesh.ConstructMesh();
        negativeObject.GetComponent<MeshFilter>().mesh = MeshConstructionHelper.negativeMesh.ConstructMesh();

        _subject.GetComponent<MeshCollider>().sharedMesh = _subject.GetComponent<MeshFilter>().mesh;
        negativeObject.GetComponent<MeshCollider>().sharedMesh = negativeObject.GetComponent<MeshFilter>().mesh;
    }

    public void Cut(GameObject subject)
    {
        GetMeshInfo(subject);
        _planeNormal = _planeTransform.up;
        _planePosition = _planeTransform.position;
        _plane = new Plane(_planeNormal, _planePosition); 
        // �ؒf�ʂɉ������_�̃��X�g
        List<VertexData> pointsAlongPlane = new List<VertexData>();

        bool[] sides = new bool[3];
        // �|���S���͎O�̒��_�ō\�������̂łR�Â������đ���
        for (int i = 0; i < _triangles.Count; i += 3)
        {
            // �O�p�`���ۗL���钸�_�̃C���f�b�N�X���擾
            int indexA = _triangles[i];
            int indexB = _triangles[i+1];
            int indexC = _triangles[i+2];

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
                MeshConstructionHelper helper = sides[0] ? MeshConstructionHelper.positiveMesh : MeshConstructionHelper.negativeMesh;
                helper.AddMeshSection(vA, vB, vC);
            }
            else
            {
                VertexData intersectionD;
                VertexData intersectionE;
                MeshConstructionHelper helperA = sides[0] ? MeshConstructionHelper.positiveMesh : MeshConstructionHelper.negativeMesh;
                MeshConstructionHelper helperB = sides[1] ? MeshConstructionHelper.positiveMesh : MeshConstructionHelper.negativeMesh;
                MeshConstructionHelper helperC = sides[2] ? MeshConstructionHelper.positiveMesh : MeshConstructionHelper.negativeMesh;
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
        FillCutSurface(ref MeshConstructionHelper.positiveMesh, ref MeshConstructionHelper.negativeMesh, _planeNormal, pointsAlongPlane);
    }

    public Vector3 ComputeHalfPoint(List<VertexData> vertices)
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

    public Vector3 ComputeNormal(VertexData vertexA, VertexData vertexB, VertexData vertexC)
    {
        Vector3 sideL = vertexB.Position - vertexA.Position;
        Vector3 sideR = vertexC.Position - vertexA.Position;

        Vector3 normal = Vector3.Cross(sideL, sideR);

        return normal.normalized;
    }


    // �ؒf�ʂ̍\�z
    private void FillCutSurface(ref MeshConstructionHelper positive, ref MeshConstructionHelper negative, Vector3 cutNormal, List<VertexData> pointsAlongPlane)
    {
        //// �ؒf�ʂ̒��S�_���Z�o
        //VertexData halfPoint = new VertexData()
        //{
        //    Position = ComputeHalfPoint(pointsAlongPlane),
        //    Uv = new Vector3(0.5f, 0.5f)
        //};

        //// �Z�o�������S�_�{�Q���_�Ń|���S�����`��
        //for (int i = 0; i < pointsAlongPlane.Count; i += 2)
        //{
        //    VertexData v1 = pointsAlongPlane[i];
        //    VertexData v2 = pointsAlongPlane[(i + 1) % pointsAlongPlane.Count];

        //    Vector3 normal = _subject.transform.TransformDirection(ComputeNormal(halfPoint, v2, v1));
        //    halfPoint.Normal = normal;
        //    v1.Normal = normal;
        //    v2.Normal = normal;

        //    float dot = Vector3.Dot(normal, cutNormal);

        //    if (dot > 0)
        //    {
        //        positive.AddMeshSection(v1, v2, halfPoint);
        //        negative.AddMeshSection(v2, v1, halfPoint);
        //    }
        //    else
        //    {
        //        positive.AddMeshSection(v2, v1, halfPoint);
        //        negative.AddMeshSection(v1, v2, halfPoint);
        //    }
        //}
        // center of the cap
        // �J�b�g���ʂ̒��S�_���v�Z����
        Vector3 center = Vector3.zero;

        // �����œn���ꂽ���_�ʒu�����ׂč��v����
        foreach (Vector3 point in pointsAlongPlane)
        {
            center += point;
        }

        // ����𒸓_���̍��v�Ŋ���A���S�Ƃ���
        center = center / pointsAlongPlane.Count;

        // you need an axis based on the cap
        // �J�b�g���ʂ��x�[�X�ɂ���upward
        Vector3 upward = Vector3.zero;

        // 90 degree turn
        // �J�b�g���ʂ̖@���𗘗p���āA�u��v���������߂�
        // ��̓I�ɂ́A���ʂ̍�������Ƃ��ė��p����
        upward.x = cutNormal.y;
        upward.y = -cutNormal.x;
        upward.z = cutNormal.z;

        // �@���Ɓu������v����A�������Z�o
        Vector3 left = Vector3.Cross(cutNormal, upward);

        Vector3 displacement = Vector3.zero;
        Vector3 newUV1 = Vector3.zero;
        Vector3 newUV2 = Vector3.zero;

        // �����ŗ^����ꂽ���_�����[�v����
        for (int i = 0; i < pointsAlongPlane.Count; i++)
        {
            // �v�Z�ŋ��߂����S�_����A�e���_�ւ̕����x�N�g��
            displacement = pointsAlongPlane[i].Position - center;

            // �V�K��������|���S����UV���W�����߂�B
            // displacement�����S����̃x�N�g���̂��߁AUV�I�Ȓ��S�ł���0.5���x�[�X�ɁA���ς��g����UV�̍ŏI�I�Ȉʒu�𓾂�
            newUV1 = Vector3.zero;
            newUV1.x = 0.5f + Vector3.Dot(displacement, left);
            newUV1.y = 0.5f + Vector3.Dot(displacement, upward);
            newUV1.z = 0.5f + Vector3.Dot(displacement, cutNormal);

            // ���̒��_�B�������A�Ō�̒��_�̎��͍ŏ��̒��_�𗘗p���邽�߁A�኱�g���b�L�[�Ȏw����@�����Ă���i% vertices.Count�j
            displacement = pointsAlongPlane[(i + 1) % pointsAlongPlane.Count].Position - center;

            newUV2 = Vector3.zero;
            newUV2.x = 0.5f + Vector3.Dot(displacement, left);
            newUV2.y = 0.5f + Vector3.Dot(displacement, upward);
            newUV2.z = 0.5f + Vector3.Dot(displacement, cutNormal);

            // uvs.Add(new Vector2(relativePosition.x, relativePosition.y));
            // normals.Add(cutNormal);

            // �����̃|���S���Ƃ��āA���߂�UV�𗘗p���ăg���C�A���O����ǉ�
            positive.AddMeshSection(
                new Vector3[]{
                        pointsAlongPlane[i],
                        pointsAlongPlane[(i + 1) % pointsAlongPlane.Count],
                        center
                },
            new Vector3[]{
                -cutNormal,
                        -cutNormal,
                        -cutNormal
                },
                new Vector2[]{
                        newUV1,
                        newUV2,
                        new Vector2(0.5f, 0.5f)
                },
                -cutNormal,
                left_side.subIndices.Count - 1 // �J�b�g�ʁB�Ō�̃T�u���b�V���Ƃ��ăg���C�A���O����ǉ�
            );

            // �E���̃g���C�A���O���B��{�͍����Ɠ��������A�@�������t�����B
            negative.AddMeshSection(
                new Vector3[]{
                        pointsAlongPlane[i],
                        pointsAlongPlane[(i + 1) % pointsAlongPlane.Count],
                        center
                },
                new Vector3[]{
                        cutNormal,
                        cutNormal,
                        cutNormal
                },
                new Vector2[]{
                        newUV1,
                        newUV2,
                        new Vector2(0.5f, 0.5f)
                },
                cutNormal,
                right_side.subIndices.Count - 1 // �J�b�g�ʁB�Ō�̃T�u���b�V���Ƃ��ăg���C�A���O����ǉ�
            );
        }
    }
}

    #region
    private void OnDrawGizmos()
    {
        Gizmos.color = UnityEngine.Color.red;
        _planePosition = _planeTransform.position;
        _planeNormal = _planeTransform.up;
        _plane = new Plane(_planeNormal, _planePosition);

        // Plane�̖@���x�N�g������������`��
        Gizmos.DrawLine(_planeTransform.position, _planeTransform.position + _planeTransform.up * 5); // �@�������ɐ���`��

        // Plane�̈ʒu�����������ȋ���`��
        Gizmos.color = UnityEngine.Color.green;
        Gizmos.DrawSphere(_planeTransform.position, 0.1f); // Plane�̈ʒu�ɋ���`��

        Gizmos.matrix = Matrix4x4.TRS(_planePosition, Quaternion.LookRotation(_planeNormal), Vector3.one);
        Gizmos.color = new Color(0, 1, 0, 0.4f);
        Gizmos.DrawCube(Vector3.zero, new Vector3(2, 2, 0f)); // Matrix�Ɋ�Â��ʒu�ɕ`��
        Gizmos.color = new Color(0, 1, 0, 1f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(2, 2, 0f)); // Matrix�Ɋ�Â��ʒu�ɕ`��
        Gizmos.matrix = transform.localToWorldMatrix;
    }
    #endregion
}

public class MeshSlicer_InfinitePlane
{
    [SerializeField] private Vector3 _planePosition;
    [SerializeField] private Vector3 _planeNormal;
}

public class MeshSlicer_Plane
{
    [SerializeField] private Vector3 _planePosition;
    [SerializeField] private Vector3 _planeNormal;
    [SerializeField] private Vector2 _planeSize;


}

public class MeshSlicer_Mesh
{

}