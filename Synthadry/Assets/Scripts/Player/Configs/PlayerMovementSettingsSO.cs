using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMovementSettings", menuName = "Player/Player Movement")]
public class PlayerMovementConfig : ScriptableObject
{
    [Header("Чувствительность мыши по горизонтали")]
    public float SensitivityX = 2.0f;

    [Header("Чувствительность мыши по вертикали")]
    public float SensitivityY = 2.0f;

    [Header("Минимальный угол камеры по вертикали")]
    public float MinAngleY = -75.0f;

    [Header("Максимальный угол камеры по вертикали")]
    public float MaxAngleY = 75.0f;

    [Header("Скорость движения по земле")]
    public float MoveSpeed = 11.0f;

    [Header("Множитель скорости при беге")]
    public float RunSpeedMultiplier = 1.15f;

    [Header("Ускорение на земле")]
    public float GroundAcceleration = 85.0f;

    [Header("Трение на земле")]
    public float GroundFriction = 9.0f;

    [Header("Минимальная скорость для трения")]
    public float StopSpeed = 2.0f;

    [Header("Скорость движения в воздухе")]
    public float AirMoveSpeed = 11.0f;

    [Header("Ускорение в воздухе")]
    public float AirAcceleration = 35.0f;

    [Header("Контроль движения в воздухе")]
    [Range(0.0f, 1.0f)]
    public float AirControl = 0.65f;

    [Header("Высота прыжка")]
    public float JumpHeight = 1.8f;

    [Header("Гравитация")]
    public float Gravity = 28.0f;

    [Header("Максимальная скорость падения")]
    public float MaxFallSpeed = 45.0f;

    [Header("Клавиша дэша")]
    public KeyCode DashKey = KeyCode.Q;

    [Header("Дистанция дэша")]
    public float DashDistance = 5.0f;

    [Header("Длительность дэша (сек)")]
    public float DashDuration = 0.14f;

    [Header("Перезарядка дэша (сек)")]
    public float DashCooldown = 0.45f;

    [Header("Стоимость дэша в стамине")]
    public float DashStaminaCost = 30.0f;

    [Header("Разрешить дэш в воздухе")]
    public bool AllowAirDash = true;

    [Header("Максимальная стамина")]
    public float MaxStamina = 100.0f;

    [Header("Скорость восстановления стамины")]
    public float StaminaRegenerationRate = 35.0f;

    [Header("Задержка восстановления стамины (сек)")]
    public float StaminaRegenerationDelay = 1.2f;
}