using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string GamePlaySceneName;
    public void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Play()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ScenesManager.Instance.ChangeScene(GamePlaySceneName);
    }

    public void Exit()
    {
        Application.Quit();
    }
}
