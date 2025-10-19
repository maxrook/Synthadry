using System.Collections.Generic;
using UnityEngine;

public class GroundWave : MonoBehaviour
{
    public bool debugLogs = true;
    void Log(string m){ if(debugLogs) Debug.Log($"[BOSS:WAVE] {m}"); }

    LineRenderer lr;
    const int SEGMENTS = 64;

    private BossController owner;
    private Vector3 origin;
    private float moveSpeed, maxRadius, thickness, height, damage;
    private LayerMask playerMask;
    private string playerTag;

    private float currentRadius;
    private bool init;

    public float footHitWindow = 0.20f;
    private readonly HashSet<int> damagedTargets = new HashSet<int>();

    public void Init(
        BossController owner, Vector3 origin, float moveSpeed, float maxRadius,
        float thickness, float height, float damage, LayerMask playerMask, string playerTag)
    {
        this.owner = owner; 
        this.origin = origin; 
        this.moveSpeed = moveSpeed; 
        this.maxRadius = maxRadius;
        this.thickness = thickness; 
        this.height = height; 
        this.damage = damage;
        this.playerMask = playerMask; 
        this.playerTag = playerTag;

        transform.position = origin; 
        init = true;

        SetupLR();
        Log($"Spawn (speed={moveSpeed}, maxR={maxRadius}, y={origin.y:0.00})");
    }

    void SetupLR()
    {
        lr = gameObject.AddComponent<LineRenderer>();
        lr.positionCount = SEGMENTS + 1;
        lr.loop = true;
        lr.useWorldSpace = true;
        lr.widthMultiplier = 0.08f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = new Color(0.15f, 0.8f, 1f, 0.9f);
    }

    void Update()
    {
        if (!init) return;

        currentRadius += moveSpeed * Time.deltaTime;
        if (currentRadius > maxRadius) { Destroy(gameObject); return; }

        float y = origin.y + 0.02f;
        for (int i = 0; i <= SEGMENTS; i++)
        {
            float a = i * Mathf.PI * 2f / SEGMENTS;
            Vector3 p = origin + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * currentRadius;
            lr.SetPosition(i, new Vector3(p.x, y, p.z));
        }

        float inner = currentRadius - thickness * 0.5f;
        float outer = currentRadius + thickness * 0.5f;

        Collider[] cols = Physics.OverlapSphere(origin, outer, playerMask, QueryTriggerInteraction.Ignore);

        foreach (var c in cols)
        {
            if (c == null) continue;

            GameObject root = c.attachedRigidbody ? c.attachedRigidbody.gameObject : c.transform.root.gameObject;
            if (root == null) root = c.gameObject;
            if (!root.CompareTag(playerTag) && !c.CompareTag(playerTag)) continue;

            int id = root.GetInstanceID();
            if (damagedTargets.Contains(id)) continue;

            Vector3 rootPosXZ = root.transform.position; 
            rootPosXZ.y = origin.y;
            float dist = Vector3.Distance(rootPosXZ, origin);
            if (dist <= inner || dist >= outer) continue;

            float feetY = float.PositiveInfinity;
            var allCols = root.GetComponentsInChildren<Collider>();
            foreach (var pc in allCols)
            {
                if (pc == null || pc.isTrigger) continue;
                feetY = Mathf.Min(feetY, pc.bounds.min.y);
            }
            if (float.IsPositiveInfinity(feetY)) feetY = root.transform.position.y;

            if (feetY > origin.y + footHitWindow) 
            {
                continue;
            }

            damagedTargets.Add(id);
            owner.TryDamagePlayerGO(root, damage);
            Log($"Hit player, r≈{currentRadius:0.0}");
        }
    }

    void OnDestroy()
    {
        damagedTargets.Clear();
    }
}
