using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class SceneTeleporter : MonoBehaviour
{
    [Header("Сцена")]
    [SerializeField] private string sceneToLoad;

    [Header("Затемнение (можно без него)")]
    [SerializeField] private bool useFade = true;
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;

    void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(LoadScene());
        }
    }

    IEnumerator LoadScene()
    {
        // Затемнение
        if (useFade && fadeImage != null)
        {
            float elapsed = 0f;
            Color color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                fadeImage.color = color;
                yield return null;
            }
        }
        SceneManager.LoadScene(sceneToLoad);
    }
}