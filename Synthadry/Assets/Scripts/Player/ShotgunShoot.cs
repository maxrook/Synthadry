using UnityEngine;

public class Shotgun : MonoBehaviour
{
    [Header("��������� ���������")]
    public float damage = 25f;
    public float range = 50f;
    public float coneAngle = 20f; // ���� ������ � ��������
    public float fireRate = 1f; // ��������� � �������
    public Transform barrelEnd; // �����, �� ������� �������� ������� (����� ���������)
    public Camera playerCamera; // ������ ������ (��� ������������)
    public LayerMask targetLayers; // ����, ������� ��������� �������
    public int coneVertices = 8; // ����� ������ ��� ������������ ������
    public Vector3 positionOffset; // �������� ������� ��������� �� ������
    public Vector3 rotationOffset; // �������� �������� ��������� �� ������

    [Header("�������")]
    public GameObject muzzleFlashPrefab;
    public GameObject hitEffectPrefab;
    public AudioClip shotSound;
    private AudioSource audioSource;

    private float nextTimeToFire = 0f;

    void Start()
    {
        if (barrelEnd == null)
        {
            // Debug.LogError("�� ��������� ����� barrelEnd!");
            enabled = false;
            return;
        }

        if (playerCamera == null)
        {
            // Debug.LogError("�� ��������� ������ ������!");
            enabled = false;
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.clip = shotSound; // ���� ��������
    }

    void Update()
    {
        if (Input.GetButtonDown("Fire1") && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + 1f / fireRate;
            Shoot();
        }
    }


    void LateUpdate()
    {
        // ��������� ������� � �������� ��������� � ������������ � �������
        transform.position = playerCamera.transform.position + playerCamera.transform.rotation * positionOffset;
        transform.rotation = playerCamera.transform.rotation * Quaternion.Euler(rotationOffset);
    }

    void Shoot()
    {
        //������ ��������
        GameObject muzzleFlash = Instantiate(muzzleFlashPrefab, barrelEnd.position, barrelEnd.rotation);
        Destroy(muzzleFlash, 1f); // �������� ������� ����� ��������� �����

        // ��������������� ����� ��������
        audioSource.PlayOneShot(shotSound);

        // ����������� �� ������
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // ����� ������
        // ����������� ����������� ��������
        Vector3 shootDirection = ray.direction;

        // ����������� ������ � ������
        Collider[] hitColliders = Physics.OverlapSphere(barrelEnd.position, range, targetLayers); // ����� ����� � �������

        foreach (Collider collider in hitColliders)
        {
            // ���������� ���� � ������
            Vector3 targetDirection = (collider.transform.position - barrelEnd.position).normalized;
            float angleToTarget = Vector3.Angle(shootDirection, targetDirection);

            if (angleToTarget <= coneAngle / 2)
            {
                // ��������� �����
                // Debug.Log("������ �� �����: " + collider.name);
                // ���� �� � ���� ������ �������� ��� ������ ������ ��������� �����
                MobController targetHealth = collider.GetComponent<MobController>();
                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(damage);

                    // ������ ���������
                    GameObject hitEffect = Instantiate(hitEffectPrefab, collider.ClosestPoint(barrelEnd.position), Quaternion.identity);
                    Destroy(hitEffect, 1f);
                }
                else
                {
                    // Debug.LogWarning("� ���� " + collider.name + " ��� ������� Health ��� ������� ������� ��� ��������� �����!");
                }
            }
        }
    }


    // ������������ ������ � ���������
    void OnDrawGizmosSelected()
    {
        if (barrelEnd == null) return;

        Gizmos.color = Color.yellow;
        Matrix4x4 originalMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(barrelEnd.position, barrelEnd.rotation, Vector3.one);

        // ������ ��������
        float angleStep = 360f / coneVertices;
        float coneRadius = Mathf.Tan(coneAngle / 2 * Mathf.Deg2Rad) * range;

        Vector3 apex = Vector3.zero; // ������� �������� (��������� � barrelEnd)

        Vector3[] baseVertices = new Vector3[coneVertices];
        for (int i = 0; i < coneVertices; i++)
        {
            float angle = i * angleStep;
            baseVertices[i] = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * coneRadius, Mathf.Sin(angle * Mathf.Deg2Rad) * coneRadius, range);
        }

        // ������ ����� ��������
        for (int i = 0; i < coneVertices; i++)
        {
            Gizmos.DrawLine(apex, baseVertices[i]);
            Gizmos.DrawLine(baseVertices[i], baseVertices[(i + 1) % coneVertices]); // ��������� ������� ���������
        }
        Gizmos.matrix = originalMatrix;
    }
}
