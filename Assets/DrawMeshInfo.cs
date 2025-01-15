using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
public class DrawMeshInfo : MonoBehaviour
{
    public float VertexWidth = 0.05f;
    // OnDrawGizmos() メソッドを使用して、頂点を描画
    private void OnDrawGizmos()
    {
        // メッシュフィルターを取得
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        // メッシュの頂点群を取得
        List<Vector3> vertices = new List<Vector3>(meshFilter.sharedMesh.vertices);
        int[] triangles = meshFilter.sharedMesh.triangles;

        // ワールド座標に変換
        Transform objectTransform = transform;
        Gizmos.color = Color.red;

        // 頂点
        {
            // 頂点を描画
            foreach (Vector3 vertex in vertices)
            {
                // ワールド空間に変換した頂点を描画
                Gizmos.DrawSphere(objectTransform.TransformPoint(vertex), VertexWidth);
            }
        }
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Gizmos.color = Color.blue;

            // 3頂点を使って三角形を構成
            Vector3 vertex1 = objectTransform.TransformPoint(vertices[triangles[i]]);
            Vector3 vertex2 = objectTransform.TransformPoint(vertices[triangles[i + 1]]);
            Vector3 vertex3 = objectTransform.TransformPoint(vertices[triangles[i + 2]]);

            // 三角形の辺を描画
            Gizmos.DrawLine(vertex1, vertex2);
            Gizmos.DrawLine(vertex2, vertex3);
            Gizmos.DrawLine(vertex3, vertex1);
        }
    }
}