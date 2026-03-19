using System.Collections.Generic;
using UnityEngine;

public sealed class BossObjectPool
{
    private readonly Dictionary<GameObject, Stack<GameObject>> _pools = new();
    private readonly Dictionary<GameObject, GameObject> _instanceToPrefab = new();
    private readonly Dictionary<GameObject, Transform> _parents = new();

    private readonly Transform _root;

    public BossObjectPool(string rootName)
    {
        var rootGO = new GameObject(rootName);
        _root = rootGO.transform;
    }

    public void Warmup(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0)
            return;

        var pool = GetOrCreatePool(prefab);
        var parent = GetOrCreateParent(prefab);

        for (int i = pool.Count; i < count; i++)
        {
            var instance = CreateNewInstance(prefab, parent);
            instance.SetActive(false);
            pool.Push(instance);
        }
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[BossObjectPool] Spawn called with null prefab.");
            return null;
        }

        var pool = GetOrCreatePool(prefab);

        GameObject instance = pool.Count > 0
            ? pool.Pop()
            : CreateNewInstance(prefab, GetOrCreateParent(prefab));

        instance.transform.SetParent(null, worldPositionStays: false);
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);

        NotifyTakenFromPool(instance);
        return instance;
    }

    public void Return(GameObject instance)
    {
        if (instance == null)
            return;

        if (!_instanceToPrefab.TryGetValue(instance, out var prefab) || prefab == null)
        {
            Object.Destroy(instance);
            return;
        }

        NotifyReturnedToPool(instance);

        instance.SetActive(false);
        instance.transform.SetParent(GetOrCreateParent(prefab), worldPositionStays: false);
        GetOrCreatePool(prefab).Push(instance);
    }

    private Stack<GameObject> GetOrCreatePool(GameObject prefab)
    {
        if (!_pools.TryGetValue(prefab, out var pool))
        {
            pool = new Stack<GameObject>();
            _pools[prefab] = pool;
        }

        return pool;
    }

    private Transform GetOrCreateParent(GameObject prefab)
    {
        if (!_parents.TryGetValue(prefab, out var parent) || parent == null)
        {
            var go = new GameObject($"{prefab.name}_Pool");
            parent = go.transform;
            parent.SetParent(_root, worldPositionStays: false);
            _parents[prefab] = parent;
        }

        return parent;
    }

    private GameObject CreateNewInstance(GameObject prefab, Transform parent)
    {
        var instance = Object.Instantiate(prefab, parent);
        _instanceToPrefab[instance] = prefab;

        var pooledBehaviours = instance.GetComponentsInChildren<BossPooledBehaviour>(true);
        foreach (var pooled in pooledBehaviours)
            pooled.SetPool(this);

        return instance;
    }

    private static void NotifyTakenFromPool(GameObject instance)
    {
        var pooledBehaviours = instance.GetComponentsInChildren<BossPooledBehaviour>(true);
        foreach (var pooled in pooledBehaviours)
            pooled.OnTakenFromPool();
    }

    private static void NotifyReturnedToPool(GameObject instance)
    {
        var pooledBehaviours = instance.GetComponentsInChildren<BossPooledBehaviour>(true);
        foreach (var pooled in pooledBehaviours)
            pooled.OnReturnedToPool();
    }
}