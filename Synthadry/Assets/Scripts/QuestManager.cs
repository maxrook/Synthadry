using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject questPanel;
    [SerializeField] private TMP_Text questTitleText;
    [SerializeField] private TMP_Text questListText;

    private List<Quest> activeQuests = new List<Quest>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        UpdateUI();
    }

    public void AddQuest(string questName, string description)
    {
        Quest newQuest = new Quest(questName, description);
        activeQuests.Add(newQuest);
        UpdateUI();
    }

    public void CompleteQuest(string questName)
    {
        Quest quest = activeQuests.Find(q => q.questName == questName);
        if (quest != null)
        {
            activeQuests.Remove(quest);
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        if (questTitleText != null)
            questTitleText.text = " весты";

        if (questListText != null)
        {
            questListText.text = "";
            foreach (Quest q in activeQuests)
            {
                questListText.text += "Х " + q.questName + "\n";
            }
        }
    }
}

[System.Serializable]
public class Quest
{
    public string questName;
    public string description;
    public bool isCompleted;

    public Quest(string name, string desc)
    {
        questName = name;
        description = desc;
        isCompleted = false;
    }
}