using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowObjectManager : MonoBehaviour
{
    [SerializeField] private GameObject _throwObjectPrefab;
    [SerializeField] private Vector3 _targetOrigin;
    [SerializeField] private Vector3 _targetOffset;
    [SerializeField] private float _spawnTime;
    [SerializeField] private List<Vector3> _spanwnPoint;
    [SerializeField] private KnifeController _knifeController;
    private float timeSince;

    // Start is called before the first frame update
    void Start()
    {
        // �R���[�`���̋N��
        StartCoroutine(DelayCoroutine());
    }

    // �R���[�`���{��
    private IEnumerator DelayCoroutine()
    {
        foreach (var obj in _spanwnPoint)
        {
            GameObject subject = Instantiate(_throwObjectPrefab, obj, Quaternion.identity);
            _knifeController.CutTarget.Add(subject);
            ThrowScript.ThrowObject(subject, _targetOrigin, _targetOffset);

            // 3�b�ԑ҂�
            yield return new WaitForSeconds(0.5f);
        }
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
