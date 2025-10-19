using UnityEngine;

public class Totem : MonoBehaviour
{
    public bool debugLogs = true;
    void Log(string m){ if(debugLogs) Debug.Log($"[BOSS:TOTEM] {m}"); }

    public float maxHealth = 120f;
    public float currentHealth;
    public bool IsDead { get; private set; }

    public string[] damageTags = new string[] { "PlayerProjectile", "Bullet" };

    private BossController owner;

    void Awake()
    {
        currentHealth = maxHealth;
        Log("Spawn");
    }

    public void Init(BossController owner)
    {
        this.owner = owner;
    }

    public void ApplyExternalDamage(float dmg)
    {
        if (IsDead) return;
        currentHealth -= dmg;
        Log($"Damage {dmg}, HP {currentHealth}/{maxHealth}");
        if (currentHealth <= 0f)
        {
            IsDead = true;
            currentHealth = 0f;
            if (owner != null) owner.OnTotemDestroyed(this);
            Log("Destroyed");
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        foreach (var t in damageTags)
        {
            if (other.CompareTag(t))
            {
                float dmg = 25f;
                var info = other.GetComponent<ComponentWithDamage>();
                if (info != null) dmg = info.damage;

                ApplyExternalDamage(dmg);

                if (other.attachedRigidbody == null || other.isTrigger) Destroy(other.gameObject);
                break;
            }
        }
    }
}

public class ComponentWithDamage : MonoBehaviour
{
    public float damage = 25f;
}
