using UnityEngine;

// Throws the dagger forward — spins, damages on the way out, returns and can hit again.
public class DaggerWeapon : Weapon
{
    public float damage     = 18f;
    public float throwSpeed = 20f;

    public override float Cooldown    => 1.0f;
    public override float CombatRange => 7f;

    static readonly Color BladeColor  = new Color(1.00f, 0.95f, 0.60f);
    static readonly Color HandleColor = new Color(0.40f, 0.25f, 0.10f);
    static readonly Color WrapColor   = new Color(0.70f, 0.15f, 0.15f);

    Transform daggerRoot;
    bool      inFlight;

    protected override void BuildVisuals()
    {
        daggerRoot = new GameObject("DaggerRoot").transform;
        daggerRoot.SetParent(transform);

        MakePart("Blade",  daggerRoot, new Vector3(0,  0.28f, 0), new Vector3(0.10f, 0.55f, 1), 0, BladeColor);
        MakePart("Guard",  daggerRoot, new Vector3(0,  0.00f, 0), new Vector3(0.30f, 0.07f, 1), 0, BladeColor);
        MakePart("Handle", daggerRoot, new Vector3(0, -0.18f, 0), new Vector3(0.09f, 0.28f, 1), 0, HandleColor);
        MakePart("Wrap",   daggerRoot, new Vector3(0, -0.12f, 0), new Vector3(0.11f, 0.10f, 1), 0, WrapColor);

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
        daggerRoot.localPosition = new Vector3(f * 0.30f, 0.10f, -0.05f);
        daggerRoot.localScale    = new Vector3(f, 1f, 1f);
    }

    protected override void DoAttack()
    {
        if (inFlight) return;
        inFlight = true;
        SetVisible(false);

        float   facing = owner.transform.localScale.x;
        Vector2 spawn  = (Vector2)owner.transform.position + new Vector2(facing * 0.4f, 0.15f);

        var go = new GameObject("ThrownDagger");
        go.transform.position   = spawn;
        go.transform.localScale = Vector3.one * 0.38f;

        MakePart("Blade",  go.transform, new Vector3(0,  0.28f, 0), new Vector3(0.10f, 0.55f, 1), 0, BladeColor,  4);
        MakePart("Guard",  go.transform, new Vector3(0,  0.00f, 0), new Vector3(0.30f, 0.07f, 1), 0, BladeColor,  4);
        MakePart("Handle", go.transform, new Vector3(0, -0.18f, 0), new Vector3(0.09f, 0.28f, 1), 0, HandleColor, 4);
        MakePart("Wrap",   go.transform, new Vector3(0, -0.12f, 0), new Vector3(0.11f, 0.10f, 1), 0, WrapColor,   4);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.45f;

        var tw = go.AddComponent<ThrownWeapon>();
        tw.damage      = damage;
        tw.speed       = throwSpeed;
        tw.returnDelay = 0.22f;
        tw.spinSpeed   = 900f;
        tw.ownerRoot   = owner.transform;
        tw.onCaught    = () => { if (this != null) { inFlight = false; SetVisible(true); } };

        rb.linearVelocity = new Vector2(facing * throwSpeed, 0f);
        Destroy(go, 6f);
    }

    void SetVisible(bool v)
    {
        if (daggerRoot == null) return;
        foreach (var sr in daggerRoot.GetComponentsInChildren<SpriteRenderer>())
            sr.enabled = v;
    }
}
