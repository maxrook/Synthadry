using UnityEngine;
using System.Collections;

public class PlayerDodge : MonoBehaviour
{
    [Header("��������� �������� �����")]
    [Tooltip("����� �������� �����")]
    public float dodgeDistance = 3.5f;
    [Tooltip("������� ������� �������� �����")]
    public KeyCode dodgeKey = KeyCode.Q;
    [Tooltip("����� �����")]
    public float dodgeDuration = 0.2f;
    [Tooltip("������ �� ���������� �����")]
    public float dodgeCooldown = 0.5f;
    [Tooltip("��������� �������� ����� � �������")]
    public float dodgeStaminaCost = 30f;

    [Header("��������� ����������� �����")]
    [Tooltip("������� ������� ����������� �����")]
    public KeyCode chargedDodgeKey = KeyCode.E;
    [Tooltip("������������ ����� ����������� �����")]
    public float maxChargedDodgeDistance = 7f;
    [Tooltip("��������� ��������� ������� ����������� ����� (��� ������ ���������, ��� ������ �������)")]
    public float chargedDodgeStaminaMultiplier = 1.5f;
    [Tooltip("���������� ������ �� ����� ������� ����� (0 - ��� ����������, 1 - ������ ���������)")]
    [Range(0f, 1f)]
    public float chargeSlowdownFactor = 0.5f;
    [Tooltip("�������� ������� �����")]
    public float chargeSpeed = 2f;

    [Header("��������� �������")]
    [Tooltip("������������ �������� �������")]
    public float maxStamina = 100f;
    [Tooltip("�������� �������������� ������� (� �������)")]
    public float staminaRegenerationRate = 40f;
    [Tooltip("����� �� ������ �������������� ������� ����� ���������� �����")]
    public float staminaRegenerationDelay = 2.5f;

    private float _currentStamina;
    private bool _isDodging = false;
    private bool _isChargingDodge = false;
    private float _dodgeCooldownTimer = 0f;
    private float _staminaRegenerationTimer = 0f;
    private float _chargedDodgeCharge = 0f; // ����������� ���� ��� ����������� �����
    private CharacterController _characterController;
    private PlayerMovement _playerMovement; // ��������������, ��� � PlayerMovement ���� ���������� speed
    private Vector3 _dodgeDirection;
    private float _originalSpeed; // ��������� ������������ �������� ������

    // ������ �� UI ������� ������� ������� (���� �����)
    public UnityEngine.UI.Slider staminaSlider;


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

        _currentStamina = maxStamina;
        _originalSpeed = _playerMovement.moveSpeed; // ��������� ������������ ��������
        UpdateStaminaUI();
    }

    void Update()
    {
        if (_dodgeCooldownTimer > 0)
        {
            _dodgeCooldownTimer -= Time.deltaTime;
        }

        // ��������� ������� ����� (������� ������)
        if (Input.GetKeyDown(dodgeKey) && !_isDodging && !_isChargingDodge && _dodgeCooldownTimer <= 0 && _currentStamina >= dodgeStaminaCost)
        {
            StartCoroutine(Dodge(dodgeDistance, dodgeStaminaCost));
        }

        // ��������� ������ ����������� ����� (��������� ������)
        if (Input.GetKey(chargedDodgeKey) && !_isDodging && _currentStamina > 0)
        {
            StartChargingDodge();
        }

        // ��������� ���������� ������ ����������� �����
        if (Input.GetKeyUp(chargedDodgeKey) && _isChargingDodge)
        {
            ReleaseChargedDodge();
        }

        //�������������� �������
        if (_currentStamina < maxStamina)
        {
            if (_staminaRegenerationTimer > 0)
            {
                _staminaRegenerationTimer -= Time.deltaTime;
            }
            else
            {
                _currentStamina = Mathf.Min(_currentStamina + staminaRegenerationRate * Time.deltaTime, maxStamina);
                UpdateStaminaUI();
            }
        }

        UpdateStaminaUI(); //���������� ������� �������
    }

    // ������� ���
    private IEnumerator Dodge(float distance, float staminaCost)
    {
        _isDodging = true;
        _dodgeCooldownTimer = dodgeCooldown;
        _staminaRegenerationTimer = staminaRegenerationDelay;
        _dodgeDirection = GetDodgeDirection();
        _currentStamina -= staminaCost;
        UpdateStaminaUI();

        float timer = 0;
        while (timer < dodgeDuration)
        {
            _characterController.Move(_dodgeDirection * (distance / dodgeDuration) * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        _isDodging = false;
    }

    // ������ ������� ����
    private void StartChargingDodge()
    {
        _isChargingDodge = true;
        _chargedDodgeCharge = 0f;
        _playerMovement.moveSpeed = _originalSpeed * (1 - chargeSlowdownFactor); // ���������� ������
    }

    // ������ ������� ����
    void FixedUpdate()
    {
        if (_isChargingDodge)
        {
            //����������� ���������� ������
            _chargedDodgeCharge = Mathf.Clamp01(_chargedDodgeCharge + chargeSpeed * Time.fixedDeltaTime);
            // ����������� ���������� ������� �� ����� �������
            float staminaDrain = Mathf.Lerp(0f, maxStamina, _chargedDodgeCharge) * Time.fixedDeltaTime;
            _currentStamina -= staminaDrain;

            UpdateStaminaUI();

            if (_currentStamina <= 0)
            {
                //���� ������� ��������� �� ����� ������� - ���������
                ReleaseChargedDodge();
            }
        }
    }

    // ���������� ������ ����������� ����
    private void ReleaseChargedDodge()
    {
        _isChargingDodge = false;
        _playerMovement.moveSpeed = _originalSpeed; // ���������� ������������ ��������
        _staminaRegenerationTimer = staminaRegenerationDelay;

        // ������ ��������� � ��������� ������� �� ������ ������������ ������
        float dodgeDistance = Mathf.Lerp(0, maxChargedDodgeDistance, _chargedDodgeCharge);
        float staminaCost = Mathf.Lerp(0, maxStamina * chargedDodgeStaminaMultiplier, _chargedDodgeCharge);

        // �����������, ��� �� �������� ������ �������, ��� ����
        staminaCost = Mathf.Min(staminaCost, maxStamina);

        //Debug.Log("Charged dodge: Distance = " + dodgeDistance + ", Stamina Cost = " + staminaCost);

        StartCoroutine(Dodge(dodgeDistance, staminaCost));
    }


    private Vector3 GetDodgeDirection()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        Vector3 dodgeDir;

        if (horizontalInput != 0 || verticalInput != 0)
        {
            // ����� ��� ������� WASD
            dodgeDir = transform.forward * verticalInput + transform.right * horizontalInput;
            dodgeDir.Normalize();
        }
        else
        {
            // ����� �����, ���� ���� ������� WASD
            dodgeDir = -transform.forward;
        }
        return dodgeDir;
    }

    // ������� ��� ���������� UI ������� �������
    private void UpdateStaminaUI()
    {
        if (staminaSlider != null)
        {
            staminaSlider.value = _currentStamina / maxStamina;
        }
    }
}