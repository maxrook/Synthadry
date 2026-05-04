using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NPCQuestGiver : MonoBehaviour
{
    private bool playerIsNear = false;

    [Header("Настройки квеста")]
    [SerializeField] private string questName = "Мой квест";
    [SerializeField] private string questDescription = "Описание";
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerIsNear = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerIsNear = false;
    }

    void Update()
    {
        if (playerIsNear && Input.GetKeyDown(interactKey))
        {
            GiveQuest();
        }
    }

    void GiveQuest()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.AddQuest(questName, questDescription);
            Debug.Log("Квест получен: " + questName);
        }
    }
}