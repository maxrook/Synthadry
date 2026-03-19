using UnityEngine;

[CreateAssetMenu(fileName = "SlamSettings", menuName = "Boss/Attacks/Slam")]
public class SlamSettingsSO : ScriptableObject
{
    [Header("Префаб волны")]
    public GameObject WavePrefab;

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

    public GroundWave SpawnWave(
        BossObjectPool pool,
        BossController owner,
        bool debugLogs,
        LayerMask playerMask,
        string playerTag,
        Vector3 origin,
        bool wide)
    {
        if (WavePrefab == null)
        {
            Debug.LogWarning("[SlamSettingsSO] WavePrefab is null.");
            return null;
        }

        var go = pool.Spawn(WavePrefab, origin, Quaternion.identity);
        if (go == null)
            return null;

        var wave = go.GetComponent<GroundWave>();
        if (wave == null)
        {
            Debug.LogError("[SlamSettingsSO] GroundWave component not found on WavePrefab.");
            return null;
        }

        wave.DebugLogs = debugLogs;
        wave.Init(
            owner: owner,
            origin: origin,
            moveSpeed: wide ? WideWaveSpeed : NormalWaveSpeed,
            maxRadius: wide ? WideWaveMaxRadius : NormalWaveMaxRadius,
            thickness: 1.2f,
            height: 0.5f,
            damage: Damage,
            playerMask: playerMask,
            playerTag: playerTag
        );

        return wave;
    }
}