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
    [SerializeField] private TextMeshProUGUI countdownText;  // �J�E���g�_�E���\���p
    [SerializeField] private TextMeshProUGUI phaseResultText;  // �t�F�[�Y�N���A�\���p
    [SerializeField] private TextMeshProUGUI currentPhaseText;  // ���݂̃t�F�[�Y�\���p
    [SerializeField] private AudioSource audioSource;        // �T�E���h�Đ��p
    [SerializeField] private AudioClip sliceSound;  // �J�E���g�_�E���p����

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
