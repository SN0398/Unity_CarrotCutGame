using System.Collections.Generic;
using UnityEngine;
using static MeshCut;

// unityメッシュ
// https://qiita.com/keito_takaishi/items/8e56d5117ee90502e864
// メッシュカットサンプル
// https://qiita.com/edo_m18/items/31961cd19fd19e09b675
// https://medium.com/@hesmeron/mesh-slicing-in-unity-740b21ffdf84
// メッシュカット高速化
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

    // 板
    [SerializeField] private Transform _planeTransform;
    [SerializeField] private Vector3 _planePosition;
    [SerializeField] private Vector3 _planeNormal;
    // 無限遠平面
    private Plane _plane;

    // メッシュ
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

    // メッシュ情報取得
    public void GetMeshInfo(GameObject subject)
    {
        _meshFilter = subject.GetComponent<MeshFilter>();
        _vertices = new List<Vector3>(_meshFilter.sharedMesh.vertices);
        _normals = new List<Vector3>(_meshFilter.sharedMesh.normals);
        _uvs = new List<Vector2>(_meshFilter.sharedMesh.uv);
        _triangles = new List<int>(_meshFilter.sharedMesh.triangles);
        // 頂点の位置をワールド空間に変換
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

    // 交点への距離の算出
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

    // 交点に頂点を生成
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

    // 面の表裏どちらに位置するか取得する
    bool ComputeSide(Vector3 p)
    {
        return _plane.GetSide(p);
    }

    public void Start()
    {
        MeshConstructionHelper.ClearMesh();
        GetMeshInfo(_subject);
        Cut(_subject);

        // 元のオブジェクトを複製
        //GameObject positiveObject = Instantiate(_subject);
        GameObject negativeObject = Instantiate(_subject);

        // 複製したオブジェクトに新しいメッシュを適用
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
        // 切断面に沿った点のリスト
        List<VertexData> pointsAlongPlane = new List<VertexData>();

        bool[] sides = new bool[3];
        // ポリゴンは三つの頂点で構成されるので３づつ増分して走査
        for (int i = 0; i < _triangles.Count; i += 3)
        {
            // 三角形が保有する頂点のインデックスを取得
            int indexA = _triangles[i];
            int indexB = _triangles[i+1];
            int indexC = _triangles[i+2];

            // 頂点データ取得
            var vA = GetVertexData(indexA);
            var vB = GetVertexData(indexB);
            var vC = GetVertexData(indexC);

            // 頂点それぞれの面との左右判定
            sides[0] = ComputeSide(vA.Position);
            sides[1] = ComputeSide(vB.Position);
            sides[2] = ComputeSide(vC.Position);

            bool isABSameSide = sides[0] == sides[1];
            bool isBCSameSide = sides[1] == sides[2];

            // 全ての頂点が片側に寄っていればそのままどちらかのメッシュにポリゴンとして登録
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
                // Cが片側にある
                if (isABSameSide)
                {
                    // 交点の算出
                    intersectionD = ComputeIntersectionVertex(indexA, indexC);  // AとC
                    intersectionE = ComputeIntersectionVertex(indexB, indexC);  // BとC
                    // 交点から新しく生成される二つの頂点で新しくポリゴンを形成する
                    // ポリゴンはAB側は二つ、C側は一つ
                    // ポリゴンに登録する頂点は反時計回りに順番
                    helperA.AddMeshSection(vA, vB, intersectionE);              // A -> B -> E
                    helperA.AddMeshSection(vA, intersectionE, intersectionD);   // A -> E -> D
                    helperC.AddMeshSection(intersectionE, vC, intersectionD);   // E -> C -> D
                }
                // Aが片側にある
                else if (isBCSameSide)
                {
                    intersectionD = ComputeIntersectionVertex(indexB, indexA);
                    intersectionE = ComputeIntersectionVertex(indexC, indexA);
                    helperB.AddMeshSection(vB, vC, intersectionE);
                    helperB.AddMeshSection(vB, intersectionE, intersectionD);
                    helperA.AddMeshSection(intersectionE, vA, intersectionD);
                }
                // Bが片側にある
                else
                {
                    intersectionD = ComputeIntersectionVertex(indexA, indexB);
                    intersectionE = ComputeIntersectionVertex(indexC, indexB);
                    helperA.AddMeshSection(vA, intersectionE, vC);
                    helperA.AddMeshSection(intersectionD, intersectionE, vA);
                    helperB.AddMeshSection(vB, intersectionE, intersectionD);
                }
                // 切断面を構成する頂点をリストに追加
                pointsAlongPlane.Add(intersectionD);
                pointsAlongPlane.Add(intersectionE);
            }
        }
        // 切断面の構築
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

            // それを頂点数の合計で割り、中心とする
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


    // 切断面の構築
    private void FillCutSurface(ref MeshConstructionHelper positive, ref MeshConstructionHelper negative, Vector3 cutNormal, List<VertexData> pointsAlongPlane)
    {
        //// 切断面の中心点を算出
        //VertexData halfPoint = new VertexData()
        //{
        //    Position = ComputeHalfPoint(pointsAlongPlane),
        //    Uv = new Vector3(0.5f, 0.5f)
        //};

        //// 算出した中心点＋２頂点でポリゴンを形成
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
        // カット平面の中心点を計算する
        Vector3 center = Vector3.zero;

        // 引数で渡された頂点位置をすべて合計する
        foreach (Vector3 point in pointsAlongPlane)
        {
            center += point;
        }

        // それを頂点数の合計で割り、中心とする
        center = center / pointsAlongPlane.Count;

        // you need an axis based on the cap
        // カット平面をベースにしたupward
        Vector3 upward = Vector3.zero;

        // 90 degree turn
        // カット平面の法線を利用して、「上」方向を求める
        // 具体的には、平面の左側を上として利用する
        upward.x = cutNormal.y;
        upward.y = -cutNormal.x;
        upward.z = cutNormal.z;

        // 法線と「上方向」から、横軸を算出
        Vector3 left = Vector3.Cross(cutNormal, upward);

        Vector3 displacement = Vector3.zero;
        Vector3 newUV1 = Vector3.zero;
        Vector3 newUV2 = Vector3.zero;

        // 引数で与えられた頂点分ループを回す
        for (int i = 0; i < pointsAlongPlane.Count; i++)
        {
            // 計算で求めた中心点から、各頂点への方向ベクトル
            displacement = pointsAlongPlane[i].Position - center;

            // 新規生成するポリゴンのUV座標を求める。
            // displacementが中心からのベクトルのため、UV的な中心である0.5をベースに、内積を使ってUVの最終的な位置を得る
            newUV1 = Vector3.zero;
            newUV1.x = 0.5f + Vector3.Dot(displacement, left);
            newUV1.y = 0.5f + Vector3.Dot(displacement, upward);
            newUV1.z = 0.5f + Vector3.Dot(displacement, cutNormal);

            // 次の頂点。ただし、最後の頂点の次は最初の頂点を利用するため、若干トリッキーな指定方法をしている（% vertices.Count）
            displacement = pointsAlongPlane[(i + 1) % pointsAlongPlane.Count].Position - center;

            newUV2 = Vector3.zero;
            newUV2.x = 0.5f + Vector3.Dot(displacement, left);
            newUV2.y = 0.5f + Vector3.Dot(displacement, upward);
            newUV2.z = 0.5f + Vector3.Dot(displacement, cutNormal);

            // uvs.Add(new Vector2(relativePosition.x, relativePosition.y));
            // normals.Add(cutNormal);

            // 左側のポリゴンとして、求めたUVを利用してトライアングルを追加
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
                left_side.subIndices.Count - 1 // カット面。最後のサブメッシュとしてトライアングルを追加
            );

            // 右側のトライアングル。基本は左側と同じだが、法線だけ逆向き。
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
                right_side.subIndices.Count - 1 // カット面。最後のサブメッシュとしてトライアングルを追加
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

        // Planeの法線ベクトルを示す線を描画
        Gizmos.DrawLine(_planeTransform.position, _planeTransform.position + _planeTransform.up * 5); // 法線方向に線を描画

        // Planeの位置を示す小さな球を描画
        Gizmos.color = UnityEngine.Color.green;
        Gizmos.DrawSphere(_planeTransform.position, 0.1f); // Planeの位置に球を描画

        Gizmos.matrix = Matrix4x4.TRS(_planePosition, Quaternion.LookRotation(_planeNormal), Vector3.one);
        Gizmos.color = new Color(0, 1, 0, 0.4f);
        Gizmos.DrawCube(Vector3.zero, new Vector3(2, 2, 0f)); // Matrixに基づく位置に描画
        Gizmos.color = new Color(0, 1, 0, 1f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(2, 2, 0f)); // Matrixに基づく位置に描画
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