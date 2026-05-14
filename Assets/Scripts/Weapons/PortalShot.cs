using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PortalShot : MonoBehaviour
{
    public Action<Vector2> OnLand;
    public Transform       ownerRoot;

    bool landed;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (landed) return;
        if (ownerRoot != null &&
            (other.transform == ownerRoot || other.transform.IsChildOf(ownerRoot))) return;

        // Stop on any solid surface; pass straight through fighters
        var fc = other.GetComponentInParent<FighterController>();
        if (fc != null) return;

        landed = true;
        OnLand?.Invoke(transform.position);
        Destroy(gameObject);
    }
}
