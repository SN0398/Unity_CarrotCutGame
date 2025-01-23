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

        // targetOffsetの範囲内でランダムな位置を計算
        Vector3 randomOffset = new Vector3(
            Random.Range(-targetOffset.x, targetOffset.x),
            Random.Range(-targetOffset.y, targetOffset.y),
            Random.Range(-targetOffset.z, targetOffset.z)
        );

        // ターゲット位置を算出
        targetPos = targetOrigin + randomOffset;

        Vector3 velocity = Vector3.zero;

        // 射出角をラジアンに変換
        float rad = 20f * Mathf.PI / 180;

        // 水平方向の距離x
        float x = Vector2.Distance(new Vector2(origin.x, origin.z), new Vector2(targetPos.x, targetPos.z));

        // 垂直方向の距離y
        float y = origin.y - targetPos.y;

        // 斜方投射の公式を初速度について解く
        float speed = Mathf.Sqrt(-Physics.gravity.y * Mathf.Pow(x, 2) / (2 * Mathf.Pow(Mathf.Cos(rad), 2) * (x * Mathf.Tan(rad) + y)));

        if (float.IsNaN(speed))
        {
            // 条件を満たす初速を算出できなければVector3.zeroを返す
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
