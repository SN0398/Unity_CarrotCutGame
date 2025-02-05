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

        while (time < duration)
        {
            if (_subject == null) { break; }

            time += Time.deltaTime * _speed;
            _distance = time / duration;

            _subject.transform.position = Parabolic(_startPos, _endPos, _distance);

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
        }
    }

    public Vector3 Parabolic(Vector3 a, Vector3 b, float t)
    {
        float distance = Vector3.Distance(a, b);

        float height = Mathf.Abs(b.y - a.y) + 2f;

        Vector3 mid = (a + b) / 2;
        mid.y += height;

        Vector3 p0 = a;
        Vector3 p1 = mid;
        Vector3 p2 = b;

        // ベジェ曲線
        Vector3 position = (1 - t) * (1 - t) * p0 + 2 * (1 - t) * t * p1 + t * t * p2;

        return position;
    }
}
