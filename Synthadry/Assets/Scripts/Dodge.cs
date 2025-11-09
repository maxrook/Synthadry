using UnityEngine;
using System.Collections;

public class PlayerDodge : MonoBehaviour
{
    [Header("Настройки обычного рывка")]
    [Tooltip("Длина обычного рывка")]
    public float dodgeDistance = 3.5f;
    [Tooltip("Горячая клавиша обычного рывка")]
    public KeyCode dodgeKey = KeyCode.Q;
    [Tooltip("Время рывка")]
    public float dodgeDuration = 0.2f;
    [Tooltip("Отсчёт до следующего рывка")]
    public float dodgeCooldown = 0.5f;
    [Tooltip("Стоимость обычного рывка в стамине")]
    public float dodgeStaminaCost = 30f;

    [Header("Настройки заряженного рывка")]
    [Tooltip("Горячая клавиша заряженного рывка")]
    public KeyCode chargedDodgeKey = KeyCode.E;
    [Tooltip("Максимальная длина заряженного рывка")]
    public float maxChargedDodgeDistance = 7f;
    [Tooltip("Множитель стоимости стамины заряженного рывка (чем больше дистанция, тем больше стамины)")]
    public float chargedDodgeStaminaMultiplier = 1.5f;
    [Tooltip("Замедление игрока во время зарядки рывка (0 - без замедления, 1 - полная остановка)")]
    [Range(0f, 1f)]
    public float chargeSlowdownFactor = 0.5f;
    [Tooltip("Скорость зарядки рывка")]
    public float chargeSpeed = 2f;

    [Header("Настройки стамины")]
    [Tooltip("Максимальное значение стамины")]
    public float maxStamina = 100f;
    [Tooltip("Скорость восстановления стамины (в секунду)")]
    public float staminaRegenerationRate = 40f;
    [Tooltip("Время до начала восстановления стамины после последнего рывка")]
    public float staminaRegenerationDelay = 2.5f;

    private float _currentStamina;
    private bool _isDodging = false;
    private bool _isChargingDodge = false;
    private float _dodgeCooldownTimer = 0f;
    private float _staminaRegenerationTimer = 0f;
    private float _chargedDodgeCharge = 0f; // Накопленная сила для заряженного рывка
    private CharacterController _characterController;
    private PlayerMovement _playerMovement; // Предполагается, что в PlayerMovement есть переменная speed
    private Vector3 _dodgeDirection;
    private float _originalSpeed; // Сохраняем оригинальную скорость игрока

    // Ссылка на UI элемент полоски стамины (если нужно)
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
        _originalSpeed = _playerMovement.moveSpeed; // Сохраняем оригинальную скорость
        UpdateStaminaUI();
    }

    void Update()
    {
        if (_dodgeCooldownTimer > 0)
        {
            _dodgeCooldownTimer -= Time.deltaTime;
        }

        // Обработка обычной атаки (нажатие кнопки)
        if (Input.GetKeyDown(dodgeKey) && !_isDodging && !_isChargingDodge && _dodgeCooldownTimer <= 0 && _currentStamina >= dodgeStaminaCost)
        {
            StartCoroutine(Dodge(dodgeDistance, dodgeStaminaCost));
        }

        // Обработка начала заряженного рывка (удержание кнопки)
        if (Input.GetKey(chargedDodgeKey) && !_isDodging && _currentStamina > 0)
        {
            StartChargingDodge();
        }

        // Обработка отпускания кнопки заряженного рывка
        if (Input.GetKeyUp(chargedDodgeKey) && _isChargingDodge)
        {
            ReleaseChargedDodge();
        }

        //Восстановление стамины
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

        UpdateStaminaUI(); //Обновление полоски стамины
    }

    // Обычный дэш
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

    // Начало зарядки дэша
    private void StartChargingDodge()
    {
        _isChargingDodge = true;
        _chargedDodgeCharge = 0f;
        _playerMovement.moveSpeed = _originalSpeed * (1 - chargeSlowdownFactor); // Замедление игрока
    }

    // Логика зарядки дэша
    void FixedUpdate()
    {
        if (_isChargingDodge)
        {
            //Постепенное увеличение заряда
            _chargedDodgeCharge = Mathf.Clamp01(_chargedDodgeCharge + chargeSpeed * Time.fixedDeltaTime);
            // Постепенное уменьшение стамины во время зарядки
            float staminaDrain = Mathf.Lerp(0f, maxStamina, _chargedDodgeCharge) * Time.fixedDeltaTime;
            _currentStamina -= staminaDrain;

            UpdateStaminaUI();

            if (_currentStamina <= 0)
            {
                //Если стамина кончилась во время зарядки - завершаем
                ReleaseChargedDodge();
            }
        }
    }

    // Отпускание кнопки заряженного дэша
    private void ReleaseChargedDodge()
    {
        _isChargingDodge = false;
        _playerMovement.moveSpeed = _originalSpeed; // Возвращаем оригинальную скорость
        _staminaRegenerationTimer = staminaRegenerationDelay;

        // Расчет дистанции и стоимости стамины на основе накопленного заряда
        float dodgeDistance = Mathf.Lerp(0, maxChargedDodgeDistance, _chargedDodgeCharge);
        float staminaCost = Mathf.Lerp(0, maxStamina * chargedDodgeStaminaMultiplier, _chargedDodgeCharge);

        // Гарантируем, что не потратим больше стамины, чем есть
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

    // Функция для обновления UI полоски стамины
    private void UpdateStaminaUI()
    {
        if (staminaSlider != null)
        {
            staminaSlider.value = _currentStamina / maxStamina;
        }
    }
}