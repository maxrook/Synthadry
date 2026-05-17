using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeathManager : MonoBehaviour
{
    public static DeathManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Button restartButton;

    [Header("Настройки")]
    [SerializeField] private KeyCode restartKey = KeyCode.R;

    private bool isDead = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (deathPanel != null)
            deathPanel.SetActive(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartLevel);
    }

    void Update()
    {
        if (isDead && Input.GetKeyDown(restartKey))
        {
            RestartLevel();
        }
    }

    public void PlayerDied()
    {
        if (isDead) return;

        isDead = true;
        deathPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}