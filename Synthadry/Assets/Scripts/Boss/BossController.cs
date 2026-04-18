using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Collider))]
public class BossController : MonoBehaviour
{
    private enum BossAttackPattern
    {
        None,
        Slam,
        Raven,
        Slash,
        Charge,
        Totems
    }

    [Header("Конфиг босса")]
    [SerializeField] private BossConfigSO _config;

    [Header("Настройки атаки волнами")]
    [SerializeField] private SlamSettingsSO _slam;

    [Header("Настройки атаки воронами")]
    [SerializeField] private RavenSettingsSO _raven;

    [Header("Настройки слешей")]
    [SerializeField] private SlashSettingsSO _slash;

    [Header("Настройки рывка")]
    [SerializeField] private ChargeSettingsSO _charge;

    [Header("Настройки тотемов")]
    [SerializeField] private TotemSettingsSO _totems;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = false;

    private LayerMask _playerMask = 0;
    private string _playerTag = "Player";
    private bool _useManualRotation = true;
    private float _chaseTurnSpeed = 720f;
    private float _pivotTurnSpeed = 1440f;
    private float _chaseRepathInterval = 0.15f;
    private float _chaseSampleRadius = 2.0f;

    private float _windupBillboardSize = 1.2f;
    private float _windupBillboardHeight = 2.2f;
    private Color _windupBillboardColor = new Color(1f, 0.92f, 0.16f, 1f);
    private float _windupPulseMinAlpha = 0.25f;
    private float _windupPulseSpeed = 8f;

    private float _groundRayStartHeight = 1000f;
    private float _groundRayMaxDistance = 5000f;

    private const float _nearDistancePadding = 0.75f;
    private const float _chargeMaxDistanceFactor = 0.9f;
    private const float _chargeForwardDotThreshold = 0.45f;

    [SerializeField, Header("Текущее здоровье босса")]
    private float _currentHealth;

    private NavMeshAgent _agent;
    private Collider _bossCol;
    private BossObjectPool _objectPool;

    private bool _fightStarted;
    private bool _isAttacking;
    private bool _isStunned;
    private bool _isDead;

    private float _lastChargeTime;
    private readonly List<Totem> _activeTotems = new();
    private bool _totemsTriggeredOnce = false;

    private float _centerToBottomOffsetY;
    private float _nextPatternTime = 0f;
    private float _chaseRepathTimer = 0f;

    private GameObject _windupBillboardGO;
    private Material _windupBillboardMat;
    private bool _windupOn;
    private float _windupLocalTime;

    private Transform _player;
    private BossAttackPattern _lastUsedPattern = BossAttackPattern.None;

    void Log(string msg)
    {
        if (_debugLogs)
            Debug.Log($"[BOSS] {msg}");
    }

    void Awake()
    {
        _playerMask = LayerMask.GetMask("Default");
        _agent = GetComponent<NavMeshAgent>();
        _bossCol = GetComponent<Collider>();
        _objectPool = new BossObjectPool($"{name}_BossPool");

        if (_config != null)
            _currentHealth = _config.MaxHealth;

        WarmupPools();
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag(_playerTag);
        if (p) _player = p.transform;
        else Log("Игрок не найден по тегу.");

        _centerToBottomOffsetY = transform.position.y - _bossCol.bounds.min.y;

        if (NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas))
            _agent.Warp(hit.position);

        _agent.updatePosition = true;
        _agent.updateRotation = !_useManualRotation;
        _agent.autoBraking = false;
        _agent.angularSpeed = _useManualRotation ? 0f : 1200f;
        _agent.acceleration = 80f;
        _agent.stoppingDistance = 2f;
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        EnsureWindupIndicator();

        Log($"Старт. HP={_currentHealth}/{_config.MaxHealth}");
    }

    void Update()
    {
        UpdateWindupIndicator();

        if (_isDead)
            return;

        if (_player == null)
        {
            var p = GameObject.FindGameObjectWithTag(_playerTag);
            if (p) _player = p.transform;
        }

        TryStartFight();
        UpdateTotemRegen();
        CheckDeath();

        if (_isDead || !_fightStarted)
            return;

        if (!_isAttacking && !_isStunned)
            UpdateChase();

        if (!_isAttacking && !_isStunned && Time.time >= _nextPatternTime)
            StartCoroutine(ChooseAndExecutePattern());
    }

    private void WarmupPools()
    {
        _objectPool.Warmup(_slam.WavePrefab, Mathf.Max(1, _config.SlamSeriesCount));
        _objectPool.Warmup(_raven.ProjectilePrefab, Mathf.Max(1, _config.RavenBurst));
        _objectPool.Warmup(_slash.HitboxPrefab, Mathf.Max(1, _config.SlashCount));
        _objectPool.Warmup(_totems.Prefab, 3);
    }

    private void UpdateWindupIndicator()
    {
        if (!_windupOn || _windupBillboardGO == null)
            return;

        var cam = Camera.main;
        if (cam)
            _windupBillboardGO.transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);

        _windupLocalTime += Time.deltaTime * _windupPulseSpeed;
        float a = Mathf.Lerp(_windupPulseMinAlpha, 1f, 0.5f + 0.5f * Mathf.Sin(_windupLocalTime));

        if (_windupBillboardMat != null)
        {
            Color c = _windupBillboardColor;
            c.a = a;
            _windupBillboardMat.color = c;
        }

        Vector3 basePos = _bossCol.bounds.max + Vector3.up * (_windupBillboardHeight - (transform.position.y - _bossCol.bounds.max.y));
        _windupBillboardGO.transform.position = basePos;
        _windupBillboardGO.transform.localScale = Vector3.one * _windupBillboardSize;
    }

    private void TryStartFight()
    {
        if (_fightStarted || _player == null)
            return;

        if (Vector3.Distance(transform.position, _player.position) <= _config.AggroRadius)
        {
            _fightStarted = true;
            Log("Бой начался.");
        }
    }

    private void UpdateTotemRegen()
    {
        if (_activeTotems.Count > 0)
            _currentHealth = Mathf.Min(_config.MaxHealth, _currentHealth + _config.TotemRegenPerSec * Time.deltaTime);
    }

    private void CheckDeath()
    {
        if (_currentHealth > 0f)
            return;

        _currentHealth = 0f;
        StopAllCoroutines();

        _isDead = true;
        _isAttacking = false;
        _isStunned = false;

        if (_agent != null && _agent.enabled)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }

        EnableWindupIndicator(false);
        Log("Босс погиб.");
    }

    private void UpdateChase()
    {
        if (!_agent.enabled)
            _agent.enabled = true;

        _agent.isStopped = false;
        _agent.speed = _config.ChaseSpeed;

        _chaseRepathTimer -= Time.deltaTime;
        if (_chaseRepathTimer <= 0f)
        {
            _chaseRepathTimer = Mathf.Max(0.05f, _chaseRepathInterval);

            if (_player != null)
            {
                Vector3 goal = _player.position;
                if (NavMesh.SamplePosition(goal, out var near, _chaseSampleRadius, NavMesh.AllAreas))
                    _agent.SetDestination(near.position);
                else
                    _agent.SetDestination(goal);
            }
        }

        if (_useManualRotation && _player != null)
        {
            Vector3 dir = _player.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
            {
                float turnSpeed = _agent.velocity.sqrMagnitude < 0.01f ? _pivotTurnSpeed : _chaseTurnSpeed;
                Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
            }
        }
    }

    IEnumerator ChooseAndExecutePattern()
    {
        _isAttacking = true;

        if (!_totemsTriggeredOnce && _currentHealth <= _config.MaxHealth * _config.TotemTriggerHealthPercent)
        {
            Log("Фаза тотемов.");
            yield return SpawnTotemsPhase();
            _totemsTriggeredOnce = true;
            _lastUsedPattern = BossAttackPattern.Totems;
        }
        else
        {
            BossAttackPattern pattern = SelectNextPattern();

            switch (pattern)
            {
                case BossAttackPattern.Slash:
                    Log("Паттерн: SLASH SERIES");
                    yield return SlashSeries();
                    _lastUsedPattern = BossAttackPattern.Slash;
                    break;

                case BossAttackPattern.Charge:
                    Log("Паттерн: CHARGE");
                    yield return ChargeRam();
                    _lastChargeTime = Time.time;
                    _lastUsedPattern = BossAttackPattern.Charge;
                    break;

                case BossAttackPattern.Slam:
                    Log("Паттерн: SLAM WAVES");
                    yield return SlamSeries();
                    _lastUsedPattern = BossAttackPattern.Slam;
                    break;

                case BossAttackPattern.Raven:
                    Log("Паттерн: RAVEN STREAM");
                    yield return RavenStream();
                    _lastUsedPattern = BossAttackPattern.Raven;
                    break;

                default:
                    Log("Паттерн: SLAM WAVES");
                    yield return SlamSeries();
                    _lastUsedPattern = BossAttackPattern.Slam;
                    break;
            }
        }

        if (!_isDead)
        {
            float pause = Random.Range(_config.PatternPause.x, _config.PatternPause.y);
            _nextPatternTime = Time.time + pause;
        }

        _isAttacking = false;
    }

    private BossAttackPattern SelectNextPattern()
    {
        float distance = GetDistanceToPlayerXZ();
        float nearDistance = GetNearAttackDistance();
        float chargeMaxDistance = GetChargeMaxDistance();

        List<BossAttackPattern> candidates = new();

        if (distance <= nearDistance)
        {
            candidates.Add(BossAttackPattern.Slash);
            candidates.Add(BossAttackPattern.Slam);
            candidates.Add(BossAttackPattern.Raven);
        }
        else if (distance <= chargeMaxDistance)
        {
            if (CanUseCharge(distance))
                candidates.Add(BossAttackPattern.Charge);

            candidates.Add(BossAttackPattern.Slam);
            candidates.Add(BossAttackPattern.Raven);
            candidates.Add(BossAttackPattern.Slash);
        }
        else
        {
            candidates.Add(BossAttackPattern.Raven);
            candidates.Add(BossAttackPattern.Slam);
        }

        BossAttackPattern selected = SelectPatternAvoidRepeat(candidates);

        Log($"Выбор атаки. Dist={distance:0.00}, Near={nearDistance:0.00}, ChargeMax={chargeMaxDistance:0.00}, Last={_lastUsedPattern}, Next={selected}");
        return selected;
    }

    private BossAttackPattern SelectPatternAvoidRepeat(List<BossAttackPattern> candidates)
    {
        for (int i = 0; i < candidates.Count; i++)
        {
            var pattern = candidates[i];
            if (!IsPatternCurrentlyAvailable(pattern))
                continue;

            if (pattern == _lastUsedPattern)
                continue;

            return pattern;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            var pattern = candidates[i];
            if (IsPatternCurrentlyAvailable(pattern))
                return pattern;
        }

        return BossAttackPattern.Slam;
    }

    private bool IsPatternCurrentlyAvailable(BossAttackPattern pattern)
    {
        float distance = GetDistanceToPlayerXZ();

        switch (pattern)
        {
            case BossAttackPattern.Slash:
                return distance <= GetNearAttackDistance() + 0.5f;

            case BossAttackPattern.Charge:
                return CanUseCharge(distance);

            case BossAttackPattern.Slam:
                return true;

            case BossAttackPattern.Raven:
                return true;

            default:
                return false;
        }
    }

    private float GetDistanceToPlayerXZ()
    {
        if (_player == null)
            return float.MaxValue;

        Vector3 to = _player.position - transform.position;
        to.y = 0f;
        return to.magnitude;
    }

    private float GetNearAttackDistance()
    {
        return Mathf.Max(1f, _slash.Radius + _nearDistancePadding);
    }

    private float GetChargeMaxDistance()
    {
        return Mathf.Max(GetNearAttackDistance() + 0.5f, _charge.Speed * _charge.Duration * _chargeMaxDistanceFactor);
    }

    private bool CanUseCharge(float distance)
    {
        if (_player == null)
            return false;

        if (Time.time - _lastChargeTime <= _config.ChargeCooldown)
            return false;

        if (distance <= GetNearAttackDistance())
            return false;

        if (distance > GetChargeMaxDistance())
            return false;

        Vector3 toPlayer = _player.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude <= 0.001f)
            return false;

        float forwardDot = Vector3.Dot(transform.forward, toPlayer.normalized);
        return forwardDot >= _chargeForwardDotThreshold;
    }

    IEnumerator SlamSeries()
    {
        if (_agent.enabled)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }

        FaceTarget(_player ? _player.position : transform.position + transform.forward);

        for (int i = 0; i < _config.SlamSeriesCount; i++)
        {
            if (_isDead)
                yield break;

            yield return new WaitForSeconds(0.2f);

            Vector3 gpos = GetGroundPointUnderBoss();
            bool wide = i == _config.SlamSeriesCount - 1;

            _slam.Create(_objectPool, this, _debugLogs, _playerMask, _playerTag, gpos, wide);

            yield return new WaitForSeconds(_config.SlamInterval);
        }

        if (!_isDead && _agent.enabled)
            _agent.isStopped = false;
    }

    IEnumerator RavenStream()
    {
        if (_agent.enabled)
            _agent.isStopped = true;

        Vector3 dir = _player ? (_player.position - transform.position).normalized : transform.forward;
        FaceTarget(transform.position + dir);

        for (int i = 0; i < _config.RavenBurst; i++)
        {
            if (_isDead)
                yield break;

            float off = Random.Range(-_config.RavenSpreadAngle, _config.RavenSpreadAngle);
            Quaternion q = Quaternion.AngleAxis(off, Vector3.up);
            Vector3 shotDir = q * dir;

            _raven.Create(_objectPool, this, _debugLogs, _playerMask, _playerTag, transform.position + Vector3.up * 1.0f, shotDir);

            yield return new WaitForSeconds(_config.RavenRate);
        }

        if (!_isDead && _agent.enabled)
            _agent.isStopped = false;
    }

    IEnumerator SlashSeries()
    {
        if (_agent.enabled)
            _agent.isStopped = true;

        for (int i = 0; i < _config.SlashCount; i++)
        {
            if (_isDead)
                yield break;

            EnableWindupIndicator(true);

            float wind = 0f;
            while (wind < _config.SlashWindup)
            {
                if (_isDead)
                {
                    EnableWindupIndicator(false);
                    yield break;
                }

                if (_player)
                    FaceTarget(_player.position);

                wind += Time.deltaTime;
                yield return null;
            }

            EnableWindupIndicator(false);

            if (_player)
                FaceTarget(_player.position);

            Quaternion rot = transform.rotation;
            Vector3 spawnPos = GetGroundPointUnderBoss() + Vector3.up * 0.2f;

            _slash.Create(_objectPool, this, _debugLogs, _playerMask, _playerTag, spawnPos, rot);

            yield return new WaitForSeconds(_slash.Active + _config.SlashRecovery);
        }

        if (!_isDead && _agent.enabled)
            _agent.isStopped = false;
    }

    IEnumerator ChargeRam()
    {
        if (_isDead)
            yield break;

        if (_agent.enabled)
        {
            _agent.isStopped = true;
            _agent.updatePosition = false;
            _agent.updateRotation = false;
            _agent.enabled = false;
        }

        FaceTarget(_player ? _player.position : transform.position + transform.forward);
        yield return new WaitForSeconds(_charge.Windup);

        Vector3 toTarget = _player ? (_player.position - transform.position) : transform.forward;
        toTarget.y = 0f;
        Vector3 dir = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : transform.forward;

        var trail = GetComponent<TrailRenderer>();
        if (trail) trail.emitting = true;

        HashSet<int> damagedThisCharge = new HashSet<int>();

        float t = 0f;
        while (t < _charge.Duration)
        {
            if (_isDead)
                break;

            t += Time.deltaTime;

            Vector3 next = transform.position + dir * _charge.Speed * Time.deltaTime;

            Collider[] cols = Physics.OverlapSphere(next + Vector3.up * 1f, 1.2f, _playerMask, QueryTriggerInteraction.Ignore);
            foreach (var c in cols)
            {
                if (c == null) continue;

                GameObject root = c.attachedRigidbody ? c.attachedRigidbody.gameObject : c.transform.root.gameObject;
                if (root == null) root = c.gameObject;
                if (!root.CompareTag(_playerTag) && !c.CompareTag(_playerTag)) continue;

                int id = root.GetInstanceID();
                if (damagedThisCharge.Contains(id)) continue;

                damagedThisCharge.Add(id);
                TryDamagePlayerGO(root, _charge.Damage);
            }

            transform.position = next;
            FaceTarget(transform.position + dir);
            yield return null;
        }

        if (trail) trail.emitting = false;

        if (_isDead)
            yield break;

        _agent.enabled = true;
        _agent.updatePosition = true;
        _agent.updateRotation = !_useManualRotation;
        _agent.isStopped = false;
        _agent.speed = _config.ChaseSpeed;

        if (_player != null)
        {
            Vector3 goal = _player.position;
            if (NavMesh.SamplePosition(goal, out var near, _chaseSampleRadius, NavMesh.AllAreas))
                _agent.SetDestination(near.position);
            else
                _agent.SetDestination(goal);
        }
    }

    IEnumerator SpawnTotemsPhase()
    {
        _isStunned = true;

        if (_agent.enabled)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }

        _activeTotems.Clear();

        for (int i = 0; i < 3; i++)
        {
            float ang = i * 120f * Mathf.Deg2Rad;
            Vector3 ring = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * _config.TotemRingRadius;
            Vector3 targetXZ = transform.position + ring;

            Vector3 placed = targetXZ;
            if (TryRaycastGround(targetXZ, out Vector3 gp)) placed = gp;
            else if (NavMesh.SamplePosition(targetXZ, out var nh, 2f, NavMesh.AllAreas)) placed = nh.position;

            placed += Vector3.up * 0.01f;

            var totem = _totems.Create(_objectPool, this, _debugLogs, placed);
            _activeTotems.Add(totem);
            Log($"Тотем: {placed}");
        }

        yield return new WaitForSeconds(_config.TotemStunDurationOnSpawn);

        _isStunned = false;

        if (!_isDead && _agent.enabled)
            _agent.isStopped = false;
    }

    public void OnTotemDestroyed(Totem t)
    {
        if (_activeTotems.Contains(t))
            _activeTotems.Remove(t);

        Log("Тотем уничтожен.");
    }

    private void FaceTarget(Vector3 worldPos)
    {
        Vector3 dir = worldPos - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 1200f * Time.deltaTime);
        }
    }

    private bool TryRaycastGround(Vector3 xz, out Vector3 hitPoint)
    {
        Vector3 from = new Vector3(xz.x, _groundRayStartHeight, xz.z);
        RaycastHit[] hits = Physics.RaycastAll(from, Vector3.down, _groundRayMaxDistance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            if (h.collider == null) continue;

            Transform root = h.collider.transform.root;
            if (root == transform) continue;

            hitPoint = h.point;
            return true;
        }

        hitPoint = new Vector3(xz.x, transform.position.y - _centerToBottomOffsetY, xz.z);
        return false;
    }

    private Vector3 GetGroundPointUnderBoss()
    {
        Vector3 xz = new Vector3(transform.position.x, 0f, transform.position.z);
        if (TryRaycastGround(xz, out var p))
            return p + Vector3.up * 0.01f;

        return new Vector3(xz.x, transform.position.y - _centerToBottomOffsetY + 0.01f, xz.z);
    }

    void EnsureWindupIndicator()
    {
        if (_windupBillboardGO != null)
            return;

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null) unlit = Shader.Find("Unlit/Color");
        if (unlit == null) unlit = Shader.Find("Sprites/Default");

        _windupBillboardMat = new Material(unlit);
        _windupBillboardMat.color = _windupBillboardColor;
        _windupBillboardMat.renderQueue = 5000;

        _windupBillboardGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _windupBillboardGO.name = "SlashWindup_Billboard";
        _windupBillboardGO.transform.SetParent(null);

        var col = _windupBillboardGO.GetComponent<Collider>();
        if (col) Destroy(col);

        var mr = _windupBillboardGO.GetComponent<MeshRenderer>();
        if (mr) mr.sharedMaterial = _windupBillboardMat;

        _windupBillboardGO.SetActive(false);
    }

    void EnableWindupIndicator(bool on)
    {
        EnsureWindupIndicator();

        _windupOn = on;
        _windupLocalTime = 0f;
        _windupBillboardGO.SetActive(on);

        if (on)
        {
            Vector3 basePos = _bossCol.bounds.max + Vector3.up * _windupBillboardHeight;
            _windupBillboardGO.transform.position = basePos;
            _windupBillboardGO.transform.localScale = Vector3.one * _windupBillboardSize;

            var cam = Camera.main;
            if (cam)
                _windupBillboardGO.transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
        }
    }

    public void TakeDamage(float amount)
    {
        if (_isDead)
            return;

        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        Log($"Получен урон: -{amount} (HP {_currentHealth}/{_config.MaxHealth})");
    }

    public void TryDamageFoundPlayers(float dmg)
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, 2.0f, _playerMask);
        foreach (var c in cols)
            TryDamagePlayerGO(c.gameObject, dmg);
    }

    public void TryDamagePlayerGO(GameObject target, float dmg)
    {
        if (target == null)
            return;

        if (!string.IsNullOrEmpty(_playerTag) && !target.CompareTag(_playerTag))
            return;

        target.SendMessage("TakeDamage", dmg, SendMessageOptions.DontRequireReceiver);
    }

    private void OnDrawGizmosSelected()
    {
        if (_config == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _config.AggroRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _config.TotemRingRadius);
    }
}