using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BoomerangProjectile : MonoBehaviour
{
    public float     damage   = 16f;
    public float     speed    = 11f;
    public Transform ownerRoot;

    enum Phase { Outbound, Returning }
    Phase phase  = Phase.Outbound;
    float timer;

    Rigidbody2D rb;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;

        if (phase == Phase.Outbound)
        {
            // Small upward arc on the way out
            rb.linearVelocity += new Vector2(0f, -0.5f);
            if (timer > 0.45f) phase = Phase.Returning;
        }
        else
        {
            if (ownerRoot == null) { Destroy(gameObject); return; }
            Vector2 toOwner = ((Vector2)ownerRoot.position + Vector2.up * 0.4f
                               - (Vector2)transform.position).normalized;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, toOwner * speed, 0.15f);

            // Caught by owner
            if (Vector2.Distance(transform.position, ownerRoot.position) < 0.9f)
                Destroy(gameObject);
        }

        // Spin visually
        transform.Rotate(0f, 0f, 600f * Time.fixedDeltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (ownerRoot != null &&
            (other.transform == ownerRoot || other.transform.IsChildOf(ownerRoot))) return;

        var fc = other.GetComponentInParent<FighterController>();
        if (fc != null)
        {
            fc.GetComponent<FighterHealth>()?.TakeDamage(damage, transform.position);
            if (phase == Phase.Returning)
                Destroy(gameObject);          // second hit destroys it
            else
                phase = Phase.Returning;      // first hit starts the return early
        }
    }
}
