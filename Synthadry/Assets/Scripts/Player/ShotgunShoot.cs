using UnityEngine;

public class Shotgun : MonoBehaviour
{
    [Header("Параметры оружия")]
    public float damage = 25f;
    public float range = 50f;
    public float coneAngle = 20f;
    public float fireRate = 1f;
    public Transform barrelEnd;
    public Camera playerCamera;
    public LayerMask targetLayers;
    public int coneVertices = 8;
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    [Header("Эффекты")]
    public GameObject muzzleFlashPrefab;
    public GameObject hitEffectPrefab;
    public AudioClip shotSound;
    private AudioSource audioSource;

    private float nextTimeToFire = 0f;

    void Start()
    {
        if (barrelEnd == null)
        {
            Debug.LogError("❌ barrelEnd не назначен!");
            enabled = false;
            return;
        }

        if (playerCamera == null)
        {
            Debug.LogError("❌ playerCamera не назначена!");
            enabled = false;
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.clip = shotSound;
    }

    void Update()
    {
        if (Input.GetButtonDown("Fire1") && Time.time >= nextTimeToFire)
        {
            Debug.Log("🔥 FIREEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEee");
            nextTimeToFire = Time.time + 1f / fireRate;
            Shoot();
        }
    }

    void LateUpdate()
    {
        transform.position = playerCamera.transform.position + playerCamera.transform.rotation * positionOffset;
        transform.rotation = playerCamera.transform.rotation * Quaternion.Euler(rotationOffset);
    }

    void Shoot()
    {
        // Вспышка
        if (muzzleFlashPrefab != null)
        {
            var muzzleFlash = Instantiate(muzzleFlashPrefab, barrelEnd.position, barrelEnd.rotation);
            Destroy(muzzleFlash, 1f);
        }

        // Звук
        if (shotSound != null)
            audioSource.PlayOneShot(shotSound);

        // Направление центр-экрана
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        Vector3 shootDirection = ray.direction;

        Debug.Log("🔎 Поиск целей через OverlapSphere...");

        Collider[] hitColliders = Physics.OverlapSphere(barrelEnd.position, range, targetLayers);
        Debug.Log("🔵 Найдено объектов в сфере: " + hitColliders.Length);

        // ВИЗУАЛИЗАЦИЯ КОНУСА
        DrawDebugCone(shootDirection);

        foreach (Collider collider in hitColliders)
        {
            Debug.Log("🟡 Найден объект в сфере: " + collider.name);

            Vector3 targetDirection = (collider.transform.position - barrelEnd.position).normalized;
            float angleToTarget = Vector3.Angle(shootDirection, targetDirection);

            Debug.Log($"📐 Угол до {collider.name}: {angleToTarget}");

            if (angleToTarget <= coneAngle * 0.5f)
            {
                Debug.Log("🟢 ЦЕЛЬ В КОНУСЕ: " + collider.name);

                MobController mob = collider.GetComponentInParent<MobController>();
                if (mob != null)
                {
                    Debug.Log("💥 Наношу урон объекту " + collider.name);
                    mob.TakeDamage(damage);

                    if (hitEffectPrefab != null)
                    {
                        var hitEffect = Instantiate(hitEffectPrefab, collider.ClosestPoint(barrelEnd.position), Quaternion.identity);
                        Destroy(hitEffect, 1f);
                    }
                }
                else
                {
                    Debug.Log("❌ НЕ НАШЁЛ MobController на объекте " + collider.name);
                }
            }
            else
            {
                Debug.Log("⚫ Объект вне конуса: " + collider.name);
            }
        }
    }

    // 🔥 ВИЗУАЛИЗАЦИЯ КОНУСА ОРУЖИЯ
    void DrawDebugCone(Vector3 shootDirection)
    {
        int rays = 22;
        float halfAngle = coneAngle * 0.5f;

        // 1) Линии-разлёты внутри конуса
        for (int i = 0; i < rays; i++)
        {
            float yaw = Random.Range(-halfAngle, halfAngle);
            float pitch = Random.Range(-halfAngle, halfAngle);

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
            Vector3 dir = rot * shootDirection;

            Debug.DrawRay(barrelEnd.position, dir * range, Color.yellow, 0.2f);
        }

        // 2) Рисуем окружность конца конуса
        int segments = 36;
        float radius = Mathf.Tan(halfAngle * Mathf.Deg2Rad) * range;

        Vector3 forward = shootDirection.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 up = Vector3.Cross(forward, right);

        Vector3 prev = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;

            Vector3 point =
                barrelEnd.position +
                forward * range +
                (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;

            if (i > 0)
                Debug.DrawLine(prev, point, Color.cyan, 0.2f);

            prev = point;
        }

        // 3) Центральный луч
        Debug.DrawRay(barrelEnd.position, shootDirection * range, Color.red, 0.2f);
    }

    // Gizmos (редактор)
    void OnDrawGizmosSelected()
    {
        if (barrelEnd == null) return;

        Gizmos.color = Color.yellow;
        float coneRadius = Mathf.Tan(coneAngle * 0.5f * Mathf.Deg2Rad) * range;

        Gizmos.DrawWireSphere(barrelEnd.position + barrelEnd.forward * range, coneRadius);
        Gizmos.DrawRay(barrelEnd.position, barrelEnd.forward * range);
    }
}
