using UnityEngine;

public class RavenProjectile : MonoBehaviour
{
    public bool debugLogs = true;
    void Log(string m){ if(debugLogs) Debug.Log($"[BOSS:RAVEN] {m}"); }

    private BossController owner;
    private Vector3 dir;
    private float speed, life, damage;
    private LayerMask playerMask;
    private string playerTag;
    private float t;

    public void Init(BossController owner, Vector3 dir, float speed, float life, float damage,
                     LayerMask playerMask, string playerTag)
    {
        this.owner = owner; this.dir = dir.normalized; this.speed = speed; this.life = life;
        this.damage = damage; this.playerMask = playerMask; this.playerTag = playerTag;
    }

    void Update()
    {
        t += Time.deltaTime;
        if (t > life) { Destroy(gameObject); return; }

        Vector3 next = transform.position + dir * speed * Time.deltaTime;

        Collider[] hits = Physics.OverlapSphere(next, 0.4f, playerMask);
        foreach (var h in hits)
        {
            if (h.CompareTag(playerTag))
            {
                owner.TryDamagePlayerGO(h.gameObject, damage);
                Destroy(gameObject);
                return;
            }
        }

        transform.position = next;
    }
}
