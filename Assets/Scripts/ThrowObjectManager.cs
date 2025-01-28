using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UniRx.InternalUtil;
using System;

public class ThrowObjectManager : MonoBehaviour
{
    [SerializeField] private GameObject _throwObjectPrefab;
    [SerializeField] private Vector3 _targetOrigin;
    [SerializeField] private Vector3 _targetOffset;
    [SerializeField] private List<Vector3> _spanwnPoint;

    public ICutManager cutManager;
    private int _prevIndex;

    public async UniTask StartThrowAsync(int throwNum, float throwDelay, CancellationTokenSource cts)
    {
        var token = cts.Token;
        for(int i = 0; i < throwNum;i++)
        {
            // ランダムな位置から人参を投げる
            var index = UnityEngine.Random.Range(0, _spanwnPoint.Count - 1);
            // 前回と同じ位置にはスポーンさせない
            if (index == _prevIndex)
            {
                index = (index + 1) % _spanwnPoint.Count;
            }
            _prevIndex = index;
            Vector3 spawnPoint = _spanwnPoint[index];

            // オブジェクト複製して投げる
            GameObject subject = Instantiate(_throwObjectPrefab, spawnPoint, Quaternion.identity);
            ThrowScript.ThrowObject(subject, _targetOrigin, _targetOffset);
            // 切断対象に追加
            cutManager.AddCutTarget(subject);

            // 指定秒待って繰り返し
            await UniTask.Delay(TimeSpan.FromSeconds(throwDelay), cancellationToken: token);
        }
    }

    public bool IsMissed(Vector3 position)
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
    public bool IsExistMissedObject()
    {
        foreach(var obj in cutManager.CutTarget)
        {
            if(IsMissed(obj.transform.position))
            {
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
