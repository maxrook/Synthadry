using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Collider))]
public class BossController : MonoBehaviour
{
    public enum BossState { Idle, Chasing, Attacking, Stunned, Dead }

    [System.Serializable]
    public class SlamSettings
    {
        public GameObject wavePrefab;
        public int seriesCount = 4;
        public float interval = 0.75f;
        public float damage = 20f;
        public float normalWaveSpeed = 9f;
        public float normalWaveMaxRadius = 15f;
        public float wideWaveSpeed = 10.5f;
        public float wideWaveMaxRadius = 19f;
    }

    [System.Serializable]
    public class RavenSettings
    {
        public GameObject projectilePrefab;
        public int burst = 12;
        public float rate = 0.09f;
        public float speed = 16f;
        public float life = 2.5f;
        public float damage = 10f;
        public float spreadAngle = 8f;
    }

    [System.Serializable]
    public class SlashSettings
    {
        public GameObject hitboxPrefab;
        public int count = 5;
        public float windup = 0.4f;
        public float active = 0.25f;
        public float recovery = 0.3f;
        public float damage = 25f;
        public float arcDegrees = 100f;
        public float radius = 4.5f;
        public float waitMax = 3.0f;
    }

    [System.Serializable]
    public class ChargeSettings
    {
        public float windup = 0.5f;
        public float speed = 18f;
        public float duration = 0.7f;
        public float cooldown = 2.0f;
        public float damage = 30f;
    }

    [System.Serializable]
    public class TotemSettings
    {
        public GameObject prefab;
        public float regenPerSec = 8f;
        public float phaseDuration = 12f;
        public float ringRadius = 8f;
        public float stunDurationOnSpawn = 1.0f;
    }

    [HideInInspector]
    public class WindupSettings
    {
        public float billboardSize = 1.2f;
        public float billboardHeight = 2.2f;
        public Color billboardColor = new Color(1f, 0.92f, 0.16f, 1f);
        [Range(0f,1f)] public float pulseMinAlpha = 0.25f;
        public float pulseSpeed = 8f;
    }

    [HideInInspector]
    public class GroundSettings
    {
        public float rayStartHeight = 1000f;
        public float rayMaxDistance = 5000f;
    }

    public bool debugLogs = true;
    void Log(string msg) { if (debugLogs) Debug.Log($"[BOSS] {msg}"); }

    public LayerMask playerMask = ~0;
    public string playerTag = "Player";

    public float maxHealth = 1500f;
    [SerializeField] private float currentHealth;
    public float aggroRadius = 18f;

    public float chaseSpeed = 4.5f;
    public float attackMoveSpeed = 3.5f;
    public bool useManualRotation = true;
    public float chaseTurnSpeed = 720f;
    public float pivotTurnSpeed = 1440f;
    public float chaseRepathInterval = 0.15f;
    public float chaseSampleRadius = 2.0f;

    public Vector2 patternPause = new Vector2(1.0f, 1.6f);

    public SlamSettings slam = new SlamSettings();
    public RavenSettings raven = new RavenSettings();
    public SlashSettings slash = new SlashSettings();
    public ChargeSettings charge = new ChargeSettings();
    public TotemSettings totems = new TotemSettings();
    public WindupSettings windup = new WindupSettings();
    public GroundSettings ground = new GroundSettings();

    private NavMeshAgent agent;
    private Collider bossCol;
    private BossState state = BossState.Idle;
    private bool fightStarted;
    private bool isBusy;
    private float lastChargeTime;
    private readonly List<Totem> activeTotems = new();
    private bool totemsTriggeredOnce = false;

    private float centerToBottomOffsetY;
    private float nextPatternTime = 0f;
    private float chaseRepathTimer = 0f;

    private GameObject windupBillboardGO;
    private Material windupBillboardMat;
    private bool windupOn;
    private float windupLocalTime;

    private Transform player;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        bossCol = GetComponent<Collider>();
        currentHealth = maxHealth;
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;
        else Log("Игрок не найден по тегу.");

        centerToBottomOffsetY = transform.position.y - bossCol.bounds.min.y;

