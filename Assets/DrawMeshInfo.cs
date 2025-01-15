using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
public class DrawMeshInfo : MonoBehaviour
{
    public float VertexWidth = 0.05f;
    // OnDrawGizmos() ���\�b�h���g�p���āA���_��`��
    private void OnDrawGizmos()
    {
        // ���b�V���t�B���^�[���擾
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        // ���b�V���̒��_�Q���擾
        List<Vector3> vertices = new List<Vector3>(meshFilter.sharedMesh.vertices);
        int[] triangles = meshFilter.sharedMesh.triangles;

        // ���[���h���W�ɕϊ�
        Transform objectTransform = transform;
        Gizmos.color = Color.red;

        // ���_
        {
            // ���_��`��
            foreach (Vector3 vertex in vertices)
            {
                // ���[���h��Ԃɕϊ��������_��`��
                Gizmos.DrawSphere(objectTransform.TransformPoint(vertex), VertexWidth);
            }
        }
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Gizmos.color = Color.blue;

            // 3���_���g���ĎO�p�`���\��
            Vector3 vertex1 = objectTransform.TransformPoint(vertices[triangles[i]]);
            Vector3 vertex2 = objectTransform.TransformPoint(vertices[triangles[i + 1]]);
            Vector3 vertex3 = objectTransform.TransformPoint(vertices[triangles[i + 2]]);

            // �O�p�`�̕ӂ�`��
            Gizmos.DrawLine(vertex1, vertex2);
            Gizmos.DrawLine(vertex2, vertex3);
            Gizmos.DrawLine(vertex3, vertex1);
        }
    }
}