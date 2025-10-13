using UnityEngine;
using UnityEngine.AI;

public class BackUpMob : MonoBehaviour
{
    public enum MobState
    {
        Patrol,
        Run,
        Attack,
        Dead
    }
    public MobState state;

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

    void Start()
    {
        Enemy = GetComponent<NavMeshAgent>();
        Player = GameObject.FindGameObjectWithTag("Player");
        playerComponent = Player.GetComponent<PlayerHealth>();
        // material = GetComponent<MeshRenderer>().material;
        timer = timeUntilStop;
        currentSpeed = patrolSpeed;
        state = MobState.Patrol;
        animator = GetComponent<Animator>();
        Debug.Log("Animator = " + animator);
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
                Dead();
                break;
            default:
                break;
        }

        Debug.Log(state);
        currentAnimState = (int)state;
        animator.SetInteger("state", currentAnimState);
        Debug.Log("State changed to: " + currentAnimState);

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
                Enemy.isStopped = false;
                timer = timeUntilStop;
            }
            else
            {
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
            state = MobState.Attack;
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

    void Dead()
    {
        Enemy.isStopped = true;
        timeUntilDisappearance -= Time.deltaTime;
        if (timeUntilDisappearance <= 0f)
            Destroy(gameObject);
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
            health = 0f;
            state = MobState.Dead;
        }
    }

    public float GetHealth()
    {
        return health;
    }
}
