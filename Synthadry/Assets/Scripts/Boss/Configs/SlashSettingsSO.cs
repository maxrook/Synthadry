using UnityEngine;

[CreateAssetMenu(fileName = "SlashSettings", menuName = "Boss/Attacks/Slash")]
public class SlashSettingsSO : ScriptableObject
{
    [Header("Префаб зоны удара")]
    public GameObject HitboxPrefab;

    [Header("Активное окно удара (сек)")]
    public float Active = 0.25f;

    [Header("Урон от удара")]
    public float Damage = 25f;

    [Header("Дуга атаки (градусы)")]
    public float ArcDegrees = 100f;

    [Header("Радиус удара")]
    public float Radius = 4.5f;

    [Header("Высота hitbox")]
    public float Height = 1.2f;

    public SlashHitbox Create(
        BossObjectPool pool,
        BossController owner,
        bool debugLogs,
        LayerMask playerMask,
        string playerTag,
        Vector3 position,
        Quaternion rotation)
    {
        var go = pool.Get(HitboxPrefab, position, rotation);
        var hitbox = go.GetComponent<SlashHitbox>();

        hitbox.DebugLogs = debugLogs;
        hitbox.Init(
            owner: owner,
            activeTime: Active,
            damage: Damage,
            radius: Radius,
            arcDegrees: ArcDegrees,
            height: Height,
            playerMask: playerMask,
            playerTag: playerTag
        );

        return hitbox;
    }
}