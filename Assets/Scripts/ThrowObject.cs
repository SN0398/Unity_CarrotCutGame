using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Runtime.CompilerServices;
using TMPro;
using System;
using UniRx;
using System.Threading;

public interface IThrowObject
{
    public void StartThrow(Vector3 targetPos, float speed, CancellationToken token);
    public float currentDistance { get; }
}

public class ThrowObject : IThrowObject
{
    private GameObject _subject;
    private Vector3 _startPos;
    private Vector3 _endPos;
    private float _speed = 0f;
    private float _distance = 0f;

    public float currentDistance => _distance;

    public ThrowObject(GameObject subject)
    {
        _subject = subject;
        _startPos = _subject.transform.position;
    }

    public void StartThrow(Vector3 targetPos, float speed, CancellationToken token)
    {
        if(_subject == null) 
        {
            Debug.LogError("投擲対象が不明");
            return; 
        }
        _startPos = _subject.transform.position;
        _endPos = targetPos;
        _speed = speed;
        ThrowProcess(token).Forget();
    }

    private async UniTask ThrowProcess(CancellationToken token)
    {
        float time = 0f;
        float duration = 1f;
        Vector3 velocity = (_endPos - _startPos) / duration; // XZ方向の移動速度
        velocity.y = 5f; // 上方向の初速度を設定（調整可能）
        float gravity = 9.8f; // 重力加速度

        while (time < duration)
        {
            if (_subject == null) { break; }

            time += Time.deltaTime;
            _distance = time / duration;
            // XZ方向の移動
            Vector3 pos = _startPos + velocity * time;

            // Y方向の移動（放物運動）
            pos.y = _startPos.y + velocity.y * time - 0.5f * gravity * time * time;

            _subject.transform.position = pos;

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
        }

        // 最終位置の補正（終点にピッタリ合うように）
        if (_subject != null)
        {
            _subject.transform.position = _endPos;
        }
    }
}
