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

    // メッシュ情報取得
    public static void GetMeshInfo(GameObject subject)
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

    public static VertexData GetMyVertexData(int index)
    {
        VertexData vert = new VertexData();
        if(_vertices.Count <= index) { Debug.LogError("index(" + index + ")がvertices(" + _vertices.Count + ")の範囲外なため無効な値が参照されます"); }
        if(_uvs.Count <= index) { Debug.LogError("index(" + index + ")がuvs(" + _uvs.Count + ")の範囲外なため無効な値が参照されます"); }
        if(_normals.Count <= index) { Debug.LogError("index(" + index + ")がnormals(" + _normals.Count + ")の範囲外なため無効な値が参照されます"); }
        vert.Position = _vertices[index];
        vert.Uv = _uvs[index];
        vert.Normal = _normals[index];
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
        for (int i = 0; i < _triangles.Count; i += 3)
        {
            // 三角形が保有する頂点のインデックスを取得
            int indexA = _triangles[i];
            int indexB = _triangles[i+1];
            int indexC = _triangles[i+2];

            // 頂点データ取得
            var vA = GetMyVertexData(indexA);
            var vB = GetMyVertexData(indexB);
            var vC = GetMyVertexData(indexC);

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

    public static List<MeshConstructionHelper> CutDivide
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
        for (int i = 0; i < _triangles.Count; i += 3)
        {
            // 三角形が保有する頂点のインデックスを取得
            int indexA = _triangles[i];
            int indexB = _triangles[i+1];
            int indexC = _triangles[i+2];

            // 頂点データ取得
            var vA = GetMyVertexData(indexA);
            var vB = GetMyVertexData(indexB);
            var vC = GetMyVertexData(indexC);

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
        Debug.Log("メッシュ分割開始");
        var positiveResult = MeshDivision(positiveMesh, pointsAlongPlane);
        Debug.Log("左側成功");
        var negativeResult = MeshDivision(negativeMesh, pointsAlongPlane);
        Debug.Log("右側成功");

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

    #region
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
    #endregion

    public static (List<MeshConstructionHelper> meshList, List<List<VertexData>> pointsAlongPlaneList)
        MeshDivision(MeshConstructionHelper mesh, List<VertexData> pointsAlongPlane)
    {
        // メッシュグループ格納（マージされるとメッシュグループは重複する）
        List<MeshConstructionHelper> meshGroup = new List<MeshConstructionHelper>();
        // 頂点の結合情報を格納　比較は頂点座標のみで比較
        Dictionary<Vector3, int> verticesDic = new Dictionary<Vector3, int>();

        int[] index;               // 頂点を指すインデックス
        Vector3[] v;               // インデックスが指す頂点の3D座標
        int[] containIndex;        // 頂点の割り振られているメッシュグループのインデックス
        bool[] hasContain;         // メッシュグループに割り振られているかのフラグ

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
            Debug.Log("マージを実行");

            // マージ元のメッシュグループインデックス取得
            if (!hasContain[index1])
            {
                Debug.LogError("マージ元インデックスがメッシュグループを指していません");
                return false;
            }
            int receiverIndex = verticesDic[v[index1]];

            // マージ対象のメッシュグループインデックス取得
            if (!hasContain[index2])
            {
                verticesDic.Add(v[index2], receiverIndex);
                Debug.Log("マージ対象インデックスがメッシュグループを指していなかったので更新して終了します");
                return false;
            }
            int targetIndex = verticesDic[v[index2]];

            // すでに同じグループなら何もしない
            if (meshGroup[receiverIndex] == meshGroup[targetIndex])
            {
                Debug.Log("同じメッシュグループをマージしようとしました(" + receiverIndex + ", " + targetIndex + ")");
                return false;
            }

            Debug.LogWarning("正常にマージを開始します");

            counttmp++;
            // メッシュを結合
            meshGroup[receiverIndex].MergeHelper(meshGroup[targetIndex]);
            #region
            /*
                * 例：meshGroup[0]と[1]をマージしたら、
                * その二つは同じﾒｯｼｭｸﾞﾙｰﾌﾟを指すようになる。
                * この時点では問題はない。そのあと
                * [1]と[2]をマージした場合、[1]と[2]は同じ
                * ﾒｯｼｭｸﾞﾙｰﾌﾟを指すようになるが、もともと
                * [0]と[1]は同じﾒｯｼｭｸﾞﾙｰﾌﾟを指していたのに、
                * [1]だけ[2]の新しいﾒｯｼｭｸﾞﾙｰﾌﾟを指すようになったので、
                * [0]と[1]は異なるインスタンスを指すことになる。
                * 下の処理は[0]と[1]と[2]を同じﾒｯｼｭｸﾞﾙｰﾌﾟを
                * 指すようにする処理。
                */
            #endregion
            // 指すメッシュグループ更新
            for (int i = 0; i < meshGroup.Count; i++)
            {
                Debug.Log("マージループ " + i + "/" + meshGroup.Count + "回目");
                // マージに使ったインデックスと異なる
                if (targetIndex != i)
                {
                    // マージに使ったメッシュグループと同一である
                    if (meshGroup[i] == meshGroup[targetIndex])
                    {
                        Debug.Log("マージしました(" + receiverIndex + ", " + i + ")");
                        // マージ元のメッシュグループを指すように更新
                        meshGroup[i] = meshGroup[receiverIndex];
                    }
                }
            }
            // 参照型っぽいからforあとで更新
            meshGroup[targetIndex] = meshGroup[receiverIndex];
            Debug.Log("マージしました(" + receiverIndex + ", " + targetIndex + ")");
            // キーをリストに変換して回す
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
        //    // メッシュグループのインデックス
        //    int meshGroupIndex = containIndex[index2];
        //    verticesDic.Add(v[index1], meshGroupIndex);
        //    return true;
        //}
        #endregion

        Debug.Log("ポリゴン数 : " + mesh.triangles.Count / 3);
        Debug.Log("頂点数 : " + mesh.vertices.Count);
        // ポリゴンごとに走査
        for (int i = 0; i < mesh.triangles.Count; i += 3)
        {
            // インデックス
            index = new int[3];
            index[0] = mesh.triangles[i];
            index[1] = mesh.triangles[i + 1];
            index[2] = mesh.triangles[i + 2];
            // 頂点座標
            v = new Vector3[3];
            v[0] = mesh.vertices[index[0]];
            v[1] = mesh.vertices[index[1]];
            v[2] = mesh.vertices[index[2]];
            // メッシュグループインデックス
            containIndex = new int[3];
            containIndex[0] = 0;
            containIndex[1] = 0;
            containIndex[2] = 0;
            // メッシュグループに所属しているか
            hasContain = new bool[3];
            hasContain[0] = verticesDic.TryGetValue(v[0], out containIndex[0]);
            hasContain[1] = verticesDic.TryGetValue(v[1], out containIndex[1]);
            hasContain[2] = verticesDic.TryGetValue(v[2], out containIndex[2]);

            //int containNum = 0;

            //// メッシュグループに割り振り済みならカウンタをインクリメント
            //// 割り振られていなかったら-1にセット
            //// 初期状態で返ってくる０がﾒｯｼｭｸﾞﾙｰﾌﾟｲﾝﾃﾞｯｸｽ０と重複するため
            //if (!hasContain[0]) { containIndex[0] = -1; } else { containNum++; }
            //if (!hasContain[1]) { containIndex[1] = -1; } else { containNum++; }
            //if (!hasContain[2]) { containIndex[2] = -1; } else { containNum++; }

            // 割り振られていなかったら-1にセット
            // 初期状態で返ってくる０がﾒｯｼｭｸﾞﾙｰﾌﾟｲﾝﾃﾞｯｸｽ０と重複するため
            if (!hasContain[0]) { containIndex[0] = -1; }
            if (!hasContain[1]) { containIndex[1] = -1; }
            if (!hasContain[2]) { containIndex[2] = -1; }

            Debug.Log("ポリゴン走査 " + ((i / 3) + 1) + " 回目" + " (" + containIndex[0] + ", " + containIndex[1] + ", " + containIndex[2] + ")");

            // メッシュグループが割り振られているインデックスを見つけて処理
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
            // どの頂点もメッシュグループが割り振られていない
            else
            {
                // 新しいメッシュグループを作成
                MeshConstructionHelper helper = new MeshConstructionHelper();
                helper.AddMeshSection(mesh.GetVertexData(index[0]), mesh.GetVertexData(index[1]), mesh.GetVertexData(index[2]));
                meshGroup.Add(helper);
                int newGroupIndex = meshGroup.Count - 1;
                // 頂点に対応インデックスを割り振る
                verticesDic.Add(v[0], newGroupIndex);
                verticesDic.Add(v[1], newGroupIndex);
                verticesDic.Add(v[2], newGroupIndex);
            }

            #region
            //string Log = "";

            //switch (containNum)
            //{
            //    case 3:     // すべての頂点がメッシュグループに割り振り済み
            //        {
            //            #region
            //            Log += " すべての頂点がメッシュグループに割り振り済み" + "( " + containIndex[0] + ", " + containIndex[1] + ", " + containIndex[2] + " )";

            //            // すべての頂点が同じメッシュグループ
            //            if (containIndex[0] == containIndex[1] && containIndex[1] == containIndex[2])
            //            {
            //                Debug.Log(Log + " すべての頂点が同じメッシュグループ");
            //            }
            //            // すべての頂点がそれぞれ別のメッシュを指している
            //            else if
            //                (containIndex[0] != containIndex[1]
            //                &&
            //                containIndex[1] != containIndex[2]
            //                &&
            //                containIndex[0] != containIndex[2])
            //            {
            //                Debug.Log(Log + " すべての頂点がそれぞれ異なるメッシュグループ" + "( " + containIndex[0] + ", " + containIndex[1] + ", " + containIndex[2] + " )");
            //            }
            //            // ２つの頂点が同じメッシュグループを指している
            //            else
            //            {
            //                // 頂点１と頂点２が同じメッシュグループ
            //                if (containIndex[0] == containIndex[1])
            //                {
            //                    Debug.Log(Log + " 頂点１と頂点２が同じメッシュグループ");
            //                }
            //                // 頂点２と頂点３が同じメッシュグループ
            //                else if (containIndex[1] == containIndex[2])
            //                {
            //                    Debug.Log(Log + " 頂点２と頂点３が同じメッシュグループ");
            //                }
            //                // 頂点１と頂点３が同じメッシュグループ
            //                else
            //                {
            //                    Debug.Log(Log + " 頂点１と頂点３が同じメッシュグループ");
            //                }
            //            }
            //            #endregion
            //            // 冗長にしなくてもこれだけで動く？
            //            MergeHelper(0, 1);
            //            MergeHelper(1, 2);
            //            AddMeshSection(0);
            //        }
            //        break;
            //    case 2:     // いずれか２つの頂点がメッシュグループに割り振り済み
            //        {
            //            Log +=" いずれか２つの頂点がメッシュグループに割り振り済み" + "( " + containIndex[0] + ", " + containIndex[1] + ", " + containIndex[2] + " )";

            //            // 二つの割り振り済み頂点を探索

            //            // 頂点１と頂点２が割り振り済み
            //            if (hasContain[0] && hasContain[1])
            //            {
            //                // 指すメッシュグループが同一なら単純にポリゴン追加
            //                if (containIndex[0] == containIndex[1])
            //                {
            //                    Log += " 頂点１と頂点２が同一のメッシュグループを指している";
            //                    Debug.Log(Log + " 単純に追加");
            //                    // 未割り振りの頂点にメッシュ割り振り
            //                    Add(2, 0);
            //                    // ポリゴン追加
            //                    AddMeshSection(0);
            //                }
            //                // 同一でなければ片方のメッシュグループと結合してポリゴン追加
            //                else
            //                {
            //                    Log += " 頂点１と頂点２が別々のメッシュグループを指している";
            //                    Debug.Log(Log + " 結合して追加");
            //                    // 未割り振りの頂点にメッシュ割り振り
            //                    Add(2, 0);
            //                    // メッシュを結合
            //                    MergeHelper(0, 1);
            //                    // ポリゴン追加
            //                    AddMeshSection(0);
            //                }
            //            }
            //            // 頂点１と頂点２が割り振り済み
            //            else if (hasContain[0] && hasContain[2])
            //            {
            //                if (containIndex[0] == containIndex[2])
            //                {
            //                    Log += " 頂点１と頂点３が同一のメッシュグループを指している";
            //                    Debug.Log(Log + "単純に追加");
            //                    Add(1, 0);
            //                    AddMeshSection(0);
            //                }
            //                else
            //                {
            //                    Log += " 頂点１と頂点３が別々のメッシュグループを指している";
            //                    Debug.Log(Log + " 結合して追加");
            //                    Add(1, 0);
            //                    MergeHelper(0, 2);
            //                    AddMeshSection(0);
            //                }
            //            }
            //            // 頂点２と頂点３が割り振り済み
            //            else
            //            {
            //                if (containIndex[1] == containIndex[2])
            //                {
            //                    Log += " 頂点２と頂点３が同一のメッシュグループを指している";
            //                    Debug.Log(Log + " 単純に追加");
            //                    Add(0, 1);
            //                    AddMeshSection(1);
            //                }
            //                else
            //                {
            //                    Log += " 頂点２と頂点３が別々のメッシュグループを指している";
            //                    Debug.Log(Log + " 結合して追加");
            //                    Add(0, 1);
            //                    MergeHelper(1, 2);
            //                    AddMeshSection(1);
            //                }
            //            }
            //        }
            //        break;
            //    case 1:     // いずれか１つの頂点がメッシュグループに割り振り済み
            //        {
            //            Log += " いずれか１つの頂点がメッシュグループに割り振り済み" + "( " + containIndex[0] + ", " + containIndex[1] + ", " + containIndex[2] + " )";

            //            if (hasContain[0])
            //            {
            //                Log += " 頂点１がメッシュグループに割り振られている";
            //                Add(1, 0);
            //                Add(2, 0);
            //                AddMeshSection(0);
            //                Debug.Log(Log + " 単純に追加");
            //            }
            //            else if (hasContain[1])
            //            {
            //                Log += " 頂点２がメッシュグループに割り振られている";
            //                Add(0, 1);
            //                Add(2, 1);
            //                AddMeshSection(1);
            //                Debug.Log(Log + "単純に追加");
            //            }
            //            else if (hasContain[2])
            //            {
            //                Log += " 頂点３がメッシュグループに割り振られている";
            //                Add(0, 2);
            //                Add(1, 2);
            //                AddMeshSection(2);
            //                Debug.Log(Log + " 単純に追加");
            //            }
            //        }
            //        break;
            //    case 0:     // どの頂点もメッシュグループに割り振られていない
            //        {
            //            Log += " どの頂点もメッシュグループに割り振られていない" + "( " + containIndex[0] + ", " + containIndex[1] + ", " + containIndex[2] + " )";
            //            // 新しいメッシュグループを作成
            //            MeshConstructionHelper helper = new MeshConstructionHelper();
            //            helper.AddMeshSection(mesh.GetVertexData(index[0]), mesh.GetVertexData(index[1]), mesh.GetVertexData(index[2]));
            //            meshGroup.Add(helper);
            //            int newGroupIndex = meshGroup.Count - 1;
            //            // 頂点に対応インデックスを割り振る
            //            verticesDic.Add(v[0], newGroupIndex);
            //            verticesDic.Add(v[1], newGroupIndex);
            //            verticesDic.Add(v[2], newGroupIndex);
            //            Debug.Log(Log + " メッシュグループ追加");
            //        }
            //        break;
            //}
            #endregion

            if (!verticesDic.ContainsKey(v[0]))
            {
                Debug.LogWarning("頂点１が追加されずに割り振りが終了しました");
            }
            if (!verticesDic.ContainsKey(v[1]))
            {
                Debug.LogWarning("頂点２が追加されずに割り振りが終了しました");
            }
            if (!verticesDic.ContainsKey(v[2]))
            {
                Debug.LogWarning("頂点３が追加されずに割り振りが終了しました");
            }
        }

        List<MeshConstructionHelper> resultMeshList = new List<MeshConstructionHelper>();
        List<MeshConstructionHelper> closedList = new List<MeshConstructionHelper>();

        // メッシュグループの追加
        foreach(var obj in meshGroup)
        {
            // まだ追加してないメッシュグループ
            if (!closedList.Contains(obj))
            {
                closedList.Add(obj);
                resultMeshList.Add(obj);
                continue;
            }
            counttmp++;
        }
        Debug.Log("メッシュグループの数 : " + resultMeshList.Count);
        Debug.Log("結合されたメッシュグループの数 : " + (meshGroup.Count - resultMeshList.Count));

        // 切断面に接した頂点のメッシュグループ配分
        List<List<VertexData>> pointsAlongPlaneList = new List<List<VertexData>>(resultMeshList.Count);
        for (int i = 0; i < resultMeshList.Count; i++)
        {
            pointsAlongPlaneList.Add(new List<VertexData>());
        }
        Debug.Log(pointsAlongPlaneList.Count);
        // 走査対象の頂点と一致する、割り振られた頂点の指すメッシュグループを登録
        foreach (var obj in pointsAlongPlane)
        {
            // meshGroup[meshGroupIndex]が指すインスタンスがresultMeshListのどこにあるかを見つけ、そのインデックスをpointsAlongPlaneListに渡す
            int meshGroupIndex;
            if(verticesDic.TryGetValue(obj.Position, out meshGroupIndex))
            {
                // 指すメッシュグループを取得
                var targetMeshGroup = meshGroup[meshGroupIndex];
                // メッシュグループがあるインデックスを取得
                int resultMeshIndex = resultMeshList.IndexOf(targetMeshGroup);
                if (resultMeshIndex >= 0)
                {
                    pointsAlongPlaneList[resultMeshIndex].Add(obj);
                }
                else
                {
                    Debug.LogError("指定されたメッシュグループが存在しません");
                }
            }
            else
            {
                Debug.LogError("頂点がどのメッシュグループにも存在しません");
                if(mesh.vertices.Contains(obj.Position))
                {
                    Debug.LogError("頂点は処理前のメッシュに存在していました");
                }
            }
        }
        Debug.Log("終了しました");
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