using UnityEngine;

// Throws the katana — glows cyan in flight, damages out and on return.
public class KatanaWeapon : Weapon
{
    public float damage     = 28f;
    public float throwSpeed = 22f;

    public override float Cooldown    => 1.2f;
    public override float CombatRange => 8f;

    static readonly Color BladeColor  = new Color(0.88f, 0.96f, 1.00f);
    static readonly Color GuardColor  = new Color(0.20f, 0.20f, 0.25f);
    static readonly Color HandleColor = new Color(0.10f, 0.48f, 0.18f);
    static readonly Color GlowColor   = new Color(0.40f, 0.90f, 1.00f);  // cyan in flight

    Transform katanaRoot;
    bool      inFlight;

    protected override void BuildVisuals()
    {
        katanaRoot = new GameObject("KatanaRoot").transform;
        katanaRoot.SetParent(transform);

        MakePart("Blade",  katanaRoot, new Vector3(0f,  0.58f, 0f), new Vector3(0.055f, 1.16f, 1f), 0f, BladeColor);
        MakePart("Guard",  katanaRoot, new Vector3(0f,  0.02f, 0f), new Vector3(0.18f,  0.05f, 1f), 0f, GuardColor);
        MakePart("Handle", katanaRoot, new Vector3(0f, -0.24f, 0f), new Vector3(0.052f, 0.38f, 1f), 0f, HandleColor);

        UpdateRoot();
    }

    public override bool TryAttack()
    {
        if (inFlight) return false;
        return base.TryAttack();
    }

    void Update()
    {
        if (owner != null && !inFlight) UpdateRoot();
    }

    void UpdateRoot()
    {
        float f = owner.transform.localScale.x;
        katanaRoot.localPosition = new Vector3(f * 0.36f, 0.14f, -0.05f);
        katanaRoot.localScale    = new Vector3(f, 1f, 1f);
    }

    protected override void DoAttack()
    {
        if (inFlight) return;
        inFlight = true;
        SetVisible(false);

        float   facing = owner.transform.localScale.x;
        Vector2 spawn  = (Vector2)owner.transform.position + new Vector2(facing * 0.5f, 0.15f);

        var go = new GameObject("ThrownKatana");
        go.transform.position   = spawn;
        go.transform.localScale = Vector3.one * 0.45f;

        // Blade glows cyan while in flight
        MakePart("Blade",  go.transform, new Vector3(0f,  0.58f, 0f), new Vector3(0.055f, 1.16f, 1f), 0f, GlowColor,   4);
        MakePart("Guard",  go.transform, new Vector3(0f,  0.02f, 0f), new Vector3(0.18f,  0.05f, 1f), 0f, GuardColor,  4);
        MakePart("Handle", go.transform, new Vector3(0f, -0.24f, 0f), new Vector3(0.052f, 0.38f, 1f), 0f, HandleColor, 4);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.55f;

        var tw = go.AddComponent<ThrownWeapon>();
        tw.damage      = damage;
        tw.speed       = throwSpeed;
        tw.returnDelay = 0.30f;
        tw.spinSpeed   = 540f;   // slower, more elegant rotation
        tw.ownerRoot   = owner.transform;
        tw.onCaught    = () => { if (this != null) { inFlight = false; SetVisible(true); } };

        rb.linearVelocity = new Vector2(facing * throwSpeed, 0f);
        Destroy(go, 6f);
    }

    void SetVisible(bool v)
    {
        if (katanaRoot == null) return;
        foreach (var sr in katanaRoot.GetComponentsInChildren<SpriteRenderer>())
            sr.enabled = v;
    }
}
