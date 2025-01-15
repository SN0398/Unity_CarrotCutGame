using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawPlane : MonoBehaviour
{
    [SerializeField] private Vector3 positionOffset = Vector3.zero; // 板の位置オフセット
    [SerializeField] private Vector3 rotationEuler = Vector3.zero; // 板の回転（オイラー角）
    [SerializeField] private Vector3 size = new Vector3(1f, 0f, 1f); // 板のサイズ（幅、高さ、厚さ）

    // OnDrawGizmosを使用してGizmosを描画
    private void OnDrawGizmos()
    {
        // 板の位置（オフセットを考慮）
        Vector3 position = transform.position + positionOffset;

        // 回転（オイラー角からQuaternionに変換）
        Quaternion rotation = Quaternion.Euler(rotationEuler);

        // Gizmosの色を設定
        Gizmos.color = Color.green;

        // 回転とスケールを考慮して板を描画
        Gizmos.matrix = Matrix4x4.TRS(position, rotation, Vector3.one); // 位置、回転、スケールの設定
        Gizmos.DrawWireCube(Vector3.zero, size); // ワイヤーフレームの長方形（板）を描画
    }
}
