using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawMeshGizmo : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        var meshFilter = gameObject.GetComponent<MeshFilter>();
        var vertices = new List<Vector3>(meshFilter.sharedMesh.vertices);
        var triangles = new List<int>(meshFilter.sharedMesh.triangles);

		Gizmos.color = UnityEngine.Color.yellow;
        float VertexWidth = 0.035f;

        // ’¸“_‚Ì•`‰æ
        var objectTransform = gameObject.transform;
        {
            foreach (Vector3 vertex in vertices)
            {
                Gizmos.DrawSphere(objectTransform.TransformPoint(vertex), VertexWidth);
            }
        }

        // ƒ|ƒŠƒSƒ“‚Ì•`‰æ
        for (int i = 0; i < triangles.Count; i += 3)
        {
            Gizmos.color = UnityEngine.Color.yellow;

            Vector3 vertex1 = objectTransform.TransformPoint(vertices[triangles[i]]);
            Vector3 vertex2 = objectTransform.TransformPoint(vertices[triangles[i + 1]]);
            Vector3 vertex3 = objectTransform.TransformPoint(vertices[triangles[i + 2]]);

            Gizmos.DrawLine(vertex1, vertex2);
            Gizmos.DrawLine(vertex2, vertex3);
            Gizmos.DrawLine(vertex3, vertex1);

        }

        // –@ü‚Ì•`‰æ
        //Gizmos.color = new Color(0, 0, 1, 1f);
        //for (int i = 0; i < meshFilter.sharedMesh.normals.Length; i++)
        //{
        //    Vector3 normal = objectTransform.TransformDirection(meshFilter.sharedMesh.normals[i]);
        //    Vector3 vertex = objectTransform.TransformPoint(meshFilter.sharedMesh.vertices[i]);
        //    Gizmos.DrawLine(vertex, vertex + normal);
        //}
    }
}
