using System.Collections;
using System.Collections.Generic;
using TMPro.SpriteAssetUtilities;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;



public class KnifeController : MonoBehaviour
{
    [SerializeField] private GameObject _knifeObject;
    [SerializeField] private float _recordInterval = 0.2f; // �L�^�̊Ԋu�i�b�j
    [SerializeField] private float _rotationSpeed = 10f; // ��]�̑��x

    private Quaternion _defaultRotation = Quaternion.identity;
    private List<Vector3> _trailPositions = new List<Vector3>();
    private float _timeSinceLastRecord = 0f;
    private int _maxTrailRecordCount = 20;

    public GameObject KnifeObject
    {
        get => _knifeObject;
        private set => _knifeObject = value;
    }

    public ICutManager cutManager;

    private void Start()
    {
        _defaultRotation = _knifeObject.transform.rotation;
        _trailPositions.Clear();
    }

    void Update()
    {
        MoveObject();
        RecordTrail();
        RotateObject();
    }

    /// <summary>
    /// ����}�E�X�ʒu�ɒǏ]������
    /// </summary>
    public void MoveObject()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 pos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 2f));
        _knifeObject.transform.position = pos;
    }

    /// <summary>
    /// ����ړ������Ɍ�����
    /// </summary>
    private void RotateObject()
    {
        // _trailPositions�ɏ\���ȃf�[�^���Ȃ��ꍇ�͏������X�L�b�v
        if (_trailPositions.Count < 2) { return; }

        var p1 = _trailPositions[_trailPositions.Count - 2];
        var p2 = _trailPositions[_trailPositions.Count - 1];

        // ���݂̈ʒu�ƑO�̈ʒu�Ƃ̋������߂�������
        if (Vector3.Distance(p1, p2) < 0.05f) { return; }

        // �ړ��������v�Z
        Vector3 direction = (p1 - p2).normalized;

        // Z���̉�]���v�Z�iatan2���g�p���Ċp�x�����߂�j
        float angleZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // X����Y���̉�]���ێ����AZ���݂̂�ύX
        Quaternion targetRotation = Quaternion.Euler(
            _defaultRotation.eulerAngles.x,
            _defaultRotation.eulerAngles.y,
            angleZ);

        // ��]��K�p
        _knifeObject.transform.rotation = Quaternion.Lerp(
                _knifeObject.transform.rotation,  // ���݂̉�]
                targetRotation,                  // �ڕW��]
                Time.deltaTime * _rotationSpeed  // �X���[�W���O���x
            );
    }

    /// <summary>
    /// ����ʂ����ʒu���L�^����
    /// </summary>
    private void RecordTrail()
    {
        _timeSinceLastRecord += Time.deltaTime;

        if (_timeSinceLastRecord >= _recordInterval)
        {
            _trailPositions.Add(_knifeObject.transform.position);
            // ���X�g���v�f�����������Ȃ��悤�ɂ��鏈��
            if (_trailPositions.Count > _maxTrailRecordCount)
            {
                _trailPositions.RemoveAt(0); // �Â����ɍ폜
            }
            // ���R�[�h���Ԃ����Z�b�g
            _timeSinceLastRecord = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // �ڐG�����I�u�W�F�N�g���^�[�Q�b�g���X�g�Ɋ܂܂�Ă��邩�`�F�b�N
        if (cutManager.ContainTarget(other.gameObject))
        {
            // ���b�V���J�b�g���s��
            cutManager.CutObject(other.gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < _trailPositions.Count - 1; i++) 
        {
            Gizmos.DrawLine(_trailPositions[i], _trailPositions[i + 1]);
        }
        var planePosition = _knifeObject.transform.position;
        var planeNormal = _knifeObject.transform.up;
        var plane = new Plane(planeNormal, planePosition);

        // Plane�̖@���x�N�g������������`��
        Gizmos.DrawLine(_knifeObject.transform.position, _knifeObject.transform.position + _knifeObject.transform.up * 5); // �@�������ɐ���`��

        // Plane�̈ʒu�����������ȋ���`��
        Gizmos.color = UnityEngine.Color.green;
        Gizmos.DrawSphere(_knifeObject.transform.position, 0.1f); // Plane�̈ʒu�ɋ���`��

        Gizmos.matrix = Matrix4x4.TRS(planePosition, Quaternion.LookRotation(planeNormal), Vector3.one);
        Gizmos.color = new Color(0, 1, 0, 0.4f);
        Gizmos.DrawCube(Vector3.zero, new Vector3(2, 2, 0f)); // Matrix�Ɋ�Â��ʒu�ɕ`��
        Gizmos.color = new Color(0, 1, 0, 1f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(2, 2, 0f)); // Matrix�Ɋ�Â��ʒu�ɕ`��
        Gizmos.matrix = transform.localToWorldMatrix;
    }
}
