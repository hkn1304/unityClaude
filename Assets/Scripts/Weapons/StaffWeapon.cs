using System.Collections;
using UnityEngine;

public class StaffWeapon : Weapon
{
    public float orbDamage = 20f;
    public float orbSpeed  = 8f;

    public override float Cooldown    => 1.0f;
    public override float CombatRange => 6f;

    static readonly Color StaffColor  = new Color(0.45f, 0.20f, 0.70f);
    static readonly Color BandColor   = new Color(0.80f, 0.65f, 0.10f);
    static readonly Color OrbColor    = new Color(0.85f, 0.40f, 1.00f);

    Transform staffRoot;
    Transform orbGlow;

    protected override void BuildVisuals()
    {
        staffRoot = new GameObject("StaffRoot").transform;
        staffRoot.SetParent(transform);

        // Shaft
        MakePart("Shaft", staffRoot, new Vector3(0,  0.20f, 0), new Vector3(0.10f, 1.20f, 1), 0, StaffColor);
        // Bands
        MakePart("Band1", staffRoot, new Vector3(0,  0.55f, 0), new Vector3(0.16f, 0.07f, 1), 0, BandColor);
        MakePart("Band2", staffRoot, new Vector3(0, -0.12f, 0), new Vector3(0.16f, 0.07f, 1), 0, BandColor);
        // Orb at tip
        var orbGO = MakePart("OrbGlow", staffRoot, new Vector3(0, 0.90f, 0), new Vector3(0.28f, 0.28f, 1), 0, OrbColor, 3);
        orbGlow = orbGO.transform;

        UpdateRoot();
    }

    void Update()
    {
        if (owner == null) return;
        UpdateRoot();
        // Pulse orb glow
        if (orbGlow != null)
        {
            float s = 0.26f + Mathf.Sin(Time.time * 5f) * 0.04f;
            orbGlow.localScale = new Vector3(s, s, 1f);
            var sr = orbGlow.GetComponent<SpriteRenderer>();
            if (sr) sr.color = new Color(OrbColor.r, OrbColor.g, OrbColor.b,
                                         0.7f + Mathf.Sin(Time.time * 5f) * 0.3f);
        }
    }

    void UpdateRoot()
    {
        float f = owner.transform.localScale.x;
        staffRoot.localPosition = new Vector3(-f * 0.32f, 0.18f, -0.05f);
        staffRoot.localScale    = new Vector3(f, 1f, 1f);
    }

    protected override void DoAttack() => StartCoroutine(CastOrb());

    IEnumerator CastOrb()
    {
        // Charge flash
        if (orbGlow != null)
        {
            var sr = orbGlow.GetComponent<SpriteRenderer>();
            if (sr) sr.color = Color.white;
        }
        yield return new WaitForSeconds(0.12f);

        ShootOrb();
    }

    void ShootOrb()
    {
        float   facing   = owner.transform.localScale.x;
        Vector2 spawnPos = orbGlow != null
            ? (Vector2)orbGlow.position
            : (Vector2)owner.transform.position + new Vector2(facing * 0.4f, 0.9f);

        var go = new GameObject("MagicOrb");
        go.transform.position = spawnPos;
        go.transform.localScale = Vector3.one * 0.32f;

        // Visual — diamond shape (rotated square)
        var vis = MakePart("Core", go.transform, Vector3.zero, new Vector3(1f, 1f, 1f), 45f, OrbColor, 4);
        var ring = MakePart("Ring", go.transform, Vector3.zero, new Vector3(1.4f, 1.4f, 1f), 22f,
                            new Color(OrbColor.r, OrbColor.g, OrbColor.b, 0.5f), 3);

        var rb          = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        var col         = go.AddComponent<CircleCollider2D>();
        col.isTrigger   = true;
        col.radius      = 0.5f;

        var orb         = go.AddComponent<MagicOrb>();
        orb.damage      = orbDamage;
        orb.speed       = orbSpeed;
        orb.ownerRoot   = owner.transform;
        orb.target      = owner.opponent;

        orb.Launch(new Vector2(facing, 0f));
        Destroy(go, 5f);
    }
}
