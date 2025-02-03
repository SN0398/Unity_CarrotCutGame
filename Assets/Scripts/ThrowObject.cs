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
            Debug.LogError("�����Ώۂ��s��");
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
        Vector3 velocity = (_endPos - _startPos) / duration; // XZ�����̈ړ����x
        velocity.y = 5f; // ������̏����x��ݒ�i�����\�j
        float gravity = 9.8f; // �d�͉����x

        while (time < duration)
        {
            if (_subject == null) { break; }

            time += Time.deltaTime;
            _distance = time / duration;
            // XZ�����̈ړ�
            Vector3 pos = _startPos + velocity * time;

            // Y�����̈ړ��i�����^���j
            pos.y = _startPos.y + velocity.y * time - 0.5f * gravity * time * time;

            _subject.transform.position = pos;

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
        }

        // �ŏI�ʒu�̕␳�i�I�_�Ƀs�b�^�������悤�Ɂj
        if (_subject != null)
        {
            _subject.transform.position = _endPos;
        }
    }
}
