using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;
using static MeshCut;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

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
        //_vertices = new List<Vector3>(filter.mesh.vertices);
        _vertices = new List<Vector3>(_meshFilter.sharedMesh.vertices);
        _normals = new List<Vector3>(_meshFilter.sharedMesh.normals);
        _uvs = new List<Vector2>(_meshFilter.sharedMesh.uv);
        _triangles = new List<int>(_meshFilter.sharedMesh.triangles);
    }

    public void AddTriangle(VertexData p1, VertexData p2, VertexData p3)
    {
        int index = _vertices.Count;
        _vertices.Add(p1.Position);
        _normals.Add(p1.Normal);
        _uvs.Add(p1.Uv);

        _vertices.Add(p2.Position);
        _normals.Add(p2.Normal);
        _uvs.Add(p2.Uv);

        _vertices.Add(p3.Position);
        _normals.Add(p3.Normal);
        _uvs.Add(p3.Uv);

        _triangles.Add(index);
        _triangles.Add(index + 1);
        _triangles.Add(index + 2);

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
    public VertexData ComputeIntersectionVertex(int index1, int index2)
    {
        VertexData vert = new VertexData();
        var pos1 = _vertices[index1];
        var uv1 = _uvs[index1];
        var nrm1 = _normals[index1];
        var pos2 = _vertices[index2];
        var uv2 = _uvs[index2];
        var nrm2 = _normals[index2];
        var normalizedDistance = ComputeIntersection(pos1, pos2);
        vert.Position = Vector3.Lerp(pos1, pos2, normalizedDistance);
        vert.Normal = Vector3.Lerp(nrm1, nrm2, normalizedDistance);
        vert.Uv = Vector3.Lerp(uv1, uv2, normalizedDistance);
        return vert;

    }

    // 面の表裏どちらに位置するか取得する
    bool ComputeSide(Vector3 p)
    {
        return _plane.GetSide(p);
    }

    public void Start()
    {
        GetMeshInfo(_subject);
        Cut(_subject);
    }

    public void Cut(GameObject subject)
    {
        GetMeshInfo(subject);
        _plane = new Plane(_planeTransform.up, _planeTransform.position);


        bool[] sides = new bool[3];
        for (int i = 0; i < _triangles.Count; i += 3)
        {
            int index1 = _triangles[i];
            int index2 = _triangles[i+1];
            int index3 = _triangles[i+2];
            var v1 = GetVertexData(index1);
            var v2 = GetVertexData(index2);
            var v3 = GetVertexData(index3);
            // それぞれの面との左右判定
            sides[0] = ComputeSide(v1.Position);
            sides[1] = ComputeSide(v2.Position);
            sides[2] = ComputeSide(v3.Position);

            bool isABSameSide = sides[0] == sides[1];
            bool isBCSameSide = sides[1] == sides[2];

            if (isABSameSide && isBCSameSide)
            {

            }
            else
            {
                UnityEngine.Debug.Log("AAA!!!!?!!?!?");
                // 交点の算出
                VertexData intersectionD;
                VertexData intersectionE;

                if (isABSameSide)
                {
                    intersectionD = ComputeIntersectionVertex(index1, index3);
                    intersectionE = ComputeIntersectionVertex(index2, index3);
                    AddMeshSection(v1, v2, intersectionE);
                    AddMeshSection(v1, intersectionE, intersectionD);
                    AddMeshSection(intersectionE, v3, intersectionD);
                }
                else if (isBCSameSide)
                {
                    intersectionD = ComputeIntersectionVertex(index2, index1);
                    intersectionE = ComputeIntersectionVertex(index3, index1);
                    AddMeshSection(v2, v3, intersectionE);
                    AddMeshSection(v2, intersectionE, intersectionD);
                    AddMeshSection(intersectionE, v1, intersectionD);
                }
                else
                {
                    intersectionD = ComputeIntersectionVertex(index1, index2);
                    intersectionE = ComputeIntersectionVertex(index3, index2);
                    AddMeshSection(v1, intersectionE, v3);
                    AddMeshSection(intersectionD, intersectionE, v1);
                    AddMeshSection(v2, intersectionE, intersectionD);
                }
            }
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
        _vertices.Add(vertex.Position);
        _uvs.Add(vertex.Uv);
        _normals.Add(vertex.Normal);
        return _vertices.Count - 1;
    }

    #region
    private void OnDrawGizmos()
    {
        Gizmos.color = UnityEngine.Color.red;
        float VertexWidth = 0.05f;

        var objectTransform = _subject.transform;
        {
            foreach (Vector3 vertex in _vertices)
            {
                Gizmos.DrawSphere(objectTransform.TransformPoint(vertex), VertexWidth);
            }
        }

        for (int i = 0; i < _triangles.Count; i += 3)
        {
            Gizmos.color = UnityEngine.Color.blue;

            Vector3 vertex1 = objectTransform.TransformPoint(_vertices[_triangles[i]]);
            Vector3 vertex2 = objectTransform.TransformPoint(_vertices[_triangles[i + 1]]);
            Vector3 vertex3 = objectTransform.TransformPoint(_vertices[_triangles[i + 2]]);

            Gizmos.DrawLine(vertex1, vertex2);
            Gizmos.DrawLine(vertex2, vertex3);
            Gizmos.DrawLine(vertex3, vertex1);

        }
        if (_planeTransform != null)
        {
            // Planeの法線ベクトルを示す線を描画
            Gizmos.color = UnityEngine.Color.red;
            Gizmos.DrawLine(_planeTransform.position, _planeTransform.position + _planeTransform.up * 5); // 法線方向に線を描画

            // Planeの位置を示す小さな球を描画
            Gizmos.color = UnityEngine.Color.green;
            Gizmos.DrawSphere(_planeTransform.position, 0.1f); // Planeの位置に球を描画
        }
        Gizmos.matrix = Matrix4x4.TRS(_planeTransform.position, _planeTransform.rotation, _planeTransform.localScale);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
    #endregion
}
