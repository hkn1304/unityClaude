using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DamageProjectile : MonoBehaviour
{
    public float     damage;
    public float     spinSpeed;    // deg/sec, 0 = no spin
    public bool      sticky;       // parent to enemy on hit instead of destroying
    public Transform ownerRoot;

    void Update()
    {
        if (spinSpeed != 0f) transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (ownerRoot != null &&
            (other.transform == ownerRoot || other.transform.IsChildOf(ownerRoot))) return;

        var fc = other.GetComponentInParent<FighterController>();
        if (fc != null)
        {
            fc.GetComponent<FighterHealth>()?.TakeDamage(damage, transform.position);
            if (sticky)
            {
                var rb2 = GetComponent<Rigidbody2D>();
                if (rb2) { rb2.linearVelocity = Vector2.zero; rb2.bodyType = RigidbodyType2D.Kinematic; }
                GetComponent<Collider2D>().enabled = false;
                transform.SetParent(other.transform);
                Destroy(gameObject, 2.5f);
            }
            else
            {
                SpawnHitParticles();
                Destroy(gameObject);
            }
            return;
        }
        if (other.CompareTag("Ground"))
        {
            SpawnHitParticles();
            Destroy(gameObject);
        }
    }

    void SpawnHitParticles()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return;
        for (int i = 0; i < 4; i++)
        {
            var p   = new GameObject("HitParticle");
            p.transform.position   = transform.position;
            p.transform.localScale = Vector3.one * 0.1f;
            var psr = p.AddComponent<SpriteRenderer>();
            psr.sprite       = sr.sprite;
            psr.color        = sr.color;
            psr.sortingOrder = 5;
            var prb = p.AddComponent<Rigidbody2D>();
            prb.gravityScale = 0.3f;
            float a = i * 90f * Mathf.Deg2Rad;
            prb.linearVelocity = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 2.5f;
            Destroy(p, 0.3f);
        }
    }
}