        if (NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas))
            agent.Warp(hit.position);

        agent.updatePosition = true;
        agent.updateRotation = !useManualRotation;
        agent.autoBraking = false;
        agent.angularSpeed = useManualRotation ? 0f : 1200f;
        agent.acceleration = 80f;
        agent.stoppingDistance = 2f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        EnsureWindupIndicator();

        Log($"Старт. HP={currentHealth}/{maxHealth}");
    }

    void Update()
    {
        if (windupOn && windupBillboardGO != null)
        {
            var cam = Camera.main;
            if (cam)
                windupBillboardGO.transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);

            windupLocalTime += Time.deltaTime * windup.pulseSpeed;
            float a = Mathf.Lerp(windup.pulseMinAlpha, 1f, 0.5f + 0.5f * Mathf.Sin(windupLocalTime));
            if (windupBillboardMat != null)
            {
                Color c = windup.billboardColor; c.a = a;
                windupBillboardMat.color = c;
            }

            Vector3 basePos = bossCol.bounds.max + Vector3.up * (windup.billboardHeight - (transform.position.y - bossCol.bounds.max.y));
            windupBillboardGO.transform.position = basePos;
            windupBillboardGO.transform.localScale = Vector3.one * windup.billboardSize;
        }

        if (state == BossState.Dead) return;

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p) player = p.transform;
        }

        if (!fightStarted && player != null &&
            Vector3.Distance(transform.position, player.position) <= aggroRadius)
        {
            fightStarted = true;
            state = BossState.Chasing;
            Log("Бой начался.");
        }

        if (activeTotems.Count > 0)
            currentHealth = Mathf.Min(maxHealth, currentHealth + totems.regenPerSec * Time.deltaTime);

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            StopAllCoroutines();
            state = BossState.Dead;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            EnableWindupIndicator(false);
            Log("Босс погиб.");
            return;
        }

        if (!fightStarted) return;

        if (!isBusy && state != BossState.Stunned)
        {
            state = BossState.Chasing;
            if (!agent.enabled) agent.enabled = true;
            agent.isStopped = false;
            agent.speed = chaseSpeed;

            chaseRepathTimer -= Time.deltaTime;
            if (chaseRepathTimer <= 0f)
            {
                chaseRepathTimer = Mathf.Max(0.05f, chaseRepathInterval);
                if (player != null)
                {
                    Vector3 goal = player.position;
                    if (NavMesh.SamplePosition(goal, out var near, chaseSampleRadius, NavMesh.AllAreas))
                        agent.SetDestination(near.position);
                    else
                        agent.SetDestination(goal);
                }
            }

            if (useManualRotation && player != null)
            {
                Vector3 dir = player.position - transform.position; dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                {
                    float ts = (agent.velocity.sqrMagnitude < 0.01f) ? pivotTurnSpeed : chaseTurnSpeed;
                    Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, look, ts * Time.deltaTime);
                }
            }
        }

        if (!isBusy && state != BossState.Stunned && state != BossState.Dead && Time.time >= nextPatternTime)
            StartCoroutine(ChooseAndExecutePattern());
    }

    IEnumerator ChooseAndExecutePattern()
    {
        isBusy = true;

        if (!totemsTriggeredOnce && currentHealth <= maxHealth * 0.25f)
        {
            Log("Фаза тотемов.");
            yield return SpawnTotemsPhase();
            totemsTriggeredOnce = true;
        }
        else
        {
            int choice = Random.Range(0, 3);
            if      (choice == 0) { Log("Паттерн: SLAM WAVES");   yield return SlamSeries(); }
            else if (choice == 1) { Log("Паттерн: RAVEN STREAM"); yield return RavenStream(); }
            else                  { Log("Паттерн: SLASH SERIES"); yield return SlashSeries(); }

            if (Time.time - lastChargeTime > charge.cooldown && Random.value < 0.6f)
            {
                Log("Паттерн: CHARGE");
                yield return ChargeRam();
                lastChargeTime = Time.time;
            }
        }

        float pause = Random.Range(patternPause.x, patternPause.y);
        nextPatternTime = Time.time + pause;
        isBusy = false;
    }

    IEnumerator SlamSeries()
    {
        state = BossState.Attacking;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        FaceTarget(player ? player.position : transform.position + transform.forward);

        for (int i = 0; i < slam.seriesCount; i++)
        {
            yield return new WaitForSeconds(0.2f);

            Vector3 gpos = GetGroundPointUnderBoss();
            bool wide = (i == slam.seriesCount - 1);

            var go = Instantiate(slam.wavePrefab, gpos, Quaternion.identity);
            var wave = go.GetComponent<GroundWave>();
            wave.debugLogs = debugLogs;
            wave.Init(
                owner: this,
                origin: gpos,
                moveSpeed: wide ? slam.wideWaveSpeed : slam.normalWaveSpeed,
                maxRadius: wide ? slam.wideWaveMaxRadius : slam.normalWaveMaxRadius,
                thickness: 1.2f,
                height: 0.5f,
                damage: slam.damage,
                playerMask: playerMask,
                playerTag: playerTag
            );

            yield return new WaitForSeconds(slam.interval);
        }

        state = BossState.Chasing;
        agent.isStopped = false;
    }

    IEnumerator RavenStream()
    {
        state = BossState.Attacking;
        agent.isStopped = true;

        Vector3 dir = player ? (player.position - transform.position).normalized : transform.forward;
        FaceTarget(transform.position + dir);

        for (int i = 0; i < raven.burst; i++)
        {
            float off = Random.Range(-raven.spreadAngle, raven.spreadAngle);
            Quaternion q = Quaternion.AngleAxis(off, Vector3.up);
            Vector3 shotDir = q * dir;

            var go = Instantiate(raven.projectilePrefab, transform.position + Vector3.up * 1.0f, Quaternion.LookRotation(shotDir, Vector3.up));
            var rp = go.GetComponent<RavenProjectile>();
            rp.debugLogs = debugLogs;
            rp.Init(this, shotDir, raven.speed, raven.life, raven.damage, playerMask, playerTag);

            yield return new WaitForSeconds(raven.rate);
        }

        state = BossState.Chasing;
        agent.isStopped = false;
    }

    IEnumerator SlashSeries()
    {
        state = BossState.Attacking;
        agent.isStopped = true;

        for (int i = 0; i < slash.count; i++)
        {
            EnableWindupIndicator(true);

            float waited = 0f;
            while (player && PlanarDistance(transform.position, player.position) > slash.radius)
            {
                if (slash.waitMax > 0f)
                {
                    waited += Time.deltaTime;
                    if (waited >= slash.waitMax)
                    {
                        EnableWindupIndicator(false);
                        state = BossState.Chasing;
                        agent.isStopped = false;
                        yield break;
                    }
                }
                FaceTarget(player.position);
                yield return null;
            }


            float wind = 0f;
            while (wind < slash.windup)
            {
                if (player) FaceTarget(player.position);
                wind += Time.deltaTime;
                yield return null;
            }

            EnableWindupIndicator(false);

            if (player && PlanarDistance(transform.position, player.position) > slash.radius)
            {
                continue;
            }

            Quaternion rot = transform.rotation;
            Vector3 spawnPos = GetGroundPointUnderBoss() + Vector3.up * 0.02f;
            var hb = Instantiate(slash.hitboxPrefab, spawnPos, rot);
            var hitbox = hb.GetComponent<SlashHitbox>();
            hitbox.debugLogs = debugLogs;
            hitbox.arcDegrees = slash.arcDegrees;
            hitbox.radius = slash.radius;
            hitbox.Init(this, slash.active, slash.damage, playerMask, playerTag);

            yield return new WaitForSeconds(slash.active + slash.recovery);
        }

        state = BossState.Chasing;
        agent.isStopped = false;
    }

    IEnumerator ChargeRam()
    {
        state = BossState.Attacking;

        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.enabled = false;

        FaceTarget(player ? player.position : transform.position + transform.forward);
        yield return new WaitForSeconds(charge.windup);

        Vector3 toTarget = player ? (player.position - transform.position) : transform.forward;
        toTarget.y = 0f;
        Vector3 dir = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : transform.forward;

        var trail = GetComponent<TrailRenderer>();
        if (trail) trail.emitting = true;

        HashSet<int> damagedThisCharge = new HashSet<int>();

        float t = 0f;
        while (t < charge.duration)
        {
            t += Time.deltaTime;

            Vector3 next = transform.position + dir * charge.speed * Time.deltaTime;

            Collider[] cols = Physics.OverlapSphere(next + Vector3.up * 1f, 1.2f, playerMask, QueryTriggerInteraction.Ignore);
            foreach (var c in cols)
            {
                if (c == null) continue;
                GameObject root = c.attachedRigidbody ? c.attachedRigidbody.gameObject : c.transform.root.gameObject;
                if (root == null) root = c.gameObject;
                if (!root.CompareTag(playerTag) && !c.CompareTag(playerTag)) continue;

                int id = root.GetInstanceID();
                if (damagedThisCharge.Contains(id)) continue;

                damagedThisCharge.Add(id);
                TryDamagePlayerGO(root, charge.damage);
            }

            transform.position = next;
            FaceTarget(transform.position + dir);
            yield return null;
        }

        if (trail) trail.emitting = false;

        agent.enabled = true;
        agent.updatePosition = true;
        agent.updateRotation = !useManualRotation;
        state = BossState.Chasing;
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        if (player != null)
        {
            Vector3 goal = player.position;
            if (NavMesh.SamplePosition(goal, out var near, chaseSampleRadius, NavMesh.AllAreas))
                agent.SetDestination(near.position);
            else
                agent.SetDestination(goal);
        }
    }

    IEnumerator SpawnTotemsPhase()
    {
        state = BossState.Stunned;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        activeTotems.Clear();

        for (int i = 0; i < 3; i++)
        {
            float ang = i * 120f * Mathf.Deg2Rad;
            Vector3 ring = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * totems.ringRadius;
            Vector3 targetXZ = transform.position + ring;

            Vector3 placed = targetXZ;
            if (TryRaycastGround(targetXZ, out Vector3 gp)) placed = gp;
            else if (NavMesh.SamplePosition(targetXZ, out var nh, 2f, NavMesh.AllAreas)) placed = nh.position;
            placed += Vector3.up * 0.01f;

            var go = Instantiate(totems.prefab, placed, Quaternion.identity);
            var totem = go.GetComponent<Totem>();
            totem.debugLogs = debugLogs;
            totem.Init(this);

            activeTotems.Add(totem);
            Log($"Тотем: {placed}");
        }

        yield return new WaitForSeconds(totems.stunDurationOnSpawn);

        state = BossState.Chasing;
        agent.isStopped = false;
    }

    public void OnTotemDestroyed(Totem t)
    {
        if (activeTotems.Contains(t)) activeTotems.Remove(t);
        Log("Тотем уничтожен.");
    }

    private void FaceTarget(Vector3 worldPos)
    {
        Vector3 dir = (worldPos - transform.position); dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 1200f * Time.deltaTime);
        }
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f; return Vector3.Distance(a, b);
    }

    private bool TryRaycastGround(Vector3 xz, out Vector3 hitPoint)
    {
        Vector3 from = new Vector3(xz.x, ground.rayStartHeight, xz.z);
        RaycastHit[] hits = Physics.RaycastAll(from, Vector3.down, ground.rayMaxDistance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            Transform root = h.collider.transform.root;
            if (root == transform) continue;
            hitPoint = h.point;
            return true;
        }

        hitPoint = new Vector3(xz.x, transform.position.y - centerToBottomOffsetY, xz.z);
        return false;
    }

    private Vector3 GetGroundPointUnderBoss()
    {
        Vector3 xz = new Vector3(transform.position.x, 0f, transform.position.z);
        if (TryRaycastGround(xz, out var p)) return p + Vector3.up * 0.01f;
        return new Vector3(xz.x, transform.position.y - centerToBottomOffsetY + 0.01f, xz.z);
    }

    void EnsureWindupIndicator()
    {
        if (windupBillboardGO != null) return;

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null) unlit = Shader.Find("Unlit/Color");
        if (unlit == null) unlit = Shader.Find("Sprites/Default");

        windupBillboardMat = new Material(unlit);
        windupBillboardMat.color = windup.billboardColor;
        windupBillboardMat.renderQueue = 5000;

        windupBillboardGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
        windupBillboardGO.name = "SlashWindup_Billboard";
        windupBillboardGO.transform.SetParent(null);
        var col = windupBillboardGO.GetComponent<Collider>();
        if (col) Destroy(col);

        var mr = windupBillboardGO.GetComponent<MeshRenderer>();
        if (mr) mr.sharedMaterial = windupBillboardMat;

        windupBillboardGO.SetActive(false);
    }

    void EnableWindupIndicator(bool on)
    {
        EnsureWindupIndicator();
        windupOn = on;
        windupLocalTime = 0f;
        windupBillboardGO.SetActive(on);

        if (on)
        {
            Vector3 basePos = bossCol.bounds.max + Vector3.up * windup.billboardHeight;
            windupBillboardGO.transform.position = basePos;
            windupBillboardGO.transform.localScale = Vector3.one * windup.billboardSize;

            var cam = Camera.main;
            if (cam)
                windupBillboardGO.transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
        }
    }

    public void TakeDamage(float amount)
    {
        if (state == BossState.Dead) return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        Log($"Получен урон: -{amount} (HP {currentHealth}/{maxHealth})");
    }

    public void TryDamageFoundPlayers(float dmg)
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, 2.0f, playerMask);
        foreach (var c in cols) TryDamagePlayerGO(c.gameObject, dmg);
    }

    public void TryDamagePlayerGO(GameObject target, float dmg)
    {
        if (target == null) return;
        if (!string.IsNullOrEmpty(playerTag) && !target.CompareTag(playerTag)) return;
        target.SendMessage("TakeDamage", dmg, SendMessageOptions.DontRequireReceiver);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, aggroRadius);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, totems.ringRadius);
    }
}
