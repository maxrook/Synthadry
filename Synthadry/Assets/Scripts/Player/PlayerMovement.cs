using UnityEngine;

public class PlayerMovement : MonoBehaviour, IPauseHandler
{

    public Camera firstPersonCamera;

    [Header("Movement Settings")]
    public float moveSpeed = 8.0f;
    public float runSpeedMultiplier = 1.5f;
    public float gravity = 20.0f;
    public float jumpHeight = 5.0f;
    public float airControl = 0.3f;
    public float friction = 6.0f;
    public float stopSpeed = 100.0f;
    public float airMoveSpeedMultiplier = 0.8f;
    [Header("Rotation Settings")]
    public float sensitivityX = 2.0f;
    public float sensitivityY = 2.0f;
    public float minAngleY = -60.0f;
    public float maxAngleY = 60.0f;

    [Header("Falling Settings")]
    public float maxFallSpeed = 30.0f;
    public float earlyFallMultiplier = 2f;
    public float lateFallMultiplier = 1.2f;
    public float fallSpeedThreshold = 5f;

    private CharacterController _characterController;
    private float _rotationY = 0.0f;
    private Vector3 _moveDirection = Vector3.zero;
    private bool _isGrounded = false;
    private float _currentSpeed;
    private float _timeSinceLastGrounded;

    private Vector3 _lastMovementInput;

    private bool _isJumping = false;

    void Start()
    {
        _characterController = GetComponent<CharacterController>();

        if (_characterController == null)
        {
            Debug.LogError("CharacterController not found on this GameObject. Please add one.");
            enabled = false;
            return;
        }

        if (firstPersonCamera == null)
        {
            Debug.LogError("First-person camera not assigned.  Please assign the camera.");
            enabled = false;
            return;
        }
    }

    void Awake()
    {
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.Register(this);
        }
        else
        {
            PauseManager.OnPauseManagerReady += OnPauseReady;
        }
    }

    private void OnPauseReady()
    {
        PauseManager.Instance.Register(this);
        PauseManager.OnPauseManagerReady -= OnPauseReady; // Отписываемся
    }

    void OnDestroy()
    {
        PauseManager.Instance.UnRegister(this);
        PauseManager.OnPauseManagerReady -= OnPauseReady;
    }

    void Update()
    {
        if (Input.GetButtonDown("Pause"))
        {
            Pause();
        }

        HandleRotation();

        _isGrounded = _characterController.isGrounded;

        if (_isGrounded)
        {
            _timeSinceLastGrounded = 0;
            _isJumping = false;

            HandleGroundedMovement();

            if (Input.GetButtonDown("Jump"))
            {
                Jump();
            }
        }
        else
        {
            _timeSinceLastGrounded += Time.deltaTime;


            HandleAirMovement();

            HandleFalling();
        }

        float gravityMultiplier = 1f;

        if (_moveDirection.y < 0 && _timeSinceLastGrounded < 0.1f)
        {
            gravityMultiplier = earlyFallMultiplier;
        }
        else if (_moveDirection.y < -fallSpeedThreshold)
        {
            gravityMultiplier = lateFallMultiplier;
        }

        _moveDirection.y -= gravity * gravityMultiplier * Time.deltaTime;
        _moveDirection.y = Mathf.Max(_moveDirection.y, -maxFallSpeed);
        _characterController.Move(_moveDirection * Time.deltaTime);

        if (_isGrounded)
        {
            if (_lastMovementInput == Vector3.zero)
            {
                _moveDirection.x = 0;
                _moveDirection.z = 0;
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void HandleRotation()
    {
        float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;

        _rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
        _rotationY = Mathf.Clamp(_rotationY, minAngleY, maxAngleY);

        transform.localEulerAngles = new Vector3(0, rotationX, 0);
        firstPersonCamera.transform.localEulerAngles = new Vector3(-_rotationY, 0, 0);
    }
    void HandleGroundedMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.forward * z + transform.right * x;
        move.Normalize();

        _lastMovementInput = move;

        _currentSpeed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            _currentSpeed *= runSpeedMultiplier;
        }

        _moveDirection.x = move.x * _currentSpeed;
        _moveDirection.z = move.z * _currentSpeed;
    }

    void HandleAirMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 forward = firstPersonCamera.transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = firstPersonCamera.transform.right;
        right.y = 0;
        right.Normalize();


        Vector3 wishDir = (forward * z) + (right * x);

        if (wishDir.magnitude > 0)
        {
            wishDir.Normalize();
        }

        float wishSpeed = moveSpeed * airControl * airMoveSpeedMultiplier;

        _moveDirection.x = wishDir.x * wishSpeed;
        _moveDirection.z = wishDir.z * wishSpeed;
    }

    void HandleFalling()
    {
        //.
    }


    void Jump()
    {
        float currentHorizontalSpeed = new Vector2(_moveDirection.x, _moveDirection.z).magnitude;

        _moveDirection.y = Mathf.Sqrt(jumpHeight * 2f * gravity);
        Vector2 horizontalVelocity = new Vector2(_moveDirection.x, _moveDirection.z).normalized * Mathf.Min(currentHorizontalSpeed, _currentSpeed);
        _moveDirection.x = horizontalVelocity.x;
        _moveDirection.z = horizontalVelocity.y;

        _isJumping = true;
    }

    void Pause()
    {
        PauseManager.Instance.SetPaused(true);
    }

    public void SetPaused(bool isPaused)
    {
        enabled = !isPaused; // Остановка Update, если пауза = true, то enabled должен быть равен = false
    }

    void ApplyFriction()
    {
        float speed = _moveDirection.magnitude;
        if (speed != 0)
        {
            float drop = speed * friction * Time.deltaTime;
            float control = Mathf.Max(drop, stopSpeed * Time.deltaTime);
            float newSpeed = speed - control;
            if (newSpeed < 0)
                newSpeed = 0;
            newSpeed /= speed;
            _moveDirection.x *= newSpeed;
            _moveDirection.z *= newSpeed;
        }
    }
}

