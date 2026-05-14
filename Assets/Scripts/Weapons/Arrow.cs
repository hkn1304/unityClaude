using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Arrow : MonoBehaviour
{
    public float     damage    = 22f;
    public Transform ownerRoot;

    Rigidbody2D rb;
    bool        stuck;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void Update()
    {
        if (stuck) return;

        // Rotate arrow tip to match velocity direction
        if (rb.linearVelocity.sqrMagnitude > 0.3f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.x, rb.linearVelocity.y) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, -angle);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (stuck) return;
        if (ownerRoot != null &&
            (other.transform == ownerRoot || other.transform.IsChildOf(ownerRoot))) return;

        var fc = other.GetComponentInParent<FighterController>();
        if (fc != null)
        {
            fc.GetComponent<FighterHealth>()?.TakeDamage(damage, transform.position);
            StickTo(fc.transform);
            return;
        }

        if (other.CompareTag("Ground"))
            StickTo(other.transform);
    }

    void StickTo(Transform target)
    {
        stuck                    = true;
        rb.linearVelocity        = Vector2.zero;
        rb.gravityScale          = 0f;
        rb.bodyType              = RigidbodyType2D.Kinematic;
        transform.SetParent(target);
        var col = GetComponent<CapsuleCollider2D>();
        if (col) col.enabled = false;
        Destroy(gameObject, 3f);
    }
}
