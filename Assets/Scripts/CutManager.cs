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
            // オブジェクトのスクリーン座標を取得
            Vector3 screenPos = Camera.main.WorldToScreenPoint(target.transform.position);

            // 四角形範囲内にいる場合、レイキャストで判定
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == target)
                {
                    // 対象オブジェクトが接触状態であれば位置を更新
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
                        // 新規接触開始
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
                // 範囲外であれば接触終了
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
