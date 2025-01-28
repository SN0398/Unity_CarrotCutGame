using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine.SceneManagement;

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

public class GameManager : MonoBehaviour, ICutManager
{
    [SerializeField] private KnifeController _knifeController;
    [SerializeField] private ThrowObjectManager _throwManager;
    private List<GameObject> _cutTarget = new List<GameObject>();
    private List<GameObject> _slicedObject = new List<GameObject>();

    [SerializeField] private List<PhaseData> _phaseData = new List<PhaseData>();

    [Serializable]
    public struct PhaseData
    {
        public float ThrowDelay;
        public int ThrowNum;
    }

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

        Scoreboard.gameObjects.Clear();
        Scoreboard.maxPhase = 0;
    }

    private void Start()
    {
        PhaseLoop().Forget();
    }

    private async UniTask PhaseLoop()
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        int PhaseNum = _phaseData.Count;

        for (int i = 0; i < PhaseNum; i++)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: cts.Token);

            int throwNum = _phaseData[i].ThrowNum;
            float throwDelay = _phaseData[i].ThrowDelay;

            await _throwManager.StartThrowAsync(throwNum, throwDelay, cts);
            await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: cts.Token);
            
            if (_throwManager.IsExistMissedObject())
            {
                break;
            }
            Scoreboard.maxPhase++;
        }
        SceneManager.LoadScene("Result", LoadSceneMode.Single);
    }

    public List<GameObject> CutTarget => new List<GameObject>(_cutTarget);

    public void CutObject(GameObject subject)
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

        Scoreboard.gameObjects.Add(positiveObject);
        Scoreboard.gameObjects.Add(negativeObject);

        Destroy(subject);
    }

    public void AddCutTarget(GameObject target)
    {
        _cutTarget.Add(target);
    }

    public bool ContainTarget(GameObject target)
    {
        return _cutTarget.Contains(target);
    }


}