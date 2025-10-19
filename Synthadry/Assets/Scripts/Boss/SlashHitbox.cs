using System.Collections.Generic;
using UnityEngine;

public class SlashHitbox : MonoBehaviour
{
    public bool debugLogs = true;
    void Log(string m){ if(debugLogs) Debug.Log($"[BOSS:SLASH] {m}"); }

    public float radius = 4.5f;
    public float arcDegrees = 100f;
    public float height = 1.2f;

    private BossController owner;
    private float life;
    private float damage;
    private LayerMask playerMask;
    private string playerTag;
    private float t;

    private readonly HashSet<int> damagedTargets = new HashSet<int>();

    LineRenderer lrArc;
    LineRenderer lrGuide;
    GameObject tip;

    const int SEGMENTS = 36;

    public void Init(BossController owner, float activeTime, float damage,
                     LayerMask playerMask, string playerTag)
    {
        this.owner = owner; 
        this.life = activeTime; 
        this.damage = damage;
        this.playerMask = playerMask; 
        this.playerTag = playerTag;

        SetupVisuals();
        Log($"Spawn (active={activeTime:0.00}s, dmg={damage})");
    }

    void SetupVisuals()
    {
        lrArc = gameObject.AddComponent<LineRenderer>();
        lrArc.positionCount = SEGMENTS + 1;
        lrArc.loop = false;
        lrArc.useWorldSpace = true;
        lrArc.widthMultiplier = 0.08f;
        lrArc.material = new Material(Shader.Find("Sprites/Default"));
        lrArc.startColor = lrArc.endColor = new Color(1f, 0.4f, 0.2f, 0.9f);

        lrGuide = new GameObject("SlashGuide").AddComponent<LineRenderer>();
        lrGuide.transform.SetParent(transform, worldPositionStays:false);
        lrGuide.positionCount = 2;
        lrGuide.useWorldSpace = true;
        lrGuide.widthMultiplier = 0.06f;
        lrGuide.material = new Material(Shader.Find("Sprites/Default"));
        lrGuide.startColor = lrGuide.endColor = new Color(1f, 0.9f, 0.2f, 0.95f);

        tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tip.name = "SlashTip";
        tip.transform.SetParent(transform);
        tip.transform.localScale = Vector3.one * 0.18f;
        var col = tip.GetComponent<Collider>(); if (col) col.enabled = false;
        var mr = tip.GetComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Standard"));
        mr.material.color = new Color(1f, 0.9f, 0.2f, 0.95f);
    }

    void Update()
    {
        t += Time.deltaTime;
        if (t > life) { Cleanup(); return; }

        float y = transform.position.y;
        Vector3 center = new Vector3(transform.position.x, y + 0.02f, transform.position.z);

        float half = arcDegrees * 0.5f;
        Quaternion left = Quaternion.Euler(0f, -half, 0f) * transform.rotation;
        for (int i = 0; i <= SEGMENTS; i++)
        {
            float a = (i / (float)SEGMENTS) * arcDegrees;
            Quaternion rot = Quaternion.Euler(0f, a, 0f) * left;
            Vector3 p = center + (rot * Vector3.forward) * radius;
            lrArc.SetPosition(i, p);
        }

        Vector3 start = center;
        Vector3 end = center + transform.forward * radius;
        lrGuide.SetPosition(0, start);
        lrGuide.SetPosition(1, end);
        if (tip) tip.transform.position = end;

        Vector3 p1 = new Vector3(transform.position.x, y + 0.1f, transform.position.z);
        Vector3 p2 = new Vector3(transform.position.x, y + 0.1f + height, transform.position.z);
        Collider[] cols = Physics.OverlapCapsule(p1, p2, radius, playerMask);

        foreach (var c in cols)
        {
            GameObject root = c.attachedRigidbody ? c.attachedRigidbody.gameObject : c.transform.root.gameObject;
            if (root == null) root = c.gameObject;
            if (!root.CompareTag(playerTag) && !c.CompareTag(playerTag)) continue;

            Vector3 to = (root.transform.position - transform.position);
            to.y = 0f;
            if (Vector3.Angle(transform.forward, to) > half) continue;
            if (to.magnitude > radius + 0.1f) continue;

            int id = root.GetInstanceID();
            if (damagedTargets.Contains(id)) continue;

            damagedTargets.Add(id);
            owner.TryDamagePlayerGO(root, damage);
            Log($"Hit player (id={id})");
        }
    }

    void Cleanup()
    {
        if (lrGuide) Destroy(lrGuide.gameObject);
        if (tip) Destroy(tip);
        damagedTargets.Clear();
        Destroy(gameObject);
    }
}
