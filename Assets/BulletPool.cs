using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BulletPool – Object Pooling cho hiệu ứng đạn (tracer & impact spark).
/// ─── SETUP ───────────────────────────────────────────────────────────────────
/// 1. Thêm GameObject "BulletPool" vào Scene.
/// 2. Gán bulletTracerPrefab: một capsule/cube mỏng có Trail Renderer hoặc Line.
/// 3. Gán impactPrefab: một Particle System nhỏ (spark).
/// 4. poolSize mặc định 20 là đủ cho demo.
/// ─────────────────────────────────────────────────────────────────────────────
/// </summary>
public class BulletPool : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Prefab hiệu ứng vết đạn (tracer). Có thể là trail renderer nhỏ.")]
    public GameObject bulletTracerPrefab;

    [Tooltip("Prefab hiệu ứng va chạm (impact spark). Nên là Particle System.")]
    public GameObject impactPrefab;

    [Header("Settings")]
    public int poolSize = 20;

    // Internal pools
    private Queue<GameObject> _tracerPool = new Queue<GameObject>();
    private Queue<GameObject> _impactPool = new Queue<GameObject>();

    void Awake()
    {
        // Pre-warm: tạo sẵn objects, ẩn đi, đưa vào queue
        for (int i = 0; i < poolSize; i++)
        {
            if (bulletTracerPrefab != null)
            {
                var t = Instantiate(bulletTracerPrefab, transform);
                t.SetActive(false);
                _tracerPool.Enqueue(t);
            }

            if (impactPrefab != null)
            {
                var p = Instantiate(impactPrefab, transform);
                p.SetActive(false);
                _impactPool.Enqueue(p);
            }
        }

        Debug.Log($"[BulletPool] Pre-warmed {poolSize} tracers & {poolSize} impacts.");
    }

    /// <summary>Hiển thị hiệu ứng đạn từ origin theo direction.</summary>
    public void SpawnBulletEffect(Vector3 origin, Vector3 direction)
    {
        // ── Tracer ──
        if (_tracerPool.Count > 0)
        {
            var tracer = _tracerPool.Dequeue();
            tracer.transform.position = origin;
            tracer.transform.forward  = direction;
            tracer.SetActive(true);
            StartCoroutine(ReturnToPool(tracer, _tracerPool, 0.08f));
        }

        // ── Impact ──
        if (_impactPool.Count > 0 && Physics.Raycast(origin, direction, out RaycastHit hit, 150f))
        {
            var impact = _impactPool.Dequeue();
            impact.transform.position = hit.point;
            impact.transform.up       = hit.normal;
            impact.SetActive(true);

            // Reset particle nếu có
            var ps = impact.GetComponent<ParticleSystem>();
            ps?.Play();

            StartCoroutine(ReturnToPool(impact, _impactPool, 1.5f));
        }
    }

    private IEnumerator ReturnToPool(GameObject obj, Queue<GameObject> pool, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
