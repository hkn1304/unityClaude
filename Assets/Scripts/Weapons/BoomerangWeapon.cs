using UnityEngine;
using UnityEngine.InputSystem;

// Player: hold J to charge range/power, release to throw.
// AI: press J for a standard mid-power throw.
public class BoomerangWeapon : Weapon
{
    public float baseDamage = 16f;
    public float baseSpeed  = 11f;

    public override float Cooldown    => 2.0f;
    public override float CombatRange => 9f;

    const float MaxCharge      = 1.5f;
    const float MaxSpeedBonus  = 10f;
    const float MaxDmgBonus    = 16f;
    const float BaseOutbound   = 0.55f;
    const float MaxOutbound    = 1.35f;

    static readonly Color WoodColor    = new Color(0.65f, 0.40f, 0.15f);
    static readonly Color BandColor    = new Color(0.92f, 0.72f, 0.26f);
    static readonly Color ChargedColor = new Color(1.00f, 0.50f, 0.05f);

    Transform      bRoot;
    SpriteRenderer arm1SR, arm2SR;

    bool  charging;
    float chargeStart;
    float lastThrowTime = -99f;

    protected override void BuildVisuals()
    {
        bRoot = new GameObject("BoomerangRoot").transform;
        bRoot.SetParent(transform);

        var a1 = MakePart("Arm1", bRoot, new Vector3( 0.13f,  0.10f, 0f), new Vector3(0.10f, 0.50f, 1f),  26f, WoodColor);
        var a2 = MakePart("Arm2", bRoot, new Vector3(-0.13f,  0.10f, 0f), new Vector3(0.10f, 0.50f, 1f), -26f, WoodColor);
        MakePart("Band",  bRoot, new Vector3( 0.00f, -0.10f, 0f), new Vector3(0.16f, 0.07f, 1f),   0f, BandColor);
        arm1SR = a1.GetComponent<SpriteRenderer>();
        arm2SR = a2.GetComponent<SpriteRenderer>();

        UpdateRoot();
    }

    // AI: normal throw via base cooldown system
    public override bool TryAttack()
    {
        if (owner != null && owner.isPlayerControlled) return false;
        return base.TryAttack();
    }

    void Update()
    {
        if (owner != null) UpdateRoot();
        HandleCharge();
    }

    void UpdateRoot()
    {
        float f = owner.transform.localScale.x;
        bRoot.localPosition = new Vector3(f * 0.30f, 0.14f, -0.05f);
        if (!charging)
            bRoot.localScale = new Vector3(f, 1f, 1f);
    }

    void HandleCharge()
    {
        if (owner == null || !owner.isPlayerControlled) return;

        if (owner.inputFrozen)
        {
            if (charging) { charging = false; ResetVisual(); }
            return;
        }

        var kb  = Keyboard.current;
        if (kb == null) return;
        var key = FighterController.Map(owner.punchKey);
        if (key == Key.None) return;

        if (kb[key].wasPressedThisFrame && !charging && Time.time >= lastThrowTime + Cooldown)
        {
            charging    = true;
            chargeStart = Time.time;
        }

        if (!charging) return;

        float t = Mathf.Clamp01((Time.time - chargeStart) / MaxCharge);
        UpdateChargeVisual(t);

        if (kb[key].wasReleasedThisFrame)
        {
            charging      = false;
            lastThrowTime = Time.time;
            Throw(t);
            ResetVisual();
        }
    }

    void UpdateChargeVisual(float t)
    {
        Color c = Color.Lerp(WoodColor, ChargedColor, t);
        if (arm1SR) arm1SR.color = c;
        if (arm2SR) arm2SR.color = c;
        float f     = owner.transform.localScale.x;
        float scale = 1f + t * 0.30f;
        bRoot.localScale = new Vector3(f * scale, scale, 1f);
    }

    void ResetVisual()
    {
        if (arm1SR) arm1SR.color = WoodColor;
        if (arm2SR) arm2SR.color = WoodColor;
    }

    // AI fires at chargeT=0.5 (decent throw without full charge)
    protected override void DoAttack() => Throw(0.5f);

    void Throw(float chargeT)
    {
        float facing   = owner.transform.localScale.x;
        Vector2 spawn  = (Vector2)owner.transform.position + new Vector2(facing * 0.5f, 0.3f);

        float spd  = baseSpeed  + chargeT * MaxSpeedBonus;
        float dmg  = baseDamage + chargeT * MaxDmgBonus;
        float obt  = Mathf.Lerp(BaseOutbound, MaxOutbound, chargeT);

        var go = new GameObject("Boomerang");
        go.transform.position   = spawn;
        go.transform.localScale = Vector3.one * 0.30f;

        Color armCol = Color.Lerp(WoodColor, ChargedColor, chargeT);
        MakePart("Arm1", go.transform, new Vector3( 0.13f,  0.10f, 0f), new Vector3(0.10f, 0.50f, 1f),  26f, armCol, 4);
        MakePart("Arm2", go.transform, new Vector3(-0.13f,  0.10f, 0f), new Vector3(0.10f, 0.50f, 1f), -26f, armCol, 4);
        MakePart("Band", go.transform, new Vector3( 0.00f, -0.10f, 0f), new Vector3(0.16f, 0.07f, 1f),   0f, BandColor, 4);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.5f;

        var bp = go.AddComponent<BoomerangProjectile>();
        bp.damage       = dmg;
        bp.speed        = spd;
        bp.ownerRoot    = owner.transform;
        bp.outboundTime = obt;

        float arc = Mathf.Lerp(2.5f, 5.5f, chargeT);
        rb.linearVelocity = new Vector2(facing * spd, arc);
        Destroy(go, 7f);
    }
}
