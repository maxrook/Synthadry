using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenesManager : MonoBehaviour
{
    public static ScenesManager Instance { get; private set; }
    void Start()
    {
        Instance = this;
    }

    public void ChangeScene(string scene)
    {
        switch(scene)
        {
            case "MainMenu":
                HandleMainMenuScene();
                break;
            default:
                HandleLevelScene(scene);
                break;
        }
    }

    private void HandleMainMenuScene()
    {
        if (SceneManager.GetSceneByName("HUD").IsValid())
        {
            SceneManager.UnloadSceneAsync("HUD");
            SceneManager.UnloadSceneAsync("Pause");
        }
        SceneManager.LoadScene("MainMenu");
    }

    private void HandleLevelScene(string level)
    {
        SceneManager.LoadSceneAsync(level);
        if (!SceneManager.GetSceneByName("HUD").IsValid())
            SceneManager.LoadSceneAsync("HUD", LoadSceneMode.Additive);
        if (!SceneManager.GetSceneByName("Pause").IsValid())
            SceneManager.LoadSceneAsync("Pause", LoadSceneMode.Additive);
    }
}
