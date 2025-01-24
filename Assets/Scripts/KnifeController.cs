using System.Collections;
using System.Collections.Generic;
using TMPro.SpriteAssetUtilities;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;



public class KnifeController : MonoBehaviour
{
    [SerializeField] private GameObject _knifeObject;
    [SerializeField] private float _recordInterval = 0.2f; // 記録の間隔（秒）
    [SerializeField] private float _rotationSpeed = 10f; // 回転の速度

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
    /// 包丁をマウス位置に追従させる
    /// </summary>
    public void MoveObject()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 pos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 2f));
        _knifeObject.transform.position = pos;
    }

    /// <summary>
    /// 包丁を移動方向に向ける
    /// </summary>
    private void RotateObject()
    {
        // _trailPositionsに十分なデータがない場合は処理をスキップ
        if (_trailPositions.Count < 2) { return; }

        var p1 = _trailPositions[_trailPositions.Count - 2];
        var p2 = _trailPositions[_trailPositions.Count - 1];

        // 現在の位置と前の位置との距離が近すぎたら
        if (Vector3.Distance(p1, p2) < 0.05f) { return; }

        // 移動方向を計算
        Vector3 direction = (p1 - p2).normalized;

        // Z軸の回転を計算（atan2を使用して角度を求める）
        float angleZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // X軸とY軸の回転を維持し、Z軸のみを変更
        Quaternion targetRotation = Quaternion.Euler(
            _defaultRotation.eulerAngles.x,
            _defaultRotation.eulerAngles.y,
            angleZ);

        // 回転を適用
        _knifeObject.transform.rotation = Quaternion.Lerp(
                _knifeObject.transform.rotation,  // 現在の回転
                targetRotation,                  // 目標回転
                Time.deltaTime * _rotationSpeed  // スムージング速度
            );
    }

    /// <summary>
    /// 包丁が通った位置を記録する
    /// </summary>
    private void RecordTrail()
    {
        _timeSinceLastRecord += Time.deltaTime;

        if (_timeSinceLastRecord >= _recordInterval)
        {
            _trailPositions.Add(_knifeObject.transform.position);
            // リストが要素が増えすぎないようにする処理
            if (_trailPositions.Count > _maxTrailRecordCount)
            {
                _trailPositions.RemoveAt(0); // 古い順に削除
            }
            // レコード時間をリセット
            _timeSinceLastRecord = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 接触したオブジェクトがターゲットリストに含まれているかチェック
        if (cutManager.ContainTarget(other.gameObject))
        {
            // メッシュカットを行う
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

        // Planeの法線ベクトルを示す線を描画
        Gizmos.DrawLine(_knifeObject.transform.position, _knifeObject.transform.position + _knifeObject.transform.up * 5); // 法線方向に線を描画

        // Planeの位置を示す小さな球を描画
        Gizmos.color = UnityEngine.Color.green;
        Gizmos.DrawSphere(_knifeObject.transform.position, 0.1f); // Planeの位置に球を描画

        Gizmos.matrix = Matrix4x4.TRS(planePosition, Quaternion.LookRotation(planeNormal), Vector3.one);
        Gizmos.color = new Color(0, 1, 0, 0.4f);
        Gizmos.DrawCube(Vector3.zero, new Vector3(2, 2, 0f)); // Matrixに基づく位置に描画
        Gizmos.color = new Color(0, 1, 0, 1f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(2, 2, 0f)); // Matrixに基づく位置に描画
        Gizmos.matrix = transform.localToWorldMatrix;
    }
}
