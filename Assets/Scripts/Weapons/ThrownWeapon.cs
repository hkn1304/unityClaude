using System;
using UnityEngine;

// Generic throw-and-return projectile used by Dagger and Katana.
// Flies outbound, damages on contact, reverses, damages again on return, then gets caught.
[RequireComponent(typeof(Rigidbody2D))]
public class ThrownWeapon : MonoBehaviour
{
    public float  damage;
    public float  speed       = 18f;
    public float  returnDelay = 0.22f;  // seconds before turning back
    public float  spinSpeed   = 720f;   // deg/sec
    public Transform ownerRoot;
    public Action onCaught;             // called when caught by owner

    enum Phase { Outbound, Returning }
    Phase phase = Phase.Outbound;
    float timer;
    bool  hitOut, hitReturn;
    Rigidbody2D rb;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        transform.Rotate(0f, 0f, spinSpeed * Time.fixedDeltaTime);

        if (phase == Phase.Outbound && timer > returnDelay)
            phase = Phase.Returning;

        if (phase == Phase.Returning)
        {
            if (ownerRoot == null) { Destroy(gameObject); return; }
            Vector2 target  = (Vector2)ownerRoot.position + Vector2.up * 0.3f;
            Vector2 toOwner = (target - (Vector2)transform.position).normalized;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, toOwner * speed, 0.18f);

            if (Vector2.Distance(transform.position, ownerRoot.position) < 0.8f)
            {
                onCaught?.Invoke();
                Destroy(gameObject);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (ownerRoot != null &&
            (other.transform == ownerRoot || other.transform.IsChildOf(ownerRoot))) return;

        var fc = other.GetComponentInParent<FighterController>();
        if (fc == null) return;

        if (phase == Phase.Outbound && !hitOut)
        {
            hitOut = true;
            fc.GetComponent<FighterHealth>()?.TakeDamage(damage, transform.position);
            phase  = Phase.Returning;
        }
        else if (phase == Phase.Returning && !hitReturn)
        {
            hitReturn = true;
            fc.GetComponent<FighterHealth>()?.TakeDamage(damage, transform.position);
        }
    }
}
