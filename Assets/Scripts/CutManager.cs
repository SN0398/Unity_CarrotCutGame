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
            // 接触の検知
            if (isHit)
            {
                // レイキャストに当たったのが対象オブジェクト
                if (hit.collider.gameObject == target)
                {
                    // 対象オブジェクトが接触中であれば位置を更新
                    if (_interactObjects.ContainsKey(target))
                    {
                        var interactData = _interactObjects[target];
                        if (interactData.inInteract)
                        {
                            interactData.lastPosition = hit.point; // 最後に触れた位置を更新
                            _interactObjects[target] = interactData; // 更新された情報を保存
                        }
                    }
                    else
                    {
                        Debug.Log("接触開始");
                        // 新規接触開始
                        _interactObjects[target] = new InteractData
                        {
                            firstPosition = hit.point,
                            lastPosition = hit.point,
                            inInteract = true
                        };
                    }
                }
                // 接触でないが、接触中オブジェクトに存在する = 接触が終了
                else if (_interactObjects.ContainsKey(target))
                {
                    if (_interactObjects[target].inInteract)
                    {
                        Debug.Log("接触終了");
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
