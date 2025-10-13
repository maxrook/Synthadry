using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsButtons : MonoBehaviour
{
    public void BackToMainMenu()
    {
        ScenesManager.Instance.ChangeScene("MainMenu");
    }
    public void Unpause()
    {
        PauseManager.Instance.SetPaused(false);
    }
}
