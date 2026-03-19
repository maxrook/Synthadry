using UnityEngine;

[CreateAssetMenu(fileName = "TotemSettings", menuName = "Boss/Attacks/Totem")]
public class TotemSettingsSO : ScriptableObject
{
    [Header("Префаб тотема")]
    public GameObject Prefab;

    public Totem SpawnTotem(
        BossObjectPool pool,
        BossController owner,
        bool debugLogs,
        Vector3 position)
    {
        if (Prefab == null)
        {
            Debug.LogWarning("[TotemSettingsSO] Prefab is null.");
            return null;
        }

        var go = pool.Spawn(Prefab, position, Quaternion.identity);
        if (go == null)
            return null;

        var totem = go.GetComponent<Totem>();
        if (totem == null)
        {
            Debug.LogError("[TotemSettingsSO] Totem component not found on Prefab.");
            return null;
        }

        totem.DebugLogs = debugLogs;
        totem.Init(owner);
        return totem;
    }
}