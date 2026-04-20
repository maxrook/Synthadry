using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NPCTrigger : MonoBehaviour
{
    private bool playerIsNear = false;

    [Header("Íàñòðîéêè")]
    [SerializeField] private float lookAngleThreshold = 30f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.Log("Collider àâòîìàòè÷åñêè íàñòðîåí êàê Trigger");
        }
    }

    void Update()
    {
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
            Debug.Log("Èãðîê âîø¸ë â çîíó NPC");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = false;
            Debug.Log("Èãðîê âûøåë èç çîíû NPC");
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

    private void Interact()
    {
        Debug.Log($"NPC: Èãðîê â çîíå, ñìîòðèò íà ìåíÿ è íàæàë {interactionKey}!");

    }
}
