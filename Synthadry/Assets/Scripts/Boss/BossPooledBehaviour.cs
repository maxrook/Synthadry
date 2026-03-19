using UnityEngine;

public abstract class BossPooledBehaviour : MonoBehaviour
{
    private BossObjectPool _pool;

    public void SetPool(BossObjectPool pool)
    {
        _pool = pool;
    }

    protected void ReturnToPool()
    {
        if (_pool != null)
            _pool.Return(gameObject);
        else
            Destroy(gameObject);
    }

    public virtual void OnTakenFromPool() { }
    public virtual void OnReturnedToPool() { }
}