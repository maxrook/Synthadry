using System.Collections.Generic;
using UnityEngine;

public sealed class BossObjectPool
{
    private readonly Dictionary<GameObject, Stack<GameObject>> _pools = new();
    private readonly Transform _root;

    public BossObjectPool(string rootName)
    {
        _root = new GameObject(rootName).transform;
    }

    public void Warmup(GameObject prefab, int count)
    {
        var pool = GetOrCreatePool(prefab);

        for (int i = 0; i < count; i++)
        {
            var obj = Create(prefab);
            obj.gameObject.SetActive(false);
            pool.Push(obj);
        }
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var pool = GetOrCreatePool(prefab);

        GameObject obj;
        if (pool.Count > 0)
        {
            obj = pool.Pop();
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.gameObject.SetActive(true);
        }
        else
        {
            obj = Create(prefab);
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.gameObject.SetActive(true);
        }

        return obj;
    }

    public void Return(GameObject prefab, GameObject obj)
    {
        obj.gameObject.SetActive(false);
        obj.transform.SetParent(_root, false);
        GetOrCreatePool(prefab).Push(obj);
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

    private GameObject Create(GameObject prefab)
    {
        var obj = Object.Instantiate(prefab, _root);
        var pooled = obj.GetComponent<BossPooledBehaviour>();
        if (pooled != null)
            pooled.SetPool(this, prefab);

        return obj;
    }
}