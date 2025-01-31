using Cysharp.Threading.Tasks.Triggers;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEditor.AI;
using UnityEngine;

public class MeshCutManager : MonoBehaviour
{
    public List<GameObject> initObjects;
    private int _currentObjectIndex = 0;
    private List<GameObject> _cutTarget = new List<GameObject>();

    private struct InteractData
    {
        public Vector3 firstPosition;
        public Vector3 lastPosition;
        public bool inInteract;
    }

    private Dictionary<GameObject, InteractData> _interactObjects =
        new Dictionary<GameObject, InteractData>();

    public void ResetFunc()
    {
        foreach(var obj in _cutTarget)
        {
            Destroy(obj);
        }
        _cutTarget.Clear();
        _interactObjects.Clear();

        var tmp = Instantiate(initObjects[_currentObjectIndex]);
        tmp.SetActive(true);
        _cutTarget.Add(tmp);
    }

    public void SwitchObject()
    {
        _currentObjectIndex = (_currentObjectIndex + 1) % initObjects.Count;
        ResetFunc();
    }

    private void Awake()
    {
        var obj = Instantiate(initObjects[_currentObjectIndex]);
        initObjects[_currentObjectIndex].SetActive(false);
        _cutTarget.Add(obj);
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        List<GameObject> removeTargets = new List<GameObject>();

        foreach (var target in _cutTarget)
        {
            // 接触の検知
            if (Physics.Raycast(ray, out hit))
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

            Vector3 direction = temp.firstPosition - temp.lastPosition;
            var planeNormal = Vector3.Cross(direction, Vector3.forward).normalized;

            var meshes = MeshCut.CutDevide(target, planePosition, planeNormal);

            bool flag = true;
            for(int i = 0;i<meshes.Count;i++)
            {
                var obj = Instantiate(target);
                meshes[i].InverseTransformPoints(target);
                obj.GetComponent<MeshFilter>().mesh = meshes[i].ConstructMesh();
                obj.GetComponent<MeshCollider>().sharedMesh = obj.GetComponent<MeshFilter>().mesh;
                float dotProduct = Vector3.Dot(planeNormal, Vector3.up);
                if (dotProduct > 0)
                {
                    if (flag) { obj.GetComponent<Rigidbody>().AddForce(Vector3.up * 5); }
                    else { obj.GetComponent<Rigidbody>().AddForce(-Vector3.up * 5); }
                }
                else
                {
                    if (flag) { obj.GetComponent<Rigidbody>().AddForce(-Vector3.up * 5); }
                    else { obj.GetComponent<Rigidbody>().AddForce(Vector3.up * 5); }
                }
                flag = !flag;
                _cutTarget.Add(obj);
            }

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
