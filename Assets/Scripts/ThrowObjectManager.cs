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
        // �R���[�`���̋N��
        StartCoroutine(DelayCoroutine());
    }

    // �R���[�`���{��
    private IEnumerator DelayCoroutine()
    {
        while(--_throwNum >= 0)
        {
            // �����_���Ȉʒu����l�Q�𓊂���
            // �O��Ɠ����ʒu�ɂ̓X�|�[�������Ȃ�
            var index = Random.Range(0, _spanwnPoint.Count - 1);
            if(index == _prevIndex) 
            {
                index = (index + 1) % _spanwnPoint.Count; 
            }
            _prevIndex = index;
            Vector3 spawnPoint = _spanwnPoint[index];

            // �I�u�W�F�N�g�������ē�����
            GameObject subject = Instantiate(_throwObjectPrefab, spawnPoint, Quaternion.identity);
            ThrowScript.ThrowObject(subject, _targetOrigin, _targetOffset);
            cutManager.AddCutTarget(subject);
            // ��莞�ԑ҂�
            yield return new WaitForSeconds(_throwDelay);
        }
        Debug.Log("All Sliced!");
        CallThrowingFinished();
    }

    private void FixedUpdate()
    {
        // �؂葹�˂��j���W��������
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
    /// �ؑ��˂��I�u�W�F�N�g�����݂��邩
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
            Gizmos.DrawSphere(obj, 0.1f); // Plane�̈ʒu�ɋ���`��
        }
    }
}
