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
}