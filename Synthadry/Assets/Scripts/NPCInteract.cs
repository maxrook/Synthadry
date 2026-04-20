using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NPCTrigger : MonoBehaviour
{
    private bool playerIsNear = false;

    [Header("Настройки")]
    [SerializeField] private float lookAngleThreshold = 30f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    void Start()
    {
        // Автоматически настраиваем коллайдер как триггер
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.Log("Collider автоматически настроен как Trigger");
        }
    }

    void Update()
    {
        // Проверяем: игрок в зоне И смотрит на NPC И нажал кнопку
        if (playerIsNear && IsPlayerLookingAtMe() && Input.GetKeyDown(interactionKey))
        {
            Interact();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = true;
            Debug.Log("Игрок вошёл в зону NPC");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = false;
            Debug.Log("Игрок вышел из зоны NPC");
        }
    }

    private bool IsPlayerLookingAtMe()
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

    // Действие при взаимодействии
    private void Interact()
    {
        Debug.Log($"NPC: Игрок в зоне, смотрит на меня и нажал {interactionKey}!");

    }
}