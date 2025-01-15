using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawPlane : MonoBehaviour
{
    [SerializeField] private Vector3 positionOffset = Vector3.zero; // �̈ʒu�I�t�Z�b�g
    [SerializeField] private Vector3 rotationEuler = Vector3.zero; // �̉�]�i�I�C���[�p�j
    [SerializeField] private Vector3 size = new Vector3(1f, 0f, 1f); // �̃T�C�Y�i���A�����A�����j

    // OnDrawGizmos���g�p����Gizmos��`��
    private void OnDrawGizmos()
    {
        // �̈ʒu�i�I�t�Z�b�g���l���j
        Vector3 position = transform.position + positionOffset;

        // ��]�i�I�C���[�p����Quaternion�ɕϊ��j
        Quaternion rotation = Quaternion.Euler(rotationEuler);

        // Gizmos�̐F��ݒ�
        Gizmos.color = Color.green;

        // ��]�ƃX�P�[�����l�����Ĕ�`��
        Gizmos.matrix = Matrix4x4.TRS(position, rotation, Vector3.one); // �ʒu�A��]�A�X�P�[���̐ݒ�
        Gizmos.DrawWireCube(Vector3.zero, size); // ���C���[�t���[���̒����`�i�j��`��
    }
}
