using Cysharp.Threading.Tasks.Triggers;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class CutManager : MonoBehaviour
{
    public GameObject initObject;
    private List<GameObject> _cutTarget = new List<GameObject>();
    //[SerializeField] private Rect _cutArea;

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
        //Debug.DrawRay(ray.origin, ray.direction * 10f, Color.green, 5, false);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        foreach (var target in _cutTarget)
        {
            // �I�u�W�F�N�g�̃X�N���[�����W���擾
            Vector3 screenPos = Camera.main.WorldToScreenPoint(target.transform.position);

            // �l�p�`�͈͓��ɂ���ꍇ�A���C�L���X�g�Ŕ���
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == target)
                {
                    // �ΏۃI�u�W�F�N�g���ڐG��Ԃł���Έʒu���X�V
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
                        // �V�K�ڐG�J�n
                        _interactObjects[target] = new InteractData
                        {
                            firstPosition = hit.point,
                            lastPosition = hit.point,
                            inInteract = true
                        };
                    }
                }
            }
            else
            {
                // �͈͊O�ł���ΐڐG�I��
                if (_interactObjects.ContainsKey(target))
                {
                    if (_interactObjects[target].inInteract)
                    {
                        var temp = _interactObjects[target];
                        _cutTarget.Remove(target);
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

                        _cutTarget.Add(positiveObject);
                        _cutTarget.Add(negativeObject);

                        Destroy(target);
                    }
                }
            }
        }
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
