using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeButton : MonoBehaviour
{
    [SerializeField] private string _sceneName;
    public void OnClick()
    {
        SceneManager.LoadScene(_sceneName, LoadSceneMode.Single);

    }
}
