using UnityEngine;

public class RavenProjectile : BossPooledBehaviour
{
    public bool DebugLogs = false;
    void Log(string m) { if (DebugLogs) Debug.Log($"[BOSS:RAVEN] {m}"); }

    private BossController _owner;
    private Vector3 _dir;
    private float _speed, _life, _damage;
    private LayerMask _playerMask;
    private string _playerTag;
    private float _t;
    private bool _init;

    public void Init(
        BossController owner,
        Vector3 dir,
        float speed,
        float life,
        float damage,
        LayerMask playerMask,
        string playerTag)
    {
        _owner = owner;
        _dir = dir.normalized;
        _speed = speed;
        _life = life;
        _damage = damage;
        _playerMask = playerMask;
        _playerTag = playerTag;
        _t = 0f;
        _init = true;

        Log($"Spawn (speed={speed}, life={life}, damage={damage})");
    }

    void Update()
    {
        if (!_init)
            return;

        _t += Time.deltaTime;
        if (_t > _life)
        {
            ReturnToPool();
            return;
        }

        Vector3 next = transform.position + _dir * _speed * Time.deltaTime;

        Collider[] hits = Physics.OverlapSphere(next, 0.4f, _playerMask, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h == null) continue;

            GameObject root = h.attachedRigidbody ? h.attachedRigidbody.gameObject : h.transform.root.gameObject;
            if (root == null) root = h.gameObject;

            if (!root.CompareTag(_playerTag) && !h.CompareTag(_playerTag))
                continue;

            _owner.TryDamagePlayerGO(root, _damage);
            ReturnToPool();
            return;
        }

        transform.position = next;
    }

    public override void OnReturnedToPool()
    {
        _init = false;
        _t = 0f;
    }
}