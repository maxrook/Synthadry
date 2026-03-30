using UnityEngine;

public class Totem : BossPooledBehaviour
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
    }

    public void Init(BossController owner)
    {
        _owner = owner;
        CurrentHealth = MaxHealth;
        IsDead = false;
        Log("Spawn");
    }

    public void ApplyExternalDamage(float dmg)
    {
        if (IsDead)
            return;

        CurrentHealth -= dmg;
        Log($"Damage {dmg}, HP {CurrentHealth}/{MaxHealth}");

        if (CurrentHealth <= 0f)
        {
            IsDead = true;
            CurrentHealth = 0f;
            _owner.OnTotemDestroyed(this);
            Log("Destroyed");
            ReturnToPool();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsDead || other == null)
            return;

        foreach (var t in DamageTags)
        {
            if (other.CompareTag(t))
            {
                float dmg = 25f;
                var info = other.GetComponent<ComponentWithDamage>();
                if (info != null)
                    dmg = info.Damage;

                ApplyExternalDamage(dmg);

                if (other.attachedRigidbody == null || other.isTrigger)
                    Destroy(other.gameObject);

                break;
            }
        }
    }

    protected override void PrepareToReturn()
    {
        _owner = null;
        IsDead = false;
        CurrentHealth = MaxHealth;
    }
}

public class ComponentWithDamage : MonoBehaviour
{
    public float Damage = 25f;
}