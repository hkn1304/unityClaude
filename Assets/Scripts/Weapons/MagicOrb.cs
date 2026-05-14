using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MagicOrb : MonoBehaviour
{
    public float     damage    = 20f;
    public float     speed     = 8f;
    public Transform ownerRoot;
    public Transform target;

    Rigidbody2D rb;
    SpriteRenderer[] renderers;
    float lifetime;

    void Awake()
    {
        rb        = GetComponent<Rigidbody2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    public void Launch(Vector2 direction)
    {
        rb.linearVelocity = direction.normalized * speed;
    }

    void FixedUpdate()
    {
        if (target == null) return;

        // Gentle homing
        Vector2 toTarget = ((Vector2)target.position - (Vector2)transform.position).normalized;
        Vector2 current  = rb.linearVelocity.normalized;
        rb.linearVelocity = Vector2.Lerp(current, toTarget, 0.06f) * speed;
    }

    void Update()
    {
        // Pulse glow
        lifetime += Time.deltaTime * 8f;
        float pulse = 0.75f + Mathf.Sin(lifetime) * 0.25f;
        foreach (var r in renderers) r.color = new Color(r.color.r, r.color.g, r.color.b, pulse);

        // Spin
        transform.Rotate(0f, 0f, 180f * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (ownerRoot != null &&
            (other.transform == ownerRoot || other.transform.IsChildOf(ownerRoot))) return;

        var fc = other.GetComponentInParent<FighterController>();
        if (fc != null)
        {
            fc.GetComponent<FighterHealth>()?.TakeDamage(damage, transform.position);
            SpawnBurst();
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Ground"))
        {
            SpawnBurst();
            Destroy(gameObject);
        }
    }

    void SpawnBurst()
    {
        // Simple particle burst: spawn 6 tiny orbs that fade out
        for (int i = 0; i < 6; i++)
        {
            var p = new GameObject("OrbParticle");
            p.transform.position = transform.position;
            p.transform.localScale = Vector3.one * 0.15f;

            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = GetComponentInChildren<SpriteRenderer>()?.sprite;
            sr.color  = GetComponentInChildren<SpriteRenderer>()?.color ?? Color.magenta;
            sr.sortingOrder = 4;

            var rb2 = p.AddComponent<Rigidbody2D>();
            rb2.gravityScale = 0.5f;
            float angle = i * 60f * Mathf.Deg2Rad;
            rb2.linearVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 3f;

            Destroy(p, 0.4f);
        }
    }
}
