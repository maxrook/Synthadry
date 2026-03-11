using UnityEngine;

[CreateAssetMenu(fileName = "SlamSettings", menuName = "Boss/Attacks/Slam")]
public class SlamSettingsSO : ScriptableObject
{
    [Header("Префаб волны")]
    public GameObject WavePrefab;

    [Header("Количество ударов подряд")]
    public int SeriesCount = 4;

    [Header("Интервал между волнами (сек)")]
    public float Interval = 0.75f;

    [Header("Урон от волны")]
    public float Damage = 20f;

    [Header("Скорость обычной волны")]
    public float NormalWaveSpeed = 9f;

    [Header("Макс. радиус обычной волны")]
    public float NormalWaveMaxRadius = 15f;

    [Header("Скорость широкой волны")]
    public float WideWaveSpeed = 10.5f;

    [Header("Макс. радиус широкой волны")]
    public float WideWaveMaxRadius = 19f;
}