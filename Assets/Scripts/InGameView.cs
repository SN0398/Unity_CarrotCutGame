using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UniRx;
using TMPro;
using Cysharp.Threading.Tasks;

public interface IInGameView
{
    void DisplayCountdown(int count);
    void HideCountdownText();
    void PlaySliceSound();
}

public sealed class InGameView : MonoBehaviour, IInGameView
{
    [SerializeField] private TextMeshProUGUI countdownText;  // カウントダウン表示用
    [SerializeField] private TextMeshProUGUI phaseResultText;  // フェーズクリア表示用
    [SerializeField] private TextMeshProUGUI currentPhaseText;  // 現在のフェーズ表示用
    [SerializeField] private AudioSource audioSource;        // サウンド再生用
    [SerializeField] private AudioClip sliceSound;  // カウントダウン用音声

    private void Start()
    {
        countdownText.enabled = false;
        phaseResultText.enabled = false;
    }

    public void DisplayCountdown(int count)
    {
        countdownText.enabled = true;
        countdownText.text = count.ToString();
    }

    public void HideCountdownText()
    {
        countdownText.enabled = false;
    }

    public void PlaySliceSound()
    {
        audioSource.PlayOneShot(sliceSound);
    }

    public async UniTask DisplayPhaseResult(bool isClear, float displayDuration)
    {
        var token = this.GetCancellationTokenOnDestroy();
        phaseResultText.enabled = true;
        if (isClear)
        {
            phaseResultText.text = "Phase Clear!";
        }
        else
        {
            phaseResultText.text = "Phase Failed...";
        }
        await UniTask.Delay(TimeSpan.FromSeconds(displayDuration), cancellationToken: token);
        phaseResultText.enabled = false;
    }

    public void DisplayCurrentPhase(int phase)
    {
        currentPhaseText.text = "CurrentPhase " + phase.ToString();
    }
}
