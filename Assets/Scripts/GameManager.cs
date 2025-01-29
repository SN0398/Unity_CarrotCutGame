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
    /// �ؒf�Ώۂ̃��X�g
    /// </summary>
    /// <returns>�ؒf�Ώۂ̃��X�g�̃f�B�[�v�R�s�[</returns>
    public List<GameObject> CutTarget { get; }
    /// <summary>
    /// �I�u�W�F�N�g�ؒf
    /// </summary>
    /// <param name="subject">�ؒf�Ώ�</param>
    public void CutObject(GameObject subject);
    /// <summary>
    /// �ؒf�Ώۂ̒ǉ�
    /// </summary>
    /// <param name="target">�ؒf�Ώ�</param>
    public void AddCutTarget(GameObject target);
    /// <summary>
    /// �ؒf�Ώۂ����X�g�Ɋ܂܂�Ă��邩�擾����
    /// </summary>
    /// <param name="target">�����Ώ�</param>
    /// <returns>���݂���ΐ^��Ԃ�</returns>
    public bool ContainTarget(GameObject target);
}

public class GameManager : MonoBehaviour, ICutManager
{
    [SerializeField] private KnifeController _knifeController;
    [SerializeField] private ThrowObjectManager _throwManager;
    private List<GameObject> _cutTarget = new List<GameObject>();
    private List<GameObject> _slicedObject = new List<GameObject>();

    [SerializeField] private List<PhaseData> _phaseData = new List<PhaseData>();

    // UI�n
    [SerializeField] private InGameView inGameView;
    private Subject<int> _countdownSubject = new Subject<int>();  // �J�E���g�_�E���̒ʒm
    private Subject<Unit> _countdownFinishedSubject = new Subject<Unit>();  // �J�E���g�_�E���I���̒ʒm
    private Subject<Unit> _playSliceSoundSubject = new Subject<Unit>();  // �T�E���h�Đ��̒ʒm
    private Subject<int> _displaycurrentPhaseSubject = new Subject<int>();  // ���݂̃t�F�[�Y�̒ʒm
    private Subject<bool> _notifyPhaseClearSubject = new Subject<bool>();  // �t�F�[�Y����/���s�̒ʒm

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

        // �C�x���g�o�^
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
            // �t�F�[�Y��񏉊���
            int throwNum = _phaseData[i].ThrowNum;
            float throwDelay = _phaseData[i].ThrowDelay;

            // �X�^�[�g�O�J�E���g�_�E��
            await StartCountdown();

            // �j���W������
            await _throwManager.StartThrowAsync(throwNum, throwDelay, token);
            
            // �؂葹�˂��I�u�W�F�N�g������
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
            // �t�F�[�Y�ڍs����
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
            // �J�E���g�_�E����\��
            _countdownSubject.OnNext(count);
            // 1�b�ҋ@
            await UniTask.Delay(TimeSpan.FromSeconds(1));
        }
        // �J�E���g�_�E���I���ʒm
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

        // SE�Đ��ʒm
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