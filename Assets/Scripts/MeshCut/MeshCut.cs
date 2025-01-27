using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    // 板
    private static Vector3  planePosition;
    private static Vector3  planeNormal;
    
    // 無限遠平面
    private static Plane plane;

    // メッシュ
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

    // メッシュ情報取得
    public static void GetMeshInfo(GameObject subject)
    {
        meshFilter = subject.GetComponent<MeshFilter>();
        vertices = new List<Vector3>(meshFilter.sharedMesh.vertices);
        normals = new List<Vector3>(meshFilter.sharedMesh.normals);
        uvs = new List<Vector2>(meshFilter.sharedMesh.uv);
        triangles = new List<int>(meshFilter.sharedMesh.triangles);
        // 頂点の位置をワールド空間に変換
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

    // 交点への距離の算出
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

    // 交点に頂点を生成
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

    // 面の表裏どちらに位置するか取得する
    public static bool ComputeSide(Vector3 p)
    {
        return plane.GetSide(p);
    }
        
    public static (MeshConstructionHelper positive, MeshConstructionHelper negative) Cut
        (GameObject subject, Vector3 planeOrigin, Vector3 planeNormal)
    {
        GetMeshInfo(subject);
        plane = new Plane(planeNormal, planeOrigin); 
        // 切断面に沿った点のリスト
        List<VertexData> pointsAlongPlane = new List<VertexData>();
        MeshConstructionHelper positiveMesh = new MeshConstructionHelper();
        MeshConstructionHelper negativeMesh = new MeshConstructionHelper();

        bool[] sides = new bool[3];
        // ポリゴンは三つの頂点で構成されるので３づつ増分して走査
        for (int i = 0; i < triangles.Count; i += 3)
        {
            // 三角形が保有する頂点のインデックスを取得
            int indexA = triangles[i];
            int indexB = triangles[i+1];
            int indexC = triangles[i+2];

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

            // それを頂点数の合計で割り、中心とする
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


    // 切断面の構築
    private static void FillCutSurface(GameObject subject, ref MeshConstructionHelper mesh, Vector3 cutNormal, List<VertexData> pointsAlongPlane)
    {
        // 切断面の中心点を算出
        VertexData halfPoint = new VertexData()
        {
            Position = ComputeHalfPoint(pointsAlongPlane),
            Uv = new Vector3(0.5f, 0.5f)
        };

        // 算出した中心点＋２頂点でポリゴンを形成
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

        //// Planeの法線ベクトルを示す線を描画
        //Gizmos.DrawLine(_planeTransform.position, _planeTransform.position + _planeTransform.up * 5); // 法線方向に線を描画

        //// Planeの位置を示す小さな球を描画
        //Gizmos.color = UnityEngine.Color.green;
        //Gizmos.DrawSphere(_planeTransform.position, 0.1f); // Planeの位置に球を描画

        //Gizmos.matrix = Matrix4x4.TRS(_planePosition, Quaternion.LookRotation(_planeNormal), Vector3.one);
        //Gizmos.color = new Color(0, 1, 0, 0.4f);
        //Gizmos.DrawCube(Vector3.zero, new Vector3(2, 2, 0f)); // Matrixに基づく位置に描画
        //Gizmos.color = new Color(0, 1, 0, 1f);
        //Gizmos.DrawWireCube(Vector3.zero, new Vector3(2, 2, 0f)); // Matrixに基づく位置に描画
        //Gizmos.matrix = transform.localToWorldMatrix;
    }
    #endregion
}