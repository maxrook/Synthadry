using UnityEngine;

[CreateAssetMenu(fileName = "RavenSettings", menuName = "Boss/Attacks/Raven")]
public class RavenSettingsSO : ScriptableObject
{
    [Header("Префаб ворона / снаряда")]
    public GameObject ProjectilePrefab;

    [Header("Скорость полёта снаряда")]
    public float Speed = 16f;

    [Header("Время жизни снаряда (сек)")]
    public float Life = 2.5f;

    [Header("Урон от снаряда")]
    public float Damage = 10f;

    public RavenProjectile SpawnProjectile(
        BossObjectPool pool,
        BossController owner,
        bool debugLogs,
        LayerMask playerMask,
        string playerTag,
        Vector3 position,
        Vector3 direction)
    {
        if (ProjectilePrefab == null)
        {
            Debug.LogWarning("[RavenSettingsSO] ProjectilePrefab is null.");
            return null;
        }

        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector3.forward;

        var rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        var go = pool.Spawn(ProjectilePrefab, position, rotation);
        if (go == null)
            return null;

        var projectile = go.GetComponent<RavenProjectile>();
        if (projectile == null)
        {
            Debug.LogError("[RavenSettingsSO] RavenProjectile component not found on ProjectilePrefab.");
            return null;
        }

        projectile.DebugLogs = debugLogs;
        projectile.Init(
            owner: owner,
            dir: direction,
            speed: Speed,
            life: Life,
            damage: Damage,
            playerMask: playerMask,
            playerTag: playerTag
        );

        return projectile;
    }
}