using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log(transform.name + " получил " + damage + " урона.  Здоровье: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(transform.name + " уничтожен!");
        Destroy(gameObject); 
    }
}