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

    public async UniTask StartThrowAsync(int throwNum, float throwDelay, CancellationToken token)
    {
        List<GameObject> throwObjects = new List<GameObject>();

        // ��ɓ�����l�Q�𐶐����Ă���
        for(int i = 0; i < throwNum;i++)
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

            // �I�u�W�F�N�g�������ē�����
            GameObject subject = Instantiate(_throwObjectPrefab, spawnPoint, Quaternion.identity);
            subject.SetActive(false);
            throwObjects.Add(subject);
            // �ؒf�Ώۂɒǉ�
            cutManager.AddCutTarget(subject);

        }
        foreach(var obj in throwObjects)
        {
            obj.SetActive(true);
            ThrowScript.ThrowObject(obj, _targetOrigin, _targetOffset);
            // �w��b�҂��ČJ��Ԃ�
            await UniTask.Delay(TimeSpan.FromSeconds(throwDelay), cancellationToken: token);
        }
        // �Ō�̐l�Q�������I��邭�炢�܂ő҂�
        await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: token);
    }

    public bool IsMissed(GameObject target)
    {
        if (target.transform.position.z <= Camera.main.transform.position.z) 
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
        foreach(var obj in cutManager.CutTarget)
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
