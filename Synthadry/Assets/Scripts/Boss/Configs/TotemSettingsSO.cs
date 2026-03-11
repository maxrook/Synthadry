using UnityEngine;

[CreateAssetMenu(fileName = "TotemSettings", menuName = "Boss/Attacks/Totem")]
public class TotemSettingsSO : ScriptableObject
{
    [Header("Префаб тотема")]
    public GameObject Prefab;

    [Header("Регенерация HP/сек при тотемах")]
    public float RegenPerSec = 8f;

    [Header("Радиус кольца тотемов")]
    public float RingRadius = 8f;

    [Header("Стан при появлении (сек)")]
    public float StunDurationOnSpawn = 1.0f;

    [Header("Процент HP для появления тотемов (0–1)")]
    [Range(0.05f, 1f)]
    public float TriggerHealthPercent = 0.25f;
}