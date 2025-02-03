using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UniRx.InternalUtil;
using System;
using UnityEngine.UIElements;

public class ThrowObjectManager : MonoBehaviour
{
    [SerializeField] private GameObject _throwObjectPrefab;             // 投擲するオブジェクト
    [SerializeField] private Vector3 _targetOrigin;                     // 投擲目標座標
    [SerializeField] private Vector3 _targetOffset = Vector3.zero;      // 投擲目標座標からのずれ
    [SerializeField] private List<Vector3> _spanwnPoint;                // 投擲を開始する位置

    public ICutManager cutManager;
    private int _prevIndex;         // 前回のスポーンで利用したスポーンポイントへのインデックス
    private List<IThrowObject> throwsList = new List<IThrowObject>();

    public GameObject GenerateThrowObject()
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
        // オブジェクト複製
        GameObject subject = Instantiate(_throwObjectPrefab, spawnPoint, Quaternion.identity);
        return subject;
    }

    public async UniTask StartThrowAsync(float phaseTime,  float throwSpeed, float throwNumPerSecond, CancellationToken token)
    {
        List<GameObject> throwObjects = new List<GameObject>();

        // 先に投げる人参を生成しておく
        for (int i = 0; i < phaseTime / throwNumPerSecond; i++) 
        {
            // オブジェクト複製して投げる
            GameObject subject = GenerateThrowObject();
            // 実際に投射を開始するまで眠らせる
            subject.SetActive(false);
            throwObjects.Add(subject);
            // 切断対象に追加
            cutManager.AddCutTarget(subject);
        }
        // 投げる
        foreach(var obj in throwObjects)
        {
            IThrowObject throwObject = new ThrowObject(obj);
            obj.SetActive(true);
            throwObject.StartThrow(_targetOrigin + _targetOffset, throwSpeed, token);
            throwsList.Add(throwObject);
            // 指定秒待って繰り返し
            await UniTask.Delay(TimeSpan.FromSeconds(throwNumPerSecond), cancellationToken: token);
        }
        // 最後の人参が投げ終わるくらいまで待つ
        await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: token);
    }

    public bool IsMissed(IThrowObject target)
    {
        if (target.currentDistance >= 1f) 
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
        foreach(var obj in throwsList)
        {
            if(IsMissed(obj))
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
