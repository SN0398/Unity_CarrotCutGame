using JetBrains.Annotations;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml.Schema;
using UnityEngine;
using static MeshCut;

// Refference:
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

    public static List<MeshConstructionHelper> CutDevide
        (GameObject subject, Vector3 planeOrigin, Vector3 planeNormal)
    {
        // 初期化
        GetMeshInfo(subject);   // 初期メッシュ情報取得
        plane = new Plane(planeNormal, planeOrigin);    // 切断面定義

        // 切断面に沿った点のリスト
        List<VertexData> pointsAlongPlane = new List<VertexData>();

        // 左側のメッシュ、右側のメッシュ
        MeshConstructionHelper positiveMesh = new MeshConstructionHelper();
        MeshConstructionHelper negativeMesh = new MeshConstructionHelper();

        // ポリゴンで繋がっている部分を同一メッシュとしてグルーピングする
        List<MeshConstructionHelper> positiveMeshGroup = new List<MeshConstructionHelper>();
        List<MeshConstructionHelper> negativeMeshGroup = new List<MeshConstructionHelper>();

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
        var positiveResult = MeshDivision(positiveMesh, pointsAlongPlane);
        var negativeResult = MeshDivision(negativeMesh, pointsAlongPlane);

        positiveMeshGroup = positiveResult.meshList;
        negativeMeshGroup = negativeResult.meshList;

        // 切断面の構築
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

        // ポリゴンごとに走査
        for (int i = 0; i < mesh.triangles.Count; i += 3)
        {
            // インデックス
            int[] index = new int[3];
            index[0] = mesh.triangles[i];
            index[1] = mesh.triangles[i + 1];
            index[2] = mesh.triangles[i + 2];
            // 頂点座標
            Vector3[] v = new Vector3[3];
            v[0] = mesh.vertices[index[0]];
            v[1] = mesh.vertices[index[1]];
            v[2] = mesh.vertices[index[2]];
            // メッシュグループインデックス
            int[] containIndex = new int[3];
            containIndex[0] = 0;
            containIndex[1] = 0;
            containIndex[2] = 0;
            // メッシュグループに所属しているか
            bool[] hasContain = new bool[3];
            hasContain[0] = verticesDic.TryGetValue(v[0], out containIndex[0]);
            hasContain[1] = verticesDic.TryGetValue(v[1], out containIndex[1]);
            hasContain[2] = verticesDic.TryGetValue(v[2], out containIndex[2]);

            /*
            ３頂点に対して走査を行うセクションでは、次のケースが考えられる。
            ーーーーーーーーーーーーーーーーーーーーーーーーーーーーーーーーーー
            １：すべての頂点が同じメッシュグループ、もしくはそれぞれ別々のメッシュグループに割り振られている。
            ２：頂点のうち１つがメッシュグループに所属していて、それ以外はメッシュグループに割り振られていない。
            ３：頂点のうち２つが同一または別々のメッシュグループに所属していて、もう一つはメッシュグループに割り振られていない。
            ４：すべての頂点がメッシュグループに所属していない。
            ーーーーーーーーーーーーーーーーーーーーーーーーーーーーーーーーーー
            それに対して、それぞれ以下の対応を行う。
            ーーーーーーーーーーーーーーーーーーーーーーーーーーーーーーーーーー
            １：それぞれのメッシュグループを結合して、頂点のメッシュグループインデックスを更新。
            ２：メッシュグループに所属していない２つの頂点を、メッシュグループに所属している一つの頂点と同じメッシュグループインデックスを割り振る。
            ３：２つの頂点の指すメッシュグループが同一である場合はそのまま、同一出ない場合は2つのメッシュグループを結合し、そのメッシュグループインデックスを３つの頂点に割り振る。
            ４：新しいメッシュグループを作成して、そのインデックスを３つの頂点に割り振る。
            ーーーーーーーーーーーーーーーーーーーーーーーーーーーーーーーーーー
             */
            // すべての頂点がメッシュグループに割り振り済み
            if (hasContain[0] && hasContain[1] && hasContain[2])
            {
                Debug.Log("呼ばた");

            }
            // いずれかの頂点がメッシュグループに割り振り済み
            else if (hasContain[0] || hasContain[1] || hasContain[2])
            {
                Debug.Log("呼ばた1");
                // 二つの割り振り済み頂点を探索
                if (hasContain[0] && hasContain[1])
                {
                    // 指すメッシュが同一なら単純にポリゴン追加
                    if (containIndex[0] == containIndex[1])
                    {
                        // 未割り振りの頂点にメッシュ割り振り
                        verticesDic.Add(v[2], containIndex[0]);
                        // ポリゴン追加
                        meshGroup[containIndex[0]].AddMeshSection(GetVertexData(index[0]), GetVertexData(index[1]), GetVertexData(index[2]));
                    }
                    else
                    {
                        // 未割り振りの頂点にメッシュ割り振り
                        verticesDic.Add(v[2], containIndex[0]);
                        // メッシュを結合
                        meshGroup[containIndex[0]].MergeHelper(meshGroup[containIndex[1]]);
                        // 指すメッシュを同一にする
                        verticesDic[v[1]] = containIndex[0];
                        // ポリゴン追加
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
            // どの頂点もメッシュグループに割り振られていない
            else
            {
                Debug.Log("呼ばた2");
                // 新しいメッシュグループを作成
                MeshConstructionHelper helper = new MeshConstructionHelper();
                helper.AddMeshSection(GetVertexData(index[0]), GetVertexData(index[1]), GetVertexData(index[2]));
                meshGroup.Add(helper);
                int newGroupIndex = meshGroup.Count - 1; ;
                // 頂点に対応インデックスを割り振る
                verticesDic.Add(v[0], newGroupIndex);
                verticesDic.Add(v[1], newGroupIndex);
                verticesDic.Add(v[2], newGroupIndex);
            }
        }
        Debug.Log("呼ばた3");
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
            Normal = cutNormal,
            Uv = new Vector3(0.5f, 0.5f)
        };

        // 算出した中心点＋２頂点でポリゴンを形成
        for (int i = 0; i < pointsAlongPlane.Count; i += 2)
        {
            VertexData v1 = pointsAlongPlane[i];
            VertexData v2 = pointsAlongPlane[(i + 1) % pointsAlongPlane.Count];
            VertexData v3 = halfPoint;

            Vector3 normal = subject.transform.InverseTransformDirection(ComputeNormal(v3, v2, v1));
            Vector3 localCutNormal = subject.transform.InverseTransformDirection(cutNormal); // cutNormal もローカルに変換
            float dot = Vector3.Dot(normal, localCutNormal);
            // 頂点の順番を反時計回りになるようにポリゴン追加
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