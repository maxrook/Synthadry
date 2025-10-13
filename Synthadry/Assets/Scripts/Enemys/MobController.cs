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
    public MobState state;
    [SerializeField] private bool log = false;
    public Vector3[] waypoints = new Vector3[2];
    public int patrolSpeed = 5;
    public int runSpeed = 8;
    public float viewRadius = 10f;
    public float attackRadius = 2f;
    public float damage = 10f;
    public float health = 100f;
    public float attackInterval = 1f;
    public float stopDuration = 3f;
    public float timeUntilStop = 15f;
    public float timeUntilDisappearance = 10f;
    public float viewAngle = 80f;

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
        PauseManager.OnPauseManagerReady -= OnPauseReady; // Отписываемся
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

        // switch (state)
        // {
        //     case MobState.Patrol:
        //         material.color = Color.green;
        //         break;
        //     case MobState.Run:
        //         material.color = Color.magenta;
        //         break;
        //     case MobState.Attack:
        //         material.color = Color.red;
        //         break;
        //     case MobState.Die:
        //         material.color = Color.black;
        //         break;
        //     default:
        //         break;
        // }

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
        if (!isIdle)
        {
            animator.SetInteger("state", currentAnimState);
        }
        else
        {
            animator.SetInteger("state", 4);
        }

        if(log)Debug.Log("State changed to: " + currentAnimState);
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
    }
    [ContextMenu("die")]
    private void Die()
    {
        health = 0f;
        state = MobState.Dead;
        StateChanged.Invoke(state);

        Enemy.isStopped = true;
        StartCoroutine(Diyng(timeUntilDisappearance));
      
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
        Enemy.isStopped = isPaused; // Остановка SetDirection
        enabled = !isPaused; // Остановка Update, если пауза = true, то enabled должен быть равен = false
        if (isPaused)
            animator.speed = 0f;
        else
            animator.speed = 1f;
    }
}
