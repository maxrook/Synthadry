using System.Collections.Generic;
using UnityEngine;

public class SlashHitbox : MonoBehaviour
{
    public bool DebugLogs = false;
    void Log(string m) { if (DebugLogs) Debug.Log($"[BOSS:SLASH] {m}"); }

    private float _radius;
    private float _arcDegrees;
    private float _height;

    private BossController _owner;
    private float _life;
    private float _damage;
    private LayerMask _playerMask;
    private string _playerTag;
    private float _t;

    private readonly HashSet<int> _damagedTargets = new HashSet<int>();

    LineRenderer _lrArc;
    LineRenderer _lrGuide;
    GameObject _tip;

    const int SEGMENTS = 36;

    public void Init(
        BossController owner,
        float activeTime,
        float damage,
        float radius,
        float arcDegrees,
        float height,
        LayerMask playerMask,
        string playerTag)
    {
        _owner = owner;
        _life = activeTime;
        _damage = damage;
        _radius = radius;
        _arcDegrees = arcDegrees;
        _height = height;
        _playerMask = playerMask;
        _playerTag = playerTag;

        SetupVisuals();
        Log($"Spawn (active={activeTime:0.00}s, dmg={damage})");
    }

    void SetupVisuals()
    {
        _lrArc = gameObject.AddComponent<LineRenderer>();
        _lrArc.positionCount = SEGMENTS + 1;
        _lrArc.loop = false;
        _lrArc.useWorldSpace = true;
        _lrArc.widthMultiplier = 0.08f;
        _lrArc.material = new Material(Shader.Find("Sprites/Default"));
        _lrArc.startColor = _lrArc.endColor = new Color(1f, 0.4f, 0.2f, 0.9f);

        _lrGuide = new GameObject("SlashGuide").AddComponent<LineRenderer>();
        _lrGuide.transform.SetParent(transform, worldPositionStays: false);
        _lrGuide.positionCount = 2;
        _lrGuide.useWorldSpace = true;
        _lrGuide.widthMultiplier = 0.06f;
        _lrGuide.material = new Material(Shader.Find("Sprites/Default"));
        _lrGuide.startColor = _lrGuide.endColor = new Color(1f, 0.9f, 0.2f, 0.95f);

        _tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _tip.name = "SlashTip";
        _tip.transform.SetParent(transform);
        _tip.transform.localScale = Vector3.one * 0.18f;
        var col = _tip.GetComponent<Collider>();
        if (col) col.enabled = false;

        var mr = _tip.GetComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Standard"));
        mr.material.color = new Color(1f, 0.9f, 0.2f, 0.95f);
    }

    void Update()
    {
        _t += Time.deltaTime;
        if (_t > _life) { Cleanup(); return; }

        float y = transform.position.y;
        Vector3 center = new Vector3(transform.position.x, y + 0.02f, transform.position.z);

        float half = _arcDegrees * 0.5f;
        Quaternion left = Quaternion.Euler(0f, -half, 0f) * transform.rotation;

        for (int i = 0; i <= SEGMENTS; i++)
        {
            float a = (i / (float)SEGMENTS) * _arcDegrees;
            Quaternion rot = Quaternion.Euler(0f, a, 0f) * left;
            Vector3 p = center + (rot * Vector3.forward) * _radius;
            _lrArc.SetPosition(i, p);
        }

        Vector3 start = center;
        Vector3 end = center + transform.forward * _radius;
        _lrGuide.SetPosition(0, start);
        _lrGuide.SetPosition(1, end);

        if (_tip) _tip.transform.position = end;

        Vector3 p1 = new Vector3(transform.position.x, y + 0.1f, transform.position.z);
        Vector3 p2 = new Vector3(transform.position.x, y + 0.1f + _height, transform.position.z);
        Collider[] cols = Physics.OverlapCapsule(p1, p2, _radius, _playerMask);

        foreach (var c in cols)
        {
            GameObject root = c.attachedRigidbody ? c.attachedRigidbody.gameObject : c.transform.root.gameObject;
            if (root == null) root = c.gameObject;
            if (!root.CompareTag(_playerTag) && !c.CompareTag(_playerTag)) continue;

            Vector3 to = root.transform.position - transform.position;
            to.y = 0f;

            if (Vector3.Angle(transform.forward, to) > half) continue;
            if (to.magnitude > _radius + 0.1f) continue;

            int id = root.GetInstanceID();
            if (_damagedTargets.Contains(id)) continue;

            _damagedTargets.Add(id);
            _owner.TryDamagePlayerGO(root, _damage);
            Log($"Hit player (id={id})");
        }
    }

    void Cleanup()
    {
        if (_lrGuide) Destroy(_lrGuide.gameObject);
        if (_tip) Destroy(_tip);
        _damagedTargets.Clear();
        Destroy(gameObject);
    }
}