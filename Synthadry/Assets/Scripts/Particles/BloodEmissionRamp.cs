using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class BloodEmissionInsideCollider : MonoBehaviour
{
    [System.Serializable]
    public class LetterColliders
    {
        public Collider[] colliders;
    }
    [Header("Основные настройки")]
    public ParticleSystem ps;
   [SerializeField] public LetterColliders[] letterColliders;
    public float targetRate = 200f;
    public float duration = 3f;
    public bool playOnStart = true;

    [Header("Параметры спавна")]
    public float jitter = 0.02f; // небольшой разброс
    public bool useLocalSpace = false; // если хочешь спавнить в локальных координатах

    private ParticleSystem.EmissionModule emission;
    private float elapsed = 0f;
    private bool isEmitting = false;

    void Awake()
    {
        if (ps == null) ps = GetComponent<ParticleSystem>();
        emission = ps.emission;
        emission.enabled = false; // отключаем стандартную эмиссию, всё делаем вручную
    }

    void Start()
    {
        if (playOnStart) StartEmission();
    }

    public void StartEmission()
    {
        if (letterColliders == null)
        {
            Debug.LogWarning("Spawn Collider не назначен!");
            return;
        }

        ps.Play();
        StopAllCoroutines();
        StartCoroutine(RampEmission());
    }

    IEnumerator RampEmission()
    {
        elapsed = 0f;
        isEmitting = true;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float rate = Mathf.Lerp(0, targetRate, t);
            yield return EmitParticles(rate * Time.deltaTime);
        }

        // постоянный уровень эмиссии после разгона
        while (isEmitting)
        {
            Debug.Log("emiiting " + targetRate * Time.deltaTime);
            yield return EmitParticles(targetRate * Time.deltaTime);
        }   
    }

    IEnumerator EmitParticles(float count)
    {
        int emitCount = Mathf.FloorToInt(count);
        for (int i = 0; i < emitCount; i++)
        {
            int k = UnityEngine.Random.Range(0, letterColliders.Length);
            var letter = letterColliders[k];
            int l = Random.Range(0, letter.colliders.Length);
            var collider = letter.colliders[l];
            Vector3 pos = RandomPointOnSurface(collider);
            if (useLocalSpace)
                pos = transform.InverseTransformPoint(pos);

            var emitParams = new ParticleSystem.EmitParams
            {
                position = pos,
            };
            ps.Emit(emitParams, 1);
        }
        yield return null;
    }
    public void StopEmission()
    {
        StopAllCoroutines();
        isEmitting = false;
        ps.Stop();
    }

    // ============================================================
    // Вспомогательная функция: случайная точка внутри 3D Collider
    // ============================================================
    Vector3 RandomPointInCollider(Collider col)
    {
        if (col is BoxCollider box)
        {
            Vector3 localPoint = new Vector3(
                Random.Range(-box.size.x / 2f, box.size.x / 2f),
                Random.Range(-box.size.y / 2f, box.size.y / 2f),
                Random.Range(-box.size.z / 2f, box.size.z / 2f)
            );
            return box.transform.TransformPoint(localPoint + box.center);
        }

        if (col is SphereCollider sphere)
        {
            Vector3 dir = Random.insideUnitSphere;
            float dist = Random.Range(0f, sphere.radius);
            return sphere.transform.TransformPoint(sphere.center + dir * dist);
        }

        if (col is CapsuleCollider capsule)
        {
            float height = Mathf.Max(0, capsule.height / 2f - capsule.radius);
            Vector3 axis = Vector3.up;
            switch (capsule.direction)
            {
                case 0: axis = Vector3.right; break;
                case 1: axis = Vector3.up; break;
                case 2: axis = Vector3.forward; break;
            }

            Vector3 point = Random.insideUnitSphere * capsule.radius;
            point += axis * Random.Range(-height, height);
            return capsule.transform.TransformPoint(point + capsule.center);
        }

        if (col is MeshCollider mesh && mesh.sharedMesh != null)
        {
            Bounds b = mesh.sharedMesh.bounds;
            int safety = 0;
            while (safety < 100)
            {
                safety++;
                Vector3 local = new Vector3(
                    Random.Range(b.min.x, b.max.x),
                    Random.Range(b.min.y, b.max.y),
                    Random.Range(b.min.z, b.max.z)
                );

                Vector3 world = mesh.transform.TransformPoint(local);
                if (PointInsideMeshCollider(mesh, world))
                    return world;
            }
            return mesh.bounds.center;
        }

        return col.bounds.center;
    }
    // Проверка для MeshCollider — работает, если mesh замкнутый
    bool PointInsideMeshCollider(MeshCollider mesh, Vector3 point)
    {
        var dir = Random.onUnitSphere;
        Ray ray = new Ray(point, dir);
        if (mesh.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            // если пересечение есть — считаем, что внутри
            return true;
        }
        return false;
    }
    Vector3 RandomPointOnSurface(Collider col)
    {
        Vector3 dir = Random.onUnitSphere;
        if (col.Raycast(new Ray(col.bounds.center - dir * 10f, dir), out RaycastHit hit, 100f))
            return hit.point;
        return col.bounds.center;
    }
}
