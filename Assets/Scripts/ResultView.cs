using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine.SceneManagement;

public class ResultView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private Vector3 _spawnPoint;

    void Start()
    {
        _scoreText.text = "Max phase ... " + Scoreboard.maxPhase.ToString();
        SpawnCutTargets().Forget();
    }

    private async UniTaskVoid SpawnCutTargets()
    {
        CancellationToken token = this.GetCancellationTokenOnDestroy();
        List<GameObject> cutTargets = Scoreboard.gameObjects;

        foreach (var target in cutTargets)
        {
            target.SetActive(true);
            target.transform.position = _spawnPoint;
            target.transform.localScale *= 12;
            await UniTask.Delay(TimeSpan.FromSeconds(0.4f), cancellationToken: token);
        }
    }

    public void OnClickReturnTitle()
    {
        foreach(var target in Scoreboard.gameObjects)
        {
            Destroy(target);
        }
        Scoreboard.gameObjects.Clear();
        SceneManager.LoadScene("Title", LoadSceneMode.Single);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(_spawnPoint, 0.1f);
    }
}
