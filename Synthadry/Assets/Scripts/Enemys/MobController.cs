using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MobController : MonoBehaviour, IPauseHandler
{
    [Header("Конфиг моба")]
    [SerializeField] private MobSettingsSO _settings;

    [Header("Включить отладку (Debug.Log)")]
    [SerializeField] private bool _log = false;

    private float _playerHealth;
    private int _nextWaypoint = 0;

    private float _timer;
    private float _attackTimer = 0f;
    private float _distanceToPlayer;
    private float _angle;
    private float _currentSpeed;
    private Vector3 _targetPosition;
    private Vector3 _lastPlayerPosition = Vector3.zero;

    private float _health;

    private NavMeshAgent _enemy;
    private GameObject _player;
    private PlayerHealth _playerComponent;
    private Animator _animator;

    private bool _isIdle = false;
    private bool _isHurt = false;
    private bool _isDead = false;
    private Coroutine _hurtCoroutine;

    private const int AnimPatrol = 0;
    private const int AnimRun = 1;
    private const int AnimAttack = 2;
    private const int AnimIdle = 4;
    private const int AnimHurt = 5;

    private void Log(string msg)
    {
        if (_log)
            Debug.Log($"[MOB] {msg}");
    }

    private bool ValidateSettings()
    {
        if (_settings == null)
        {
            Debug.LogError("[MOB] MobSettingsSO не назначен", this);
            return false;
        }

        return true;
    }

    private Vector3 GetWaypointWorld(int index)
    {
        return _settings.Waypoints[index];
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

        if (_settings != null)
            _health = _settings.MaxHealth;
    }

    void Start()
    {
        if (!ValidateSettings())
        {
            enabled = false;
            return;
        }

        _enemy = GetComponent<NavMeshAgent>();

        _enemy.updateRotation = true;
        _enemy.angularSpeed = 1440f;
        _enemy.acceleration = 60f;
        _enemy.autoBraking = true;

        _player = GameObject.FindGameObjectWithTag("Player");
        if (_player != null)
            _playerComponent = _player.GetComponent<PlayerHealth>();

        _timer = _settings.TimeUntilStop;
        _currentSpeed = _settings.PatrolSpeed;
        _animator = GetComponent<Animator>();

        if (_health <= 0f)
            _health = _settings.MaxHealth;
    }

    private void OnPauseReady()
    {
        PauseManager.Instance.Register(this);
        PauseManager.OnPauseManagerReady -= OnPauseReady;
    }

    void OnDestroy()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.UnRegister(this);

        PauseManager.OnPauseManagerReady -= OnPauseReady;
    }

    void Update()
    {
        if (_isDead)
        {
            UpdateAnimation();
            return;
        }

        if (_player == null || _playerComponent == null)
        {
            UpdateAnimation();
            return;
        }

        Vector3 directionToPlayer = (_player.transform.position - transform.position).normalized;
        _angle = Vector3.Angle(transform.forward, directionToPlayer);

        _distanceToPlayer = Vector3.Distance(transform.position, _player.transform.position);
        _playerHealth = _playerComponent.GetHealth();

        if (_health <= 0f)
        {
            Die();
            return;
        }

        if (!_isHurt)
        {
            if (_playerHealth > 0f && _distanceToPlayer <= _settings.AttackRadius + 0.6f)
            {
                Attack();
            }
            else if (ShouldRun())
            {
                Run();
            }
            else
            {
                Patrol();
            }
        }

        _enemy.speed = _currentSpeed;

        UpdateAnimation();
    }

    private bool ShouldRun()
    {
        bool seesPlayer =
            _distanceToPlayer <= _settings.ViewRadius &&
            _angle <= _settings.ViewAngle * 0.5f &&
            _playerHealth > 0f &&
            CanSeePlayer();

        if (seesPlayer)
        {
            _lastPlayerPosition = _player.transform.position;
            return true;
        }

        if (_lastPlayerPosition != Vector3.zero)
            return true;

        return false;
    }

    bool CanSeePlayer()
    {
        Vector3 origin = transform.position;
        Vector3 direction = (_player.transform.position - origin).normalized;
        RaycastHit hit;

        if (Physics.Raycast(origin, direction, out hit, _settings.ViewRadius))
        {
            if (hit.transform.gameObject == _player || hit.transform.root.gameObject == _player)
                return true;
        }

        return false;
    }

    void Patrol()
    {
        _currentSpeed = _settings.PatrolSpeed;

        if (_lastPlayerPosition == Vector3.zero)
            _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            if (_enemy.isStopped)
            {
                _isIdle = false;
                _enemy.isStopped = false;
                _timer = _settings.TimeUntilStop;
            }
            else
            {
                _isIdle = true;
                _enemy.isStopped = true;
                _timer = _settings.StopDuration;
            }

            return;
        }

        if (Vector3.Distance(_enemy.transform.position, _targetPosition) <= 1f || !_enemy.hasPath)
        {
            _lastPlayerPosition = Vector3.zero;

            if (_settings.Waypoints == null || _settings.Waypoints.Length == 0)
            {
                _isIdle = true;
                _enemy.isStopped = true;
                return;
            }

            _targetPosition = GetWaypointWorld(_nextWaypoint);
            _nextWaypoint = _nextWaypoint + 1 >= _settings.Waypoints.Length ? 0 : _nextWaypoint + 1;

            _isIdle = false;
            _enemy.isStopped = false;
            _enemy.SetDestination(_targetPosition);
        }
    }

    void Run()
    {
        _currentSpeed = _settings.RunSpeed;
        _isIdle = false;
        _enemy.isStopped = false;

        bool canTrackPlayerNow =
            _distanceToPlayer <= _settings.ViewRadius &&
            _playerHealth > 0f;

        if (canTrackPlayerNow)
            _lastPlayerPosition = _player.transform.position;

        if (_distanceToPlayer <= _settings.AttackRadius && _playerHealth > 0f)
        {
            _enemy.ResetPath();
            Attack();
            return;
        }

        if (_lastPlayerPosition != Vector3.zero)
        {
            _enemy.SetDestination(_lastPlayerPosition);

            if (Vector3.Distance(_enemy.transform.position, _lastPlayerPosition) <= 1f)
            {
                _lastPlayerPosition = Vector3.zero;
                _currentSpeed = _settings.PatrolSpeed;
            }
        }
        else
        {
            _currentSpeed = _settings.PatrolSpeed;
            Patrol();
        }
    }

    void Attack()
    {
        _enemy.isStopped = true;
        _enemy.ResetPath();
        _isIdle = false;

        if (_playerHealth <= 0f)
        {
            _currentSpeed = _settings.PatrolSpeed;
            _lastPlayerPosition = Vector3.zero;
            return;
        }

        if (_distanceToPlayer > _settings.AttackRadius + 0.6f)
        {
            _enemy.isStopped = false;
            _currentSpeed = _settings.RunSpeed;
            Run();
            return;
        }

        _attackTimer -= Time.deltaTime;
        if (_attackTimer <= 0f)
        {
            _attackTimer = _settings.AttackInterval;
            _playerComponent.TakeDamage(_settings.Damage);
        }
    }
    private void UpdateAnimation()
    {
        if (_animator == null)
            return;

        if (_isDead)
        {
            _animator.SetInteger("state", AnimIdle);
        }
        else if (_isHurt)
        {
            _animator.SetInteger("state", AnimHurt);
        }
        else if (_playerHealth > 0f && _distanceToPlayer <= _settings.AttackRadius + 0.6f)
        {
            _animator.SetInteger("state", AnimAttack);
        }
        else if (ShouldRun())
        {
            _animator.SetInteger("state", AnimRun);
        }
        else if (_enemy != null && _enemy.isStopped)
        {
            _animator.SetInteger("state", AnimIdle);
        }
        else
        {
            _animator.SetInteger("state", AnimPatrol);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (_settings == null)
            return;

        Vector3 forward = transform.forward;
        Vector3 leftLimit = Quaternion.Euler(0, -_settings.ViewAngle / 2, 0) * forward;
        Vector3 rightLimit = Quaternion.Euler(0, _settings.ViewAngle / 2, 0) * forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + leftLimit * _settings.ViewRadius);
        Gizmos.DrawLine(transform.position, transform.position + rightLimit * _settings.ViewRadius);

        if (_player != null)
        {
            Vector3 origin = transform.position;
            Vector3 direction = (_player.transform.position - origin).normalized;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, origin + direction * _settings.ViewRadius);
        }

        if (_settings.Waypoints != null && _settings.Waypoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < _settings.Waypoints.Length; i++)
            {
                Vector3 wp = transform.parent != null
                    ? transform.parent.TransformPoint(_settings.Waypoints[i])
                    : transform.TransformPoint(_settings.Waypoints[i]);

                Gizmos.DrawSphere(wp, 0.2f);

                int next = i + 1 >= _settings.Waypoints.Length ? 0 : i + 1;
                Vector3 nextWp = transform.parent != null
                    ? transform.parent.TransformPoint(_settings.Waypoints[next])
                    : transform.TransformPoint(_settings.Waypoints[next]);

                if (_settings.Waypoints.Length > 1)
                    Gizmos.DrawLine(wp, nextWp);
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (_isDead) return;

        _health -= amount;
        if (_health <= 0f)
        {
            Die();
        }
        else
        {
            StartHurt();
        }
    }

    private void StartHurt()
    {
        if (_isDead) return;

        if (_hurtCoroutine != null)
            StopCoroutine(_hurtCoroutine);

        bool prevIdle = _isIdle;
        bool prevStopped = _enemy.isStopped;

        _isHurt = true;
        _isIdle = false;

        _enemy.isStopped = true;
        _enemy.ResetPath();

        _hurtCoroutine = StartCoroutine(HurtRoutine(prevIdle, prevStopped));
    }

    private IEnumerator HurtRoutine(bool prevIdle, bool prevStopped)
    {
        yield return new WaitForSeconds(_settings.HurtDuration);

        _isHurt = false;
        _hurtCoroutine = null;

        if (_isDead || _health <= 0f) yield break;

        _isIdle = prevIdle;
        _enemy.isStopped = prevStopped;
    }

    [ContextMenu("die")]
    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        if (_hurtCoroutine != null)
        {
            StopCoroutine(_hurtCoroutine);
            _hurtCoroutine = null;
        }

        _isHurt = false;
        _health = 0f;

        _enemy.isStopped = true;
        _enemy.ResetPath();

        _isIdle = true;

        if (_animator != null)
        {
            _animator.enabled = true;
            _animator.speed = 1f;
            _animator.SetInteger("state", AnimIdle);
            _animator.Update(0f);
        }

        StartCoroutine(DyingRoutine());
    }

    private IEnumerator DyingRoutine()
    {
        yield return new WaitForSeconds(_settings.TimeUntilDisappearance);
        Destroy(gameObject);
    }

    public float GetHealth()
    {
        return _health;
    }

    public void SetPaused(bool isPaused)
    {
        if (_enemy != null)
            _enemy.isStopped = isPaused;

        enabled = !isPaused;

        if (_animator != null)
            _animator.speed = isPaused ? 0f : 1f;
    }
}