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
    [SerializeField] private GameObject _throwObjectPrefab;             // ��������I�u�W�F�N�g
    [SerializeField] private Vector3 _targetOrigin;                     // �����ڕW���W
    [SerializeField] private Vector3 _targetOffset = Vector3.zero;      // �����ڕW���W����̂���
    [SerializeField] private List<Vector3> _spanwnPoint;                // �������J�n����ʒu

    public ICutManager cutManager;
    private int _prevIndex;         // �O��̃X�|�[���ŗ��p�����X�|�[���|�C���g�ւ̃C���f�b�N�X
    private List<IThrowObject> throwsList = new List<IThrowObject>();

    public GameObject GenerateThrowObject()
    {
        // �����_���Ȉʒu����l�Q�𓊂���
        var index = UnityEngine.Random.Range(0, _spanwnPoint.Count - 1);
        // �O��Ɠ����ʒu�ɂ̓X�|�[�������Ȃ�
        if (index == _prevIndex)
        {
            index = (index + 1) % _spanwnPoint.Count;
        }
        _prevIndex = index;
        Vector3 spawnPoint = _spanwnPoint[index];
        // �I�u�W�F�N�g����
        GameObject subject = Instantiate(_throwObjectPrefab, spawnPoint, Quaternion.identity);
        return subject;
    }

    public async UniTask StartThrowAsync(float phaseTime,  float throwSpeed, float throwNumPerSecond, CancellationToken token)
    {
        List<GameObject> throwObjects = new List<GameObject>();

        // ��ɓ�����l�Q�𐶐����Ă���
        for (int i = 0; i < phaseTime / throwNumPerSecond; i++) 
        {
            // �I�u�W�F�N�g�������ē�����
            GameObject subject = GenerateThrowObject();
            // ���ۂɓ��˂��J�n����܂Ŗ��点��
            subject.SetActive(false);
            throwObjects.Add(subject);
            // �ؒf�Ώۂɒǉ�
            cutManager.AddCutTarget(subject);
        }
        // ������
        foreach(var obj in throwObjects)
        {
            IThrowObject throwObject = new ThrowObject(obj);
            obj.SetActive(true);
            throwObject.StartThrow(_targetOrigin + _targetOffset, throwSpeed, token);
            throwsList.Add(throwObject);
            // �w��b�҂��ČJ��Ԃ�
            await UniTask.Delay(TimeSpan.FromSeconds(throwNumPerSecond), cancellationToken: token);
        }
        // �Ō�̐l�Q�������I��邭�炢�܂ő҂�
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
    /// �ؑ��˂��I�u�W�F�N�g�����݂��邩
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
            Gizmos.DrawSphere(obj, 0.1f); // Plane�̈ʒu�ɋ���`��
        }
    }
}
