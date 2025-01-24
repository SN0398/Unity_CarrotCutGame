using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public interface ICutManager
{
    /// <summary>
    /// 切断対象のリスト
    /// </summary>
    /// <returns>切断対象のリストのディープコピー</returns>
    public List<GameObject> CutTarget { get; }
    /// <summary>
    /// オブジェクト切断
    /// </summary>
    /// <param name="subject">切断対象</param>
    public void CutObject(GameObject subject);
    /// <summary>
    /// 切断対象の追加
    /// </summary>
    /// <param name="target">切断対象</param>
    public void AddCutTarget(GameObject target);
    /// <summary>
    /// 切断対象がリストに含まれているか取得する
    /// </summary>
    /// <param name="target">検査対象</param>
    /// <returns>存在すれば真を返す</returns>
    public bool ContainTarget(GameObject target);
}

public class CutManager : MonoBehaviour, ICutManager
{
    [SerializeField] private KnifeController _knifeController;
    [SerializeField] private ThrowObjectManager _throwManager;
    private List<GameObject> _cutTarget = new List<GameObject>();
    private List<GameObject> _slicedObject = new List<GameObject>();

    private void Awake()
    {
        _cutTarget.Clear();
        _slicedObject.Clear();

        if(_knifeController == null)
        {
            Debug.LogError("CutManager : KnifeController is not attached!");
        }
        if(_throwManager == null)
        {
            Debug.LogError("CutManager : ThrowObjectManager is not attached!");
        }
        _knifeController.cutManager = this;
        _throwManager.cutManager = this;
    }

    public List<GameObject> CutTarget
    {
        get => new List<GameObject>(_cutTarget);
    }

    void ICutManager.CutObject(GameObject subject)
    {
        _cutTarget.Remove(subject);

        var planePosition = _knifeController.KnifeObject.transform.position;
        var planeNormal = _knifeController.KnifeObject.transform.up;
        var meshes = MeshCut.Cut(subject, planePosition, planeNormal);

        var positiveMesh = meshes.positive;
        var negativeMesh = meshes.negative;
        var positiveObject = Instantiate(subject);
        var negativeObject = Instantiate(subject);
        var objectName = subject.name;
        positiveObject.name = objectName + "_Positive";
        negativeObject.name = objectName + "_Negative";

        positiveMesh.InverseTransformPoints(subject);
        negativeMesh.InverseTransformPoints(subject);

        positiveObject.GetComponent<MeshFilter>().mesh = positiveMesh.ConstructMesh();
        negativeObject.GetComponent<MeshFilter>().mesh = negativeMesh.ConstructMesh();

        positiveObject.GetComponent<MeshCollider>().sharedMesh = positiveObject.GetComponent<MeshFilter>().mesh;
        negativeObject.GetComponent<MeshCollider>().sharedMesh = negativeObject.GetComponent<MeshFilter>().mesh;

        _slicedObject.Add(positiveObject);
        _slicedObject.Add(negativeObject);

        Destroy(subject);
    }

    void ICutManager.AddCutTarget(GameObject target)
    {
        _cutTarget.Add(target);
    }

    bool ICutManager.ContainTarget(GameObject target)
    {
        return _cutTarget.Contains(target);
    }
}