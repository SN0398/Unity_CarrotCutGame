using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine.SceneManagement;
using UniRx;

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

    // UI系
    [SerializeField] private InGameView inGameView;
    private Subject<int> _countdownSubject = new Subject<int>();  // カウントダウンの通知
    private Subject<Unit> _countdownFinishedSubject = new Subject<Unit>();  // カウントダウン終了の通知
    private Subject<Unit> _playSliceSoundSubject = new Subject<Unit>();  // サウンド再生の通知
    private Subject<int> _displaycurrentPhaseSubject = new Subject<int>();  // 現在のフェーズの通知
    private Subject<bool> _notifyPhaseClearSubject = new Subject<bool>();  // フェーズ成功/失敗の通知

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

        inGameView.HideCountdownText();

        // イベント登録
        _countdownSubject.Subscribe(x => inGameView.DisplayCountdown(x));
        _countdownFinishedSubject.Subscribe(x => inGameView.HideCountdownText());
        _playSliceSoundSubject.Subscribe(x => inGameView.PlaySliceSound());
        _displaycurrentPhaseSubject.Subscribe(x => inGameView.DisplayCurrentPhase(x));
        _notifyPhaseClearSubject.Subscribe(x => inGameView.DisplayPhaseResult(x,2f).Forget());

        Scoreboard.gameObjects.Clear();
        Scoreboard.maxPhase = 0;
    }

    private void Start()
    {
        PhaseLoop().Forget();
        _displaycurrentPhaseSubject.OnNext(Scoreboard.maxPhase + 1);
    }

    private async UniTask PhaseLoop()
    {
        CancellationToken token = this.GetCancellationTokenOnDestroy();
        int PhaseNum = _phaseData.Count;

        for (int i = 0; i < PhaseNum; i++)
        {
            // フェーズ情報初期化
            int throwNum = _phaseData[i].ThrowNum;
            float throwDelay = _phaseData[i].ThrowDelay;

            // スタート前カウントダウン
            await StartCountdown();

            // ニンジン投げ
            await _throwManager.StartThrowAsync(throwNum, throwDelay, token);
            
            // 切り損ねたオブジェクトがある
            if (!_throwManager.IsExistMissedObject())
            {
                _notifyPhaseClearSubject.OnNext(true);
                await UniTask.Delay(TimeSpan.FromSeconds(2f));
            }
            else
            {
                _notifyPhaseClearSubject.OnNext(false);
                await UniTask.Delay(TimeSpan.FromSeconds(2f));
                break;
            }
            // フェーズ移行処理
            Scoreboard.maxPhase++;
            var phase = Scoreboard.maxPhase + 1;
            _displaycurrentPhaseSubject.OnNext(phase);
        }
        SceneManager.LoadScene("Result", LoadSceneMode.Single);
    }

    public async UniTask StartCountdown()
    {
        int CountdownNum = 3;
        for (int count = CountdownNum; count > 0; count--)
        {
            // カウントダウンを表示
            _countdownSubject.OnNext(count);
            // 1秒待機
            await UniTask.Delay(TimeSpan.FromSeconds(1));
        }
        // カウントダウン終了通知
        _countdownFinishedSubject.OnNext(Unit.Default);
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

        DontDestroyOnLoad(positiveObject);
        DontDestroyOnLoad(negativeObject);

        Destroy(subject);

        // SE再生通知
        _playSliceSoundSubject.OnNext(Unit.Default);
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