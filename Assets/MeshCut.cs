using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

// unity���b�V��
// https://qiita.com/keito_takaishi/items/8e56d5117ee90502e864
// ���b�V���J�b�g�T���v��
// https://qiita.com/edo_m18/items/31961cd19fd19e09b675
// ���b�V���J�b�g������
// https://qiita.com/suga__/items/87e952e46e205c18e95f

public class MeshCut : MonoBehaviour
{
    [SerializeField]
    private GameObject _subject, cutter;

    // ���b�V��
    private List<Vector3> _vertices;
    private List<Vector3> _normals;
    private List<Vector2> _uvs;
    private List<int> _triangles;

    // ��
    [SerializeField] private Transform _planeTransform;
    // ����������
    private Plane _plane;

    public void GetMeshInfo(GameObject subject)
    {
        MeshFilter filter = subject.GetComponent<MeshFilter>();
        //_vertices = new List<Vector3>(filter.mesh.vertices);
        _vertices = new List<Vector3>(filter.sharedMesh.vertices);
        _normals = new List<Vector3>(filter.sharedMesh.normals);
        _uvs = new List<Vector2>(filter.sharedMesh.uv);
        _triangles = new List<int>(filter.sharedMesh.triangles);
    }

    Vector3 ComputeIntersection(Vector3 p1, Vector3 p2)
    {
        Ray ray = new Ray(p1, p2 - p1);
        float enter;
        if (_plane.Raycast(ray, out enter))
        {
            var normalizedDistance = enter / (p2 - p1).magnitude;
            return Vector3.Lerp(p1, p2, normalizedDistance);

        }
        return Vector3.zero;
    }

    bool ComputeSide(Vector3 p1)
    {
        return _plane.GetSide(p1);
    }

    public void Start()
    {
        Cut(_subject);
    }

    public void Cut(GameObject subject)
    {
        GetMeshInfo(subject);
        _plane = new Plane(_planeTransform.up, _planeTransform.position);

        bool[] sides = new bool[3];
        for (int i = 0; i < _triangles.Count; i += 3)
        {
            var p1 = _vertices[_triangles[i]];
            var p2 = _vertices[_triangles[i + 1]];
            var p3 = _vertices[_triangles[i + 2]];
            // ���ꂼ��̖ʂƂ̍��E����
            sides[0] = ComputeSide(p1);
            sides[1] = ComputeSide(p2);
            sides[2] = ComputeSide(p3);

            // �����ꂩ�̒��_���ʑ��Ɋ���Ă���ꍇ��_���璸�_�𐶐�����
            if (sides[0] != sides[1] || sides[0] != sides[2])
            {
                List<Vector3> addVert = new List<Vector3>();
                if (sides[0] != sides[1])
                {
                    addVert.Add(ComputeIntersection(p1, p2));
                }
                if (sides[1] != sides[2])
                {
                    addVert.Add(ComputeIntersection(p2, p3));
                }
                if (sides[2] != sides[0])
                {
                    addVert.Add(ComputeIntersection(p3, p1));
                }
                foreach (var v in addVert)
                {
                    _vertices.Add(v);
                }
            }
        }
    }

    #region
    private void OnDrawGizmos()
    {
        Gizmos.color = UnityEngine.Color.red;
        float VertexWidth = 0.05f;
        Cut(_subject);

        //_plane = new Plane(_planeTransform.up, _planeTransform.position);

        //GetMeshInfo(_subject);

        var objectTransform = _subject.transform;

        {
            foreach (Vector3 vertex in _vertices)
            {
                Gizmos.DrawSphere(objectTransform.TransformPoint(vertex), VertexWidth);
            }
        }
        //Gizmos.color = UnityEngine.Color.green;
        //var pos = ComputeIntersection(_vertices[_triangles[0]], _vertices[_triangles[1]]);
        //Gizmos.DrawSphere(objectTransform.TransformPoint(_vertices[_triangles[0]]), VertexWidth + 0.01f);
        //Gizmos.DrawSphere(objectTransform.TransformPoint(_vertices[_triangles[1]]), VertexWidth + 0.01f);
        //Gizmos.DrawSphere(pos, VertexWidth + 0.01f);

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
            // Plane�̖@���x�N�g������������`��
            Gizmos.color = UnityEngine.Color.red;
            Gizmos.DrawLine(_planeTransform.position, _planeTransform.position + _planeTransform.up * 5); // �@�������ɐ���`��

            // Plane�̈ʒu�����������ȋ���`��
            Gizmos.color = UnityEngine.Color.green;
            Gizmos.DrawSphere(_planeTransform.position, 0.1f); // Plane�̈ʒu�ɋ���`��
        }
        Gizmos.matrix = Matrix4x4.TRS(_planeTransform.position, _planeTransform.rotation, _planeTransform.localScale);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
    #endregion
}
