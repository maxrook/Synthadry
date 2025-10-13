using UnityEngine;

public class SpawnTrigger : MonoBehaviour
{
    [Header("Префаб моба")]
    public GameObject mobPrefab;

    [Header("Точки появления")]
    public Transform[] spawnPoints;

    [Header("Спавнить, только если спавнопойнт вне поля зрения")]
    public bool spawnOnlyIfNotVisible = true;

    private bool isActive = true;
    private Camera playerCamera;

    private void Awake()
    {
        playerCamera = Camera.main;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        if (other.CompareTag("Player"))
        {
            foreach (Transform point in spawnPoints)
            {
                if (!spawnOnlyIfNotVisible || !PointVisibleToCamera(point.position))
                {
                    Instantiate(mobPrefab, point.position, point.rotation);
                }
            }
            isActive = false;
        }
    }

    private bool PointVisibleToCamera(Vector3 worldPos)
    {
        if (playerCamera == null) return false;

        Vector3 vp = playerCamera.WorldToViewportPoint(worldPos);
        bool insideViewport = vp.z > 0f && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f;
        if (!insideViewport) return false;

        Vector3 dir = worldPos - playerCamera.transform.position;
        bool hitSomething = Physics.Raycast(
            playerCamera.transform.position,
            dir.normalized,
            out RaycastHit hit,
            dir.magnitude,
            ~0,
            QueryTriggerInteraction.Ignore);

        return !hitSomething;
    }

    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        Gizmos.color = Color.red;
        foreach (Transform t in spawnPoints)
        {
            if (t == null)
                Debug.LogWarning("spawn point is null");
            else
                Gizmos.DrawSphere(t.position, 0.5f);
        }
    }
}
