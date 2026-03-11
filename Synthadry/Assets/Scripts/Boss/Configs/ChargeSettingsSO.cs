using UnityEngine;

[CreateAssetMenu(fileName = "ChargeSettings", menuName = "Boss/Attacks/Charge")]
public class ChargeSettingsSO : ScriptableObject
{
    [Header("Подготовка перед рывком (сек)")]
    public float Windup = 0.5f;

    [Header("Скорость рывка")]
    public float Speed = 18f;

    [Header("Длительность рывка (сек)")]
    public float Duration = 0.7f;

    [Header("Перезарядка рывка (сек)")]
    public float Cooldown = 2.0f;

    [Header("Урон от рывка")]
    public float Damage = 30f;
}