using UnityEngine;

[CreateAssetMenu(fileName = "BossConfig", menuName = "Boss/Boss Config")]
public class BossConfigSO : ScriptableObject
{
    [Header("Максимальное здоровье босса")]
    public float MaxHealth = 1500f;

    [Header("Радиус агро (дистанция начала боя)")]
    public float AggroRadius = 18f;

    [Header("Пауза между паттернами атак (мин / макс)")]
    public Vector2 PatternPause = new Vector2(1.0f, 1.6f);

    [Header("Скорость преследования игрока")]
    public float ChaseSpeed = 4.5f;

    [Header("Скорость движения во время атак")]
    public float AttackMoveSpeed = 3.5f;

    [Header("Raven pattern: количество выстрелов в серии")]
    public int RavenBurst = 12;

    [Header("Raven pattern: интервал между выстрелами (сек)")]
    public float RavenRate = 0.09f;

    [Header("Raven pattern: разброс угла выстрелов")]
    public float RavenSpreadAngle = 8f;

    [Header("Slam pattern: количество ударов подряд")]
    public int SlamSeriesCount = 4;

    [Header("Slam pattern: интервал между волнами (сек)")]
    public float SlamInterval = 0.75f;

    [Header("Slash pattern: количество ударов")]
    public int SlashCount = 1;

    [Header("Slash pattern: подготовка к удару (сек)")]
    public float SlashWindup = 0.4f;

    [Header("Slash pattern: задержка после удара (сек)")]
    public float SlashRecovery = 0.3f;

    [Header("Slash pattern: макс. время ожидания игрока (сек)")]
    public float SlashWaitMax = 3.0f;

    [Header("Charge pattern: перезарядка рывка (сек)")]
    public float ChargeCooldown = 2.0f;

    [Header("Totem phase: регенерация HP/сек")]
    public float TotemRegenPerSec = 8f;

    [Header("Totem phase: радиус кольца тотемов")]
    public float TotemRingRadius = 8f;

    [Header("Totem phase: стан при появлении (сек)")]
    public float TotemStunDurationOnSpawn = 1.0f;

    [Header("Totem phase: процент HP для появления тотемов (0–1)")]
    [Range(0.05f, 1f)]
    public float TotemTriggerHealthPercent = 0.25f;
}