using UnityEngine;

public abstract class BossPooledBehaviour : MonoBehaviour
{
    private BossObjectPool _pool;
    private GameObject _prefab;

    public void SetPool(BossObjectPool pool, GameObject prefab)
    {
        _pool = pool;
        _prefab = prefab;
    }

    protected void ReturnToPool()
    {
        PrepareToReturn();
        _pool.Return(_prefab, gameObject);
    }

    protected virtual void PrepareToReturn() { }
}