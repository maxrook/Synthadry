using UnityEngine;
using System.Collections;

public class PlayerDodge : MonoBehaviour
{
    [Header("Настройки рывка")]
    [Tooltip("Длина рывка")]
    public float dodgeDistance = 3.5f;
    [Tooltip("Горячая клавиша")]
    public KeyCode dodgeKey = KeyCode.Q;
    [Tooltip("Время рывка")]
    public float dodgeDuration = 0.2f;
    [Tooltip("Отсчёт до следующего рывка")]
    public float dodgeCooldown = 0.5f;
    private bool _isDodging = false;
    private float _dodgeCooldownTimer = 0f;
    private CharacterController _characterController;
    private PlayerMovement _playerMovement;
    private Vector3 _dodgeDirection;


    void Start()
    {
        _characterController = GetComponent<CharacterController>();
        if (_characterController == null)
        {
            Debug.LogError("CharacterController not found on this GameObject. Please add one.");
            enabled = false;
            return;
        }

        _playerMovement = GetComponent<PlayerMovement>();
        if (_playerMovement == null)
        {
            Debug.LogError("PlayerMovement script not found on this GameObject. Please add one.");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (_dodgeCooldownTimer > 0)
        {
            _dodgeCooldownTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(dodgeKey) && !_isDodging && _dodgeCooldownTimer <= 0)
        {
            StartCoroutine(Dodge());
        }
    }

    private IEnumerator Dodge()
    {
        _isDodging = true;
        _dodgeCooldownTimer = dodgeCooldown;
        _dodgeDirection = GetDodgeDirection();

        float timer = 0;
        while (timer < dodgeDuration)
        {
            _characterController.Move(_dodgeDirection * (dodgeDistance / dodgeDuration) * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        _isDodging = false;
    }

    private Vector3 GetDodgeDirection()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        Vector3 dodgeDir;

        if (horizontalInput != 0 || verticalInput != 0)
        {
            // Рывок при нажатых WASD
            dodgeDir = transform.forward * verticalInput + transform.right * horizontalInput;
            dodgeDir.Normalize();
        }
        else
        {
            // Рывок назад, если нету нажатых WASD
            dodgeDir = -transform.forward;
        }
        return dodgeDir;
    }
}