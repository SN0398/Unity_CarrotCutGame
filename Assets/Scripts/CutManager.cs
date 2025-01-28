using Cysharp.Threading.Tasks.Triggers;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class CutManager : MonoBehaviour
{
    public GameObject initObject;
    private List<GameObject> _cutTarget = new List<GameObject>();

    private struct InteractData
    {
        public Vector3 firstPosition;
        public Vector3 lastPosition;
        public bool inInteract;
    }

    private Dictionary<GameObject, InteractData> _interactObjects =
        new Dictionary<GameObject, InteractData>();

    private void Awake()
    {
        _cutTarget.Add(initObject);
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        bool isHit = Physics.Raycast(ray, out hit);
        List<GameObject> removeTargets = new List<GameObject>();

        foreach (var target in _cutTarget)
        {
            // �ڐG�̌��m
            if (isHit)
            {
                // ���C�L���X�g�ɓ��������̂��ΏۃI�u�W�F�N�g
                if (hit.collider.gameObject == target)
                {
                    // �ΏۃI�u�W�F�N�g���ڐG���ł���Έʒu���X�V
                    if (_interactObjects.ContainsKey(target))
                    {
                        var interactData = _interactObjects[target];
                        if (interactData.inInteract)
                        {
                            interactData.lastPosition = hit.point; // �Ō�ɐG�ꂽ�ʒu���X�V
                            _interactObjects[target] = interactData; // �X�V���ꂽ����ۑ�
                        }
                    }
                    else
                    {
                        Debug.Log("�ڐG�J�n");
                        // �V�K�ڐG�J�n
                        _interactObjects[target] = new InteractData
                        {
                            firstPosition = hit.point,
                            lastPosition = hit.point,
                            inInteract = true
                        };
                    }
                }
                // �ڐG�łȂ����A�ڐG���I�u�W�F�N�g�ɑ��݂��� = �ڐG���I��
                else if (_interactObjects.ContainsKey(target))
                {
                    if (_interactObjects[target].inInteract)
                    {
                        Debug.Log("�ڐG�I��");
                        removeTargets.Add(target);
                    }
                }
            }
        }
        foreach(var target in removeTargets)
        {
            _cutTarget.Remove(target);
            var temp = _interactObjects[target];
            _interactObjects.Remove(target);

            var planePosition = (temp.firstPosition + temp.lastPosition) / 2;

            Vector3 direction = temp.lastPosition - temp.firstPosition;
            var planeNormal = Vector3.Cross(direction, Vector3.forward).normalized;

            var meshes = MeshCut.Cut(target, planePosition, planeNormal);

            var positiveMesh = meshes.positive;
            var negativeMesh = meshes.negative;
            var positiveObject = Instantiate(target);
            var negativeObject = Instantiate(target);

            positiveMesh.InverseTransformPoints(target);
            negativeMesh.InverseTransformPoints(target);

            positiveObject.GetComponent<MeshFilter>().mesh = positiveMesh.ConstructMesh();
            negativeObject.GetComponent<MeshFilter>().mesh = negativeMesh.ConstructMesh();

            positiveObject.GetComponent<MeshCollider>().sharedMesh = positiveObject.GetComponent<MeshFilter>().mesh;
            negativeObject.GetComponent<MeshCollider>().sharedMesh = negativeObject.GetComponent<MeshFilter>().mesh;

            float dotProduct = Vector3.Dot(planeNormal, Vector3.up);
            if (dotProduct > 0)
            {
                positiveObject.GetComponent<Rigidbody>().AddForce(Vector3.up * 5);
                negativeObject.GetComponent<Rigidbody>().AddForce(-Vector3.up * 5);
            }
            else
            {
                negativeObject.GetComponent<Rigidbody>().AddForce(Vector3.up * 5);
                positiveObject.GetComponent<Rigidbody>().AddForce(-Vector3.up * 5);
            }

            _cutTarget.Add(positiveObject);
            _cutTarget.Add(negativeObject);

            Destroy(target);
        }
        removeTargets.Clear();
    }

    private void OnDrawGizmos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * 10); // Draw the ray for visual feedback

        foreach (var target in _cutTarget)
        {
            if (_interactObjects.ContainsKey(target))
            {
                var interactData = _interactObjects[target];

                if (interactData.inInteract)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(interactData.firstPosition, 0.1f); // First touch point
                    Gizmos.DrawSphere(interactData.lastPosition, 0.1f);  // Last touch point
                }
            }
        }
    }
}
