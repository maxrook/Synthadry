using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Конфиг движения игрока")]
    public PlayerMovementConfig Config;

    [Header("Камера от первого лица")]
    public Camera FirstPersonCamera;

    public Vector3 CurrentVelocity => _moveVelocity + _dashVelocity;
    public float CurrentStamina => _currentStamina;
    public bool IsGrounded => _isGrounded;
    public bool IsDashing => _isDashing;

    private CharacterController _characterController;

    private Vector3 _moveVelocity;
    private Vector3 _dashVelocity;

    private float _rotationY;
    private float _currentStamina;

    private bool _isGrounded;
    private bool _wasGrounded;
    private bool _isDashing;

    private float _dashTimer;
    private float _dashCooldownTimer;
    private float _staminaRegenerationTimer;

    private void Start()
    {
        if (Config == null)
        {
            Debug.LogError("PlayerMovementConfig не назначен.");
            enabled = false;
            return;
        }

        _characterController = GetComponent<CharacterController>();

        if (FirstPersonCamera == null)
        {
            FirstPersonCamera = Camera.main;
        }

        if (FirstPersonCamera == null)
        {
            Debug.LogError("FirstPersonCamera не назначена.");
            enabled = false;
            return;
        }

        _currentStamina = Config.MaxStamina;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    }

    private void Update()
    {
        HandleLook();
        HandleTimers();
        HandleGroundCheck();
        HandleDashInput();
        HandleMovement();
        HandleJump();
        HandleGravity();
        HandleDashVelocity();

        Vector3 finalVelocity = _moveVelocity + _dashVelocity;
        _characterController.Move(finalVelocity * Time.deltaTime);

        SnapToGround();
        HandleCursorUnlock();
        RegenerateStamina();
    }

    private void HandleLook()
    {
        float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * Config.SensitivityX;

        _rotationY += Input.GetAxis("Mouse Y") * Config.SensitivityY;
        _rotationY = Mathf.Clamp(_rotationY, Config.MinAngleY, Config.MaxAngleY);

        transform.localEulerAngles = new Vector3(0.0f, rotationX, 0.0f);
        FirstPersonCamera.transform.localEulerAngles = new Vector3(-_rotationY, 0.0f, 0.0f);
    }

    private void HandleTimers()
    {
        if (_dashCooldownTimer > 0.0f)
        {
            _dashCooldownTimer -= Time.deltaTime;
        }

        if (_staminaRegenerationTimer > 0.0f)
        {
            _staminaRegenerationTimer -= Time.deltaTime;
        }
    }

    private void HandleGroundCheck()
    {
        _wasGrounded = _isGrounded;
        _isGrounded = _characterController.isGrounded;
    }

    private void HandleMovement()
    {
        Vector3 wishDirection = GetWishDirection();

        if (_isGrounded)
        {
            ApplyGroundFriction();

            float targetSpeed = Config.MoveSpeed;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                targetSpeed *= Config.RunSpeedMultiplier;
            }

            Accelerate(wishDirection, targetSpeed, Config.GroundAcceleration);
        }
        else
        {
            Accelerate(wishDirection, Config.AirMoveSpeed, Config.AirAcceleration);
            ApplyAirControl(wishDirection);
        }
    }

    private void HandleJump()
    {
        if (!_isGrounded)
        {
            return;
        }

        if (!Input.GetButtonDown("Jump"))
        {
            return;
        }

        _moveVelocity.y = Mathf.Sqrt(Config.JumpHeight * 2.0f * Config.Gravity);
        _isGrounded = false;
    }

    private void HandleGravity()
    {
        if (_isGrounded && _moveVelocity.y < 0.0f)
        {
            _moveVelocity.y = -2.0f;
            return;
        }

        _moveVelocity.y -= Config.Gravity * Time.deltaTime;
        _moveVelocity.y = Mathf.Max(_moveVelocity.y, -Config.MaxFallSpeed);
    }

    private void HandleDashInput()
    {
        if (!Input.GetKeyDown(Config.DashKey))
        {
            return;
        }

        if (_isDashing)
        {
            return;
        }

        if (_dashCooldownTimer > 0.0f)
        {
            return;
        }

        if (_currentStamina < Config.DashStaminaCost)
        {
            return;
        }

        if (!_isGrounded && !Config.AllowAirDash)
        {
            return;
        }

        StartDash();
    }

    private void StartDash()
    {
        Vector3 dashDirection = GetDashDirection();

        _isDashing = true;
        _dashTimer = Config.DashDuration;
        _dashCooldownTimer = Config.DashCooldown;

        _currentStamina -= Config.DashStaminaCost;
        _currentStamina = Mathf.Clamp(_currentStamina, 0.0f, Config.MaxStamina);

        _staminaRegenerationTimer = Config.StaminaRegenerationDelay;

        float dashSpeed = Config.DashDistance / Config.DashDuration;
        _dashVelocity = dashDirection * dashSpeed;
    }

    private void HandleDashVelocity()
    {
        if (!_isDashing)
        {
            _dashVelocity = Vector3.zero;
            return;
        }

        _dashTimer -= Time.deltaTime;

        if (_dashTimer <= 0.0f)
        {
            _isDashing = false;
            _dashVelocity = Vector3.zero;
        }
    }

    private Vector3 GetWishDirection()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        Vector3 wishDirection = transform.right * horizontalInput + transform.forward * verticalInput;
        wishDirection.y = 0.0f;

        if (wishDirection.sqrMagnitude > 1.0f)
        {
            wishDirection.Normalize();
        }

        return wishDirection;
    }

    private Vector3 GetDashDirection()
    {
        Vector3 dashDirection = FirstPersonCamera.transform.forward;
        dashDirection.y = 0.0f;

        if (dashDirection.sqrMagnitude <= 0.001f)
        {
            dashDirection = transform.forward;
        }

        dashDirection.Normalize();
        return dashDirection;
    }

    private void Accelerate(Vector3 wishDirection, float wishSpeed, float acceleration)
    {
        if (wishDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector3 horizontalVelocity = new Vector3(_moveVelocity.x, 0.0f, _moveVelocity.z);

        float currentSpeed = Vector3.Dot(horizontalVelocity, wishDirection);
        float addSpeed = wishSpeed - currentSpeed;

        if (addSpeed <= 0.0f)
        {
            return;
        }

        float accelerationSpeed = acceleration * wishSpeed * Time.deltaTime;

        if (accelerationSpeed > addSpeed)
        {
            accelerationSpeed = addSpeed;
        }

        _moveVelocity.x += wishDirection.x * accelerationSpeed;
        _moveVelocity.z += wishDirection.z * accelerationSpeed;
    }

    private void ApplyGroundFriction()
    {
        Vector3 horizontalVelocity = new Vector3(_moveVelocity.x, 0.0f, _moveVelocity.z);
        float speed = horizontalVelocity.magnitude;

        if (speed <= 0.001f)
        {
            return;
        }

        float control = Mathf.Max(speed, Config.StopSpeed);
        float drop = control * Config.GroundFriction * Time.deltaTime;
        float newSpeed = Mathf.Max(speed - drop, 0.0f);

        newSpeed /= speed;

        _moveVelocity.x *= newSpeed;
        _moveVelocity.z *= newSpeed;
    }

    private void ApplyAirControl(Vector3 wishDirection)
    {
        if (wishDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector3 horizontalVelocity = new Vector3(_moveVelocity.x, 0.0f, _moveVelocity.z);
        float speed = horizontalVelocity.magnitude;

        if (speed <= 0.001f)
        {
            return;
        }

        Vector3 normalizedVelocity = horizontalVelocity.normalized;
        float dot = Vector3.Dot(normalizedVelocity, wishDirection);

        if (dot <= 0.0f)
        {
            return;
        }

        float controlPower = Config.AirControl * dot * dot * Time.deltaTime;

        Vector3 controlledVelocity = Vector3.Lerp(normalizedVelocity, wishDirection, controlPower).normalized * speed;

        _moveVelocity.x = controlledVelocity.x;
        _moveVelocity.z = controlledVelocity.z;
    }

    private void SnapToGround()
    {
        if (_characterController.isGrounded && !_wasGrounded && _moveVelocity.y < 0.0f)
        {
            _moveVelocity.y = -2.0f;
        }
    }

    private void RegenerateStamina()
    {
        if (_staminaRegenerationTimer > 0.0f)
        {
            return;
        }

        if (_currentStamina >= Config.MaxStamina)
        {
            return;
        }

        _currentStamina += Config.StaminaRegenerationRate * Time.deltaTime;
        _currentStamina = Mathf.Clamp(_currentStamina, 0.0f, Config.MaxStamina);
    }

    private void HandleCursorUnlock()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}