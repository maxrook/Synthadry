using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class NPCDialogue : MonoBehaviour
{
    private bool playerIsNear = false;
    private bool isDialogActive = false;
    private int currentLineIndex = 0;
    private Coroutine typingCoroutine;

    [Header("Настройки взаимодействия")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private float lookAngleThreshold = 30f;
    [SerializeField] private float typingSpeed = 0.05f;

    [Header("Реплики NPC")]
    [TextArea(3, 10)]
    [SerializeField] private List<string> dialogLines = new List<string>();

    [Header("Твой UI")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TMP_Text dialogText;
    [SerializeField] private TMP_Text hintText;

    void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }

        if (dialogPanel != null)
            dialogPanel.SetActive(false);
    }

    void Update()
    {
        if (playerIsNear && !isDialogActive && IsPlayerLookingAtMe() && Input.GetKeyDown(interactionKey))
        {
            StartDialog();
        }
        else if (isDialogActive && Input.GetKeyDown(interactionKey))
        {
            ShowNextLine();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerIsNear = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = false;
            if (isDialogActive)
                EndDialog();
        }
    }

    bool IsPlayerLookingAtMe()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;

        Camera playerCamera = player.GetComponentInChildren<Camera>();
        if (playerCamera == null) return false;

        Vector3 directionToNPC = (transform.position - playerCamera.transform.position).normalized;
        Vector3 playerLookDirection = playerCamera.transform.forward;
        float angle = Vector3.Angle(playerLookDirection, directionToNPC);

        return angle < lookAngleThreshold;
    }

    void StartDialog()
    {
        if (dialogLines.Count == 0) return;

        isDialogActive = true;
        currentLineIndex = 0;
        dialogPanel.SetActive(true);
        UpdateHintText();
        ShowCurrentLine();
    }

    void ShowNextLine()
    {
        if (!isDialogActive) return;

        currentLineIndex++;

        if (currentLineIndex < dialogLines.Count)
        {
            UpdateHintText();
            ShowCurrentLine();
        }
        else
        {
            EndDialog();
        }
    }

    void UpdateHintText()
    {
        if (hintText != null)
        {
            if (currentLineIndex < dialogLines.Count - 1)
                hintText.text = $"Нажми {interactionKey} чтобы продолжить";
            else
                hintText.text = $"Нажми {interactionKey} чтобы закрыть";
        }
    }

    void ShowCurrentLine()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeLine(dialogLines[currentLineIndex]));
    }

    IEnumerator TypeLine(string line)
    {
        dialogText.text = "";
        foreach (char c in line)
        {
            dialogText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    void EndDialog()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        isDialogActive = false;
        dialogPanel.SetActive(false);
        dialogText.text = "";
    }
}