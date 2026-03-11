using UnityEngine;

[CreateAssetMenu(fileName = "SlashSettings", menuName = "Boss/Attacks/Slash")]
public class SlashSettingsSO : ScriptableObject
{
    [Header("Префаб зоны удара")]
    public GameObject HitboxPrefab;

    [Header("Количество ударов")]
    public int Count = 1;

    [Header("Подготовка к удару (сек)")]
    public float Windup = 0.4f;

    [Header("Активное окно удара (сек)")]
    public float Active = 0.25f;

    [Header("Задержка после удара (сек)")]
    public float Recovery = 0.3f;

    [Header("Урон от удара")]
    public float Damage = 25f;

    [Header("Дуга атаки (градусы)")]
    public float ArcDegrees = 100f;

    [Header("Радиус удара")]
    public float Radius = 4.5f;

    [Header("Высота hitbox")]
    public float Height = 1.2f;

    [Header("Макс. время ожидание игрока пока он не войдёт в радиус атаки (сек)")]
    public float WaitMax = 3.0f;
}