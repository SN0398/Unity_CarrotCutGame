using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowScript
{
    public static void ThrowObject(GameObject subject, Vector3 targetOrigin, Vector3 targetOffset)
    {
        Rigidbody rb = subject.GetComponent<Rigidbody>();
        Vector3 origin = subject.transform.position;
        Vector3 targetPos = Vector3.zero;

        // targetOffset�͈͓̔��Ń����_���Ȉʒu���v�Z
        Vector3 randomOffset = new Vector3(
            Random.Range(-targetOffset.x, targetOffset.x),
            Random.Range(-targetOffset.y, targetOffset.y),
            Random.Range(-targetOffset.z, targetOffset.z)
        );

        // �^�[�Q�b�g�ʒu���Z�o
        targetPos = targetOrigin + randomOffset;

        Vector3 velocity = Vector3.zero;

        // �ˏo�p�����W�A���ɕϊ�
        float rad = 20f * Mathf.PI / 180;

        // ���������̋���x
        float x = Vector2.Distance(new Vector2(origin.x, origin.z), new Vector2(targetPos.x, targetPos.z));

        // ���������̋���y
        float y = origin.y - targetPos.y;

        // �Ε����˂̌����������x�ɂ��ĉ���
        float speed = Mathf.Sqrt(-Physics.gravity.y * Mathf.Pow(x, 2) / (2 * Mathf.Pow(Mathf.Cos(rad), 2) * (x * Mathf.Tan(rad) + y)));

        if (float.IsNaN(speed))
        {
            // �����𖞂����������Z�o�ł��Ȃ����Vector3.zero��Ԃ�
            velocity =  Vector3.zero;
        }
        else
        {
            velocity = (new Vector3(targetPos.x - origin.x, x * Mathf.Tan(rad), targetPos.z - origin.z).normalized * speed);
        }

        velocity *= rb.mass;

        rb.AddForce(velocity, ForceMode.Impulse);
    }
}
