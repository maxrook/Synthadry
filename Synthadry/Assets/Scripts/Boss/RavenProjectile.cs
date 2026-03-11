using UnityEngine;

public class RavenProjectile : MonoBehaviour
{
    public bool DebugLogs = false;
    void Log(string m) { if (DebugLogs) Debug.Log($"[BOSS:RAVEN] {m}"); }

    private BossController _owner;
    private Vector3 _dir;
    private float _speed, _life, _damage;
    private LayerMask _playerMask;
    private string _playerTag;
    private float _t;

    public void Init(BossController owner, Vector3 dir, float speed, float life, float damage,
                     LayerMask playerMask, string playerTag)
    {
        _owner = owner;
        _dir = dir.normalized;
        _speed = speed;
        _life = life;
        _damage = damage;
        _playerMask = playerMask;
        _playerTag = playerTag;
    }

    void Update()
    {
        _t += Time.deltaTime;
        if (_t > _life) { Destroy(gameObject); return; }

        Vector3 next = transform.position + _dir * _speed * Time.deltaTime;

        Collider[] hits = Physics.OverlapSphere(next, 0.4f, _playerMask);
        foreach (var h in hits)
        {
            if (h.CompareTag(_playerTag))
            {
                _owner.TryDamagePlayerGO(h.gameObject, _damage);
                Destroy(gameObject);
                return;
            }
        }

        transform.position = next;
    }
}