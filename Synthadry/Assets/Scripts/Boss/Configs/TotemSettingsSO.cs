using UnityEngine;

[CreateAssetMenu(fileName = "TotemSettings", menuName = "Boss/Attacks/Totem")]
public class TotemSettingsSO : ScriptableObject
{
    [Header("Префаб тотема")]
    public GameObject Prefab;

    public Totem Create(
        BossObjectPool pool,
        BossController owner,
        bool debugLogs,
        Vector3 position)
    {
        var go = pool.Get(Prefab, position, Quaternion.identity);
        var totem = go.GetComponent<Totem>();

        totem.DebugLogs = debugLogs;
        totem.Init(owner);

        return totem;
    }
}