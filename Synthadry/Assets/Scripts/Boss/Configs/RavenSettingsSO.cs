using UnityEngine;

[CreateAssetMenu(fileName = "RavenSettings", menuName = "Boss/Attacks/Raven")]
public class RavenSettingsSO : ScriptableObject
{
    [Header("Префаб воронов")]
    public GameObject ProjectilePrefab;

    [Header("Количество выстрелов в серии")]
    public int Burst = 12;

    [Header("Интервал между выстрелами (сек)")]
    public float Rate = 0.09f;

    [Header("Скорость полёта снаряда")]
    public float Speed = 16f;

    [Header("Время жизни снаряда (сек)")]
    public float Life = 2.5f;

    [Header("Урон от снаряда")]
    public float Damage = 10f;

    [Header("Разброс угла выстрелов")]
    public float SpreadAngle = 8f;
}