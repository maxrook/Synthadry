using System.Collections.Generic;
using UnityEngine;

public class GroundWave : BossPooledBehaviour
{
    public bool DebugLogs = false;
    void Log(string m) { if (DebugLogs) Debug.Log($"[BOSS:WAVE] {m}"); }

    private LineRenderer _lr;
    private const int SEGMENTS = 64;

    private BossController _owner;
    private Vector3 _origin;
    private float _moveSpeed, _maxRadius, _thickness, _height, _damage;
    private LayerMask _playerMask;
    private string _playerTag;

    private float _currentRadius;
    private bool _init;

    private float _footHitWindow = 0.20f;
    private readonly HashSet<int> _damagedTargets = new();

    private void Awake()
    {
        SetupLR();
    }

    public void Init(
        BossController owner,
        Vector3 origin,
        float moveSpeed,
        float maxRadius,
        float thickness,
        float height,
        float damage,
        LayerMask playerMask,
        string playerTag)
    {
        _owner = owner;
        _origin = origin;
        _moveSpeed = moveSpeed;
        _maxRadius = maxRadius;
        _thickness = thickness;
        _height = height;
        _damage = damage;
        _playerMask = playerMask;
        _playerTag = playerTag;

        _currentRadius = 0f;
        _damagedTargets.Clear();
        transform.position = origin;
        _init = true;

        SetupLR();
        Log($"Spawn (speed={moveSpeed}, maxR={maxRadius}, y={origin.y:0.00})");
    }

    private void SetupLR()
    {
        if (_lr != null)
            return;

        _lr = gameObject.GetComponent<LineRenderer>();
        if (_lr == null)
            _lr = gameObject.AddComponent<LineRenderer>();

        _lr.positionCount = SEGMENTS + 1;
        _lr.loop = true;
        _lr.useWorldSpace = true;
        _lr.widthMultiplier = 0.08f;
        _lr.material = new Material(Shader.Find("Sprites/Default"));
        _lr.startColor = _lr.endColor = new Color(0.15f, 0.8f, 1f, 0.9f);
    }

    void Update()
    {
        if (!_init)
            return;

        _currentRadius += _moveSpeed * Time.deltaTime;
        if (_currentRadius > _maxRadius)
        {
            ReturnToPool();
            return;
        }

        float y = _origin.y + 0.02f;
        for (int i = 0; i <= SEGMENTS; i++)
        {
            float a = i * Mathf.PI * 2f / SEGMENTS;
            Vector3 p = _origin + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * _currentRadius;
            _lr.SetPosition(i, new Vector3(p.x, y, p.z));
        }

        float inner = _currentRadius - _thickness * 0.5f;
        float outer = _currentRadius + _thickness * 0.5f;

        Collider[] cols = Physics.OverlapSphere(_origin, outer, _playerMask, QueryTriggerInteraction.Ignore);

        foreach (var c in cols)
        {
            if (c == null) continue;

            GameObject root = c.attachedRigidbody ? c.attachedRigidbody.gameObject : c.transform.root.gameObject;
            if (root == null) root = c.gameObject;
            if (!root.CompareTag(_playerTag) && !c.CompareTag(_playerTag)) continue;

            int id = root.GetInstanceID();
            if (_damagedTargets.Contains(id)) continue;

            Vector3 rootPosXZ = root.transform.position;
            rootPosXZ.y = _origin.y;
            float dist = Vector3.Distance(rootPosXZ, _origin);
            if (dist <= inner || dist >= outer) continue;

            float feetY = float.PositiveInfinity;
            var allCols = root.GetComponentsInChildren<Collider>();
            foreach (var pc in allCols)
            {
                if (pc == null || pc.isTrigger) continue;
                feetY = Mathf.Min(feetY, pc.bounds.min.y);
            }

            if (float.IsPositiveInfinity(feetY))
                feetY = root.transform.position.y;

            if (feetY > _origin.y + _footHitWindow)
                continue;

            _damagedTargets.Add(id);
            _owner.TryDamagePlayerGO(root, _damage);
            Log($"Hit player, r≈{_currentRadius:0.0}");
        }
    }

    public override void OnReturnedToPool()
    {
        _init = false;
        _currentRadius = 0f;
        _damagedTargets.Clear();
    }
}