using UnityEngine;

public class Totem : MonoBehaviour
{
    public bool DebugLogs = false;
    void Log(string m) { if (DebugLogs) Debug.Log($"[BOSS:TOTEM] {m}"); }

    public float MaxHealth = 120f;
    public float CurrentHealth;
    public bool IsDead { get; private set; }

    public string[] DamageTags = new string[] { "PlayerProjectile", "Bullet" };

    private BossController _owner;

    void Awake()
    {
        CurrentHealth = MaxHealth;
        Log("Spawn");
    }

    public void Init(BossController owner)
    {
        _owner = owner;
    }

    public void ApplyExternalDamage(float dmg)
    {
        if (IsDead) return;
        CurrentHealth -= dmg;
        Log($"Damage {dmg}, HP {CurrentHealth}/{MaxHealth}");
        if (CurrentHealth <= 0f)
        {
            IsDead = true;
            CurrentHealth = 0f;
            if (_owner != null) _owner.OnTotemDestroyed(this);
            Log("Destroyed");
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        foreach (var t in DamageTags)
        {
            if (other.CompareTag(t))
            {
                float dmg = 25f;
                var info = other.GetComponent<ComponentWithDamage>();
                if (info != null) dmg = info.Damage;

                ApplyExternalDamage(dmg);

                if (other.attachedRigidbody == null || other.isTrigger) Destroy(other.gameObject);
                break;
            }
        }
    }
}

public class ComponentWithDamage : MonoBehaviour
{
    public float Damage = 25f;
}