using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    private PlayerHealth playerComponent;
    [SerializeField] private TMP_Text playerHealth;
    void Start()
    {
        GameObject Player = GameObject.FindGameObjectWithTag("Player");
        playerComponent = Player.GetComponent<PlayerHealth>();
    }

    void Update()
    {
        playerHealth.text = playerComponent.GetHealth().ToString();
    }
}
