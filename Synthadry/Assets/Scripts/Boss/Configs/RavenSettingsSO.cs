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

    public RavenProjectile Create(
        BossObjectPool pool,
        BossController owner,
        bool debugLogs,
        LayerMask playerMask,
        string playerTag,
        Vector3 position,
        Vector3 direction)
    {
        var go = pool.Get(ProjectilePrefab, position, Quaternion.LookRotation(direction.normalized, Vector3.up));
        var projectile = go.GetComponent<RavenProjectile>();

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