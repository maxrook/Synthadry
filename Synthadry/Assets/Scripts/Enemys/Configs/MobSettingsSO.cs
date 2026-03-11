using UnityEngine;

[CreateAssetMenu(fileName = "MobSettings", menuName = "Mob Settings")]
public class MobSettingsSO : ScriptableObject
{
    [Header("Путь патрулирования (локальные точки)")]
    public Vector3[] Waypoints = new Vector3[2];

    [Header("Скорость патруля")]
    public float PatrolSpeed = 5f;

    [Header("Скорость бега за игроком")]
    public float RunSpeed = 8f;

    [Header("Радиус обзора")]
    public float ViewRadius = 10f;

    [Header("Радиус атаки")]
    public float AttackRadius = 2f;

    [Header("Урон моба за одну атаку")]
    public float Damage = 10f;

    [Header("Максимальное здоровье моба")]
    public float MaxHealth = 100f;

    [Header("Интервал между атаками (сек)")]
    public float AttackInterval = 1f;

    [Header("Длительность остановки при патруле (сек)")]
    public float StopDuration = 3f;

    [Header("Время до остановки при патруле (сек)")]
    public float TimeUntilStop = 120f;

    [Header("Время до исчезновения после смерти (сек)")]
    public float TimeUntilDisappearance = 10f;

    [Header("Угол обзора (в градусах)")]
    public float ViewAngle = 80f;

    [Header("Длительность hurt-состояния (сек)")]
    public float HurtDuration = 0.35f;
}