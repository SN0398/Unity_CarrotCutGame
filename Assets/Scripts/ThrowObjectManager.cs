using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowObjectManager : MonoBehaviour
{
    [SerializeField] private GameObject _throwObjectPrefab;
    [SerializeField] private Vector3 _targetOrigin;
    [SerializeField] private Vector3 _targetOffset;
    [SerializeField] private List<Vector3> _spanwnPoint;
    [SerializeField] private float _throwDelay;
    [SerializeField] private float _throwNum;

    public ICutManager cutManager;
    private int _prevIndex;

    // Start is called before the first frame update
    void Start()
    {
        // コルーチンの起動
        StartCoroutine(DelayCoroutine());
    }

    // コルーチン本体
    private IEnumerator DelayCoroutine()
    {
        while(--_throwNum >= 0)
        {
            // ランダムな位置から人参を投げる
            // 前回と同じ位置にはスポーンさせない
            var index = Random.Range(0, _spanwnPoint.Count - 1);
            if(index == _prevIndex) 
            {
                index = (index + 1) % _spanwnPoint.Count; 
            }
            _prevIndex = index;
            Vector3 spawnPoint = _spanwnPoint[index];

            // オブジェクト複製して投げる
            GameObject subject = Instantiate(_throwObjectPrefab, spawnPoint, Quaternion.identity);
            ThrowScript.ThrowObject(subject, _targetOrigin, _targetOffset);
            cutManager.AddCutTarget(subject);
            // 一定時間待つ
            yield return new WaitForSeconds(_throwDelay);
        }
        Debug.Log("All Sliced!");
        CallThrowingFinished();
    }

    private void FixedUpdate()
    {
        // 切り損ねたニンジンがある
        if(IsExistMissedObject())
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }

    private void CallThrowingFinished()
    {
        StartCoroutine(DelayCoroutine());
    }

    private bool IsMissed(Vector3 position)
    {
        if (position.z <= Camera.main.transform.position.z) 
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 切損ねたオブジェクトが存在するか
    /// </summary>
    /// <returns></returns>
    private bool IsExistMissedObject()
    {
        foreach(var obj in cutManager.CutTarget)
        {
            if(IsMissed(obj.transform.position))
            {
                Debug.Log("Missed : " + obj.name);
                return true;
            }
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (var obj in _spanwnPoint)
        {
            Gizmos.DrawSphere(obj, 0.1f); // Planeの位置に球を描画
        }
    }
}
