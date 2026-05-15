using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Player: hold J to charge the orb (grows, brightens), release to fire.
// AI: press J for a standard orb shot.
public class StaffWeapon : Weapon
{
    public float orbDamage = 20f;
    public float orbSpeed  = 8f;

    public override float Cooldown    => 1.0f;
    public override float CombatRange => 7f;

    const float MaxCharge      = 1.2f;
    const float MaxDamageBonus = 20f;   // 20 → 40
    const float MaxSpeedBonus  = 8f;    // 8  → 16
    const float MaxScaleBonus  = 0.26f; // 0.32 → 0.58

    static readonly Color StaffColor = new Color(0.45f, 0.20f, 0.70f);
    static readonly Color BandColor  = new Color(0.80f, 0.65f, 0.10f);
    static readonly Color OrbColor   = new Color(0.85f, 0.40f, 1.00f);

    Transform      staffRoot;
    Transform      orbGlow;
    SpriteRenderer orbGlowSR;

    bool  charging;
    float chargeStart;
    float lastFireTime = -99f;

    protected override void BuildVisuals()
    {
        staffRoot = new GameObject("StaffRoot").transform;
        staffRoot.SetParent(transform);

        MakePart("Shaft", staffRoot, new Vector3(0,  0.20f, 0), new Vector3(0.10f, 1.20f, 1), 0, StaffColor);
        MakePart("Band1", staffRoot, new Vector3(0,  0.55f, 0), new Vector3(0.16f, 0.07f, 1), 0, BandColor);
        MakePart("Band2", staffRoot, new Vector3(0, -0.12f, 0), new Vector3(0.16f, 0.07f, 1), 0, BandColor);

        var orbGO = MakePart("OrbGlow", staffRoot, new Vector3(0, 0.90f, 0), new Vector3(0.28f, 0.28f, 1), 0, OrbColor, 3);
        orbGlow   = orbGO.transform;
        orbGlowSR = orbGO.GetComponent<SpriteRenderer>();

        UpdateRoot();
    }

    public override bool TryAttack()
    {
        if (owner != null && owner.isPlayerControlled) return false;
        return base.TryAttack();
    }

    void Update()
    {
        if (owner == null) return;
        UpdateRoot();
        if (!charging) PulseOrb();
        HandleCharge();
    }

    void UpdateRoot()
    {
        float f = owner.transform.localScale.x;
        staffRoot.localPosition = new Vector3(-f * 0.32f, 0.18f, -0.05f);
        staffRoot.localScale    = new Vector3(f, 1f, 1f);
    }

    void PulseOrb()
    {
        if (orbGlow == null) return;
        float s = 0.26f + Mathf.Sin(Time.time * 5f) * 0.04f;
        orbGlow.localScale = new Vector3(s, s, 1f);
        if (orbGlowSR) orbGlowSR.color = new Color(OrbColor.r, OrbColor.g, OrbColor.b,
                                                    0.7f + Mathf.Sin(Time.time * 5f) * 0.3f);
    }

    void HandleCharge()
    {
        if (!owner.isPlayerControlled) return;

        if (owner.inputFrozen)
        {
            if (charging) { charging = false; PulseOrb(); }
            return;
        }

        var kb  = Keyboard.current;
        if (kb == null) return;
        var key = FighterController.Map(owner.punchKey);
        if (key == Key.None) return;

        if (kb[key].wasPressedThisFrame && !charging && Time.time >= lastFireTime + Cooldown)
        {
            charging    = true;
            chargeStart = Time.time;
        }

        if (!charging) return;

        float t = Mathf.Clamp01((Time.time - chargeStart) / MaxCharge);
        UpdateChargeVisual(t);

        if (kb[key].wasReleasedThisFrame)
        {
            charging     = false;
            lastFireTime = Time.time;
            ShootOrb(orbDamage + t * MaxDamageBonus,
                     orbSpeed  + t * MaxSpeedBonus,
                     0.32f     + t * MaxScaleBonus);
        }
    }

    void UpdateChargeVisual(float t)
    {
        if (orbGlow == null) return;
        float s = Mathf.Lerp(0.28f, 0.58f, t);
        orbGlow.localScale = new Vector3(s, s, 1f);
        if (orbGlowSR)
            orbGlowSR.color = Color.Lerp(OrbColor, Color.white, t * 0.7f);
    }

    protected override void DoAttack() => StartCoroutine(CastOrb());

    IEnumerator CastOrb()
    {
        if (orbGlowSR) orbGlowSR.color = Color.white;
        yield return new WaitForSeconds(0.12f);
        ShootOrb(orbDamage, orbSpeed, 0.32f);
    }

    void ShootOrb(float dmg, float spd, float scale)
    {
        float   facing   = owner.transform.localScale.x;
        Vector2 spawnPos = orbGlow != null
            ? (Vector2)orbGlow.position
            : (Vector2)owner.transform.position + new Vector2(facing * 0.4f, 0.9f);

        var go = new GameObject("MagicOrb");
        go.transform.position   = spawnPos;
        go.transform.localScale = Vector3.one * scale;

        MakePart("Core", go.transform, Vector3.zero, new Vector3(1f, 1f, 1f), 45f, OrbColor, 4);
        MakePart("Ring", go.transform, Vector3.zero, new Vector3(1.4f, 1.4f, 1f), 22f,
                 new Color(OrbColor.r, OrbColor.g, OrbColor.b, 0.5f), 3);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        var col       = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.5f;

        var orb       = go.AddComponent<MagicOrb>();
        orb.damage    = dmg;
        orb.speed     = spd;
        orb.ownerRoot = owner.transform;
        orb.target    = owner.opponent;

        orb.Launch(new Vector2(facing, 0f));
        Destroy(go, 5f);
    }
}
