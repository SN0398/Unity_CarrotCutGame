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
        // 頂点重複回避
        // 既に存在する頂点ならインデックスをそのまま返す
        if (_vertexDictionary.TryGetValue(vertex, out int index))
        {
			return index;
        }
        // 頂点を追加してインデックスを返す
        _vertices.Add(vertex.Position);
        _uvs.Add(vertex.Uv);
        _normals.Add(vertex.Normal);
        int newIndex = _vertices.Count - 1;
        _vertexDictionary.Add(vertex, newIndex);
        return newIndex;
    }

    /// <summary>
    /// ポリゴンの分割関数　
    /// メッシュの繋がっていない部分を別々のメッシュに構成して返す
    /// </summary>
    /// <param name="mesh">対象メッシュ</param>
    /// <returns>分割されたメッシュリスト</returns>
    public static List<MeshConstructionHelper> MeshDivision(MeshConstructionHelper mesh)
    {
        List<MeshConstructionHelper> meshGroup = new List<MeshConstructionHelper>();
        List<int> meshGroupIndex = new List<int>(mesh._triangles.Count / 3);
        Dictionary<Vector3, int> verticesDic = new Dictionary<Vector3, int>();
        
        // ポリゴンは三つの頂点で構成されるので３づつ増分して走査
        for (int i = 0; i < mesh._triangles.Count; i += 3)
        {
            // インデックス
            int indexA = mesh._triangles[i];
            int indexB = mesh._triangles[i + 1];
            int indexC = mesh._triangles[i + 2];
            // 頂点座標
            var v1 = mesh._vertices[indexA];
            var v2 = mesh._vertices[indexB];
            var v3 = mesh._vertices[indexC];
            // メッシュグループインデックス
            var containIndex1 = 0;
            var containIndex2 = 0;
            var containIndex3 = 0;
            // メッシュグループに所属しているか
            var hasContain1 = verticesDic.TryGetValue(v1, out containIndex1);
            var hasContain2 = verticesDic.TryGetValue(v2, out containIndex2);
            var hasContain3 = verticesDic.TryGetValue(v3, out containIndex3);

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
            if (hasContain1 && hasContain2 && hasContain3)
            {
                // すべての頂点が異なるメッシュグループの場合、統合する
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
                // 新しいメッシュグループを作成
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

// 切断面構築用クラス
public class SlicedMeshConstructionHelper
{
    MeshConstructionHelper helper {  get; set; }
    List<int> pointsAlongPlane {  get; set; }

    public SlicedMeshConstructionHelper()
    {
        pointsAlongPlane = new List<int>();

    }
}