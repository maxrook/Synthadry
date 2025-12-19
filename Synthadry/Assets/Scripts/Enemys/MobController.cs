using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MobController : MonoBehaviour, IPauseHandler
{
    public enum MobState
    {
        Patrol,
        Run,
        Attack,
        Dead
    }
    [Header("Текущее состояние моба")]
    public MobState state;

    [Header("Включить отладку (Debug.Log)")]
    [SerializeField] private bool log = false;

    [Header("Путь патрулирования (точки)")]
    public Vector3[] waypoints = new Vector3[2];

    [Header("Скорость патруля")]
    public int patrolSpeed = 5;

    [Header("Скорость бега за игроком")]
    public int runSpeed = 8;

    [Header("Радиус обзора")]
    public float viewRadius = 10f;

    [Header("Радиус атаки")]
    public float attackRadius = 2f;

    [Header("Урон моба за одну атаку")]
    public float damage = 10f;

    [Header("Здоровье моба")]
    public float health = 100f;

    [Header("Интервал между атаками (сек)")]
    public float attackInterval = 1f;

    [Header("Длительность остановки при патруле (сек)")]
    public float stopDuration = 3f;

    [Header("Время до остановки при патруле (сек)")]
    public float timeUntilStop = 15f;

    [Header("Время до исчезновения после смерти (сек)")]
    public float timeUntilDisappearance = 10f;

    [Header("Угол обзора (в градусах)")]
    public float viewAngle = 80f;

    [SerializeField] private float hurtDuration = 0.35f;

    private float playerHealth;
    private int nextWaypoint = 0;

    private float timer, attackTimer = 0f;
    private float distanceToPlayer;
    private float angle;
    private float currentSpeed;
    private Vector3 targetPosition, lastPlayerPosition = Vector3.zero;
    // private Material material;

    private NavMeshAgent Enemy;
    private GameObject Player;
    private PlayerHealth playerComponent;
    private Animator animator;
    private int currentAnimState = -1;

    private bool isIdle = false;
    public Action<MobState> StateChanged;

    private bool isHurt = false;
    private Coroutine hurtCoroutine;

    void Start()
    {
        Enemy = GetComponent<NavMeshAgent>();

        Enemy.updateRotation = true;
        Enemy.angularSpeed = 1440f;
        Enemy.acceleration = 60f;
        Enemy.autoBraking = true;

        Player = GameObject.FindGameObjectWithTag("Player");
        playerComponent = Player.GetComponent<PlayerHealth>();

        timer = timeUntilStop;
        currentSpeed = patrolSpeed;
        state = MobState.Patrol;
        animator = GetComponent<Animator>();
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
        PauseManager.OnPauseManagerReady -= OnPauseReady; // ������������
    }

    void OnDestroy()
    {
        PauseManager.Instance.UnRegister(this);
        PauseManager.OnPauseManagerReady -= OnPauseReady;
    }

    void Update()
    {
        Vector3 directionToPlayer = (Player.transform.position - transform.position).normalized;
        angle = Vector3.Angle(transform.forward, directionToPlayer);

        distanceToPlayer = Vector3.Distance(Enemy.transform.position, Player.transform.position);
        Enemy.speed = currentSpeed;
        playerHealth = playerComponent.GetHealth();

        if (health <= 0f)
            state = MobState.Dead;

        switch (state)
        {
            case MobState.Patrol:
                Patrol();
                break;
            case MobState.Run:
                Run();
                break;
            case MobState.Attack:
                Attack();
                break;
            case MobState.Dead:
                Die();
                break;
            default:
                break;
        }

        if (log) Debug.Log(state);
        currentAnimState = (int)state;

        if (isDead)
        {
            animator.SetInteger("state", 4);
        }
        else if (isHurt)
        {
            animator.SetInteger("state", 5);
        }
        else if (!isIdle)
        {
            animator.SetInteger("state", currentAnimState);
        }
        else
        {
            animator.SetInteger("state", 4);
        }

        if (log) Debug.Log("State changed to: " + currentAnimState);
        // RotateToMoveDirection();

    }

    bool CanSeePlayer()
    {
        Vector3 origin = transform.position;
        Vector3 direction = (Player.transform.position - origin).normalized;
        RaycastHit hit;


        if (Physics.Raycast(origin, direction, out hit, viewRadius))
        {
            if (hit.transform.gameObject == Player || hit.transform.root.gameObject == Player)
            {
                return true;
            }

        }
        return false;
    }

    void Patrol()
    {
        if (lastPlayerPosition == Vector3.zero)
            timer -= Time.deltaTime;


        if (distanceToPlayer <= viewRadius && angle <= viewAngle && playerHealth > 0f && CanSeePlayer())
        {
            Enemy.isStopped = false;
            state = MobState.Run;
        }
        else if (timer <= 0f)
        {
            if (Enemy.isStopped)
            {
                isIdle = false;
                Enemy.isStopped = false;
                timer = timeUntilStop;
            }
            else
            {
                isIdle = true;
                Enemy.isStopped = true;
                timer = stopDuration;
            }
        }
        else if (Vector3.Distance(Enemy.transform.position, targetPosition) <= 1f || !Enemy.hasPath)
        {
            lastPlayerPosition = Vector3.zero;
            currentSpeed = patrolSpeed;
            targetPosition = waypoints[nextWaypoint];
            nextWaypoint = nextWaypoint + 1 >= waypoints.Length ? 0 : nextWaypoint + 1;

            Enemy.SetDestination(targetPosition);
        }
    }

    void Run()
    {
        currentSpeed = runSpeed;

        if (distanceToPlayer > viewRadius)
            state = MobState.Patrol;
        else if (distanceToPlayer > attackRadius)
        {
            lastPlayerPosition = Player.transform.position;
            Enemy.SetDestination(lastPlayerPosition);
        }
        else
        {
            Enemy.ResetPath();
            state = MobState.Attack;
        }

    }

    void Attack()
    {
        if (playerHealth <= 0f)
        {
            state = MobState.Patrol;
        }
        else if (distanceToPlayer > attackRadius)
        {
            state = MobState.Run;
        }
        else
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                attackTimer = attackInterval;
                playerComponent.TakeDamage(damage);
            }
        }
    }
    void OnDrawGizmosSelected()
    {
        Vector3 forward = transform.forward;
        Vector3 leftLimit = Quaternion.Euler(0, -viewAngle / 2, 0) * forward;
        Vector3 rightLimit = Quaternion.Euler(0, viewAngle / 2, 0) * forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + leftLimit * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + rightLimit * viewRadius);

        if (Player != null)
        {
            Vector3 origin = transform.position;
            Vector3 direction = (Player.transform.position - origin).normalized;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, origin + direction * viewRadius);
        }
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f)
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
        if (isDead) return;

        if (hurtCoroutine != null)
            StopCoroutine(hurtCoroutine);

        bool prevIdle = isIdle;
        bool prevStopped = Enemy.isStopped;

        isHurt = true;
        isIdle = false;

        Enemy.isStopped = true;
        Enemy.ResetPath();

        hurtCoroutine = StartCoroutine(HurtRoutine(prevIdle, prevStopped));
    }

    private IEnumerator HurtRoutine(bool prevIdle, bool prevStopped)
    {
        yield return new WaitForSeconds(hurtDuration);

        isHurt = false;
        hurtCoroutine = null;

        if (isDead || health <= 0f) yield break;

        isIdle = prevIdle;
        Enemy.isStopped = prevStopped;
    }

    private bool isDead = false;

    [ContextMenu("die")]
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (hurtCoroutine != null)
        {
            StopCoroutine(hurtCoroutine);
            hurtCoroutine = null;
        }
        isHurt = false;

        health = 0f;
        state = MobState.Dead;

        Enemy.isStopped = true;
        Enemy.ResetPath();

        isIdle = true;
        animator.enabled = true;
        animator.speed = 1f;
        animator.SetInteger("state", 4);
        animator.Update(0f);


        StartCoroutine(DyingRoutine());
    }

    private IEnumerator DyingRoutine()
    {
        yield return new WaitForSeconds(timeUntilDisappearance);
        Destroy(gameObject);
    }

    private IEnumerator Diyng(float time)
    {
        yield return new WaitForSeconds(time);
        Destroy(gameObject);
    }

    public float GetHealth()
    {
        return health;
    }

    public void SetPaused(bool isPaused)
    {
        Enemy.isStopped = isPaused;
        enabled = !isPaused; 
        if (isPaused)
            animator.speed = 0f;
        else
            animator.speed = 1f;
    }
}
