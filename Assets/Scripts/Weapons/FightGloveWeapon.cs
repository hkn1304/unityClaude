using System.Collections;
using UnityEngine;

// J = quick jab (14 dmg).  K = Hadouken fireball (30 dmg).
// AI uses hadouken at range, jab up close.
public class FightGloveWeapon : Weapon
{
    public float punchDamage    = 14f;
    public float hadoukenDamage = 30f;
    public float hadoukenSpeed  = 16f;

    public override float Cooldown          => 0.30f;
    public override float SecondaryCooldown => 1.00f;
    public override float CombatRange       => 5f;

    static readonly Color GloveRed  = new Color(0.88f, 0.10f, 0.10f);
    static readonly Color GloveDark = new Color(0.48f, 0.05f, 0.05f);
    static readonly Color WrapBeige = new Color(0.78f, 0.62f, 0.38f);

    Transform rightGlove;
    Transform leftGlove;

    bool    punching;
    bool    hitThisPunch;
    Vector3 rRest;
    Vector3 lRest;

    protected override void BuildVisuals()
    {
        rightGlove = MakeGlove("RightGlove", 1.00f);
        leftGlove  = MakeGlove("LeftGlove",  0.80f);
        UpdateGloves();
    }

    Transform MakeGlove(string name, float s)
    {
        var root = new GameObject(name).transform;
        root.SetParent(transform);
        MakePart("Knuckle", root, new Vector3(0f,      0.06f,  0f), new Vector3(0.30f * s, 0.24f * s, 1f), 0f, GloveRed,  3);
        MakePart("Cuff",    root, new Vector3(0f,     -0.14f * s, 0f), new Vector3(0.24f * s, 0.12f * s, 1f), 0f, GloveDark, 3);
        MakePart("Wrap",    root, new Vector3(0f,     -0.09f * s, 0f), new Vector3(0.26f * s, 0.04f * s, 1f), 0f, WrapBeige, 4);
        MakePart("Divider", root, new Vector3(0f,      0.05f,  0f), new Vector3(0.03f * s, 0.20f * s, 1f), 0f, GloveDark, 4);
        return root;
    }

    void Update()
    {
        if (!punching) UpdateGloves();
    }

    void UpdateGloves()
    {
        if (owner == null) return;
        float f = owner.transform.localScale.x;

        rRest = new Vector3( f * 0.40f, 0.20f, -0.05f);
        lRest = new Vector3(-f * 0.26f, 0.17f, -0.04f);

        rightGlove.localPosition = rRest;
        rightGlove.localScale    = new Vector3(f, 1f, 1f);
        leftGlove.localPosition  = lRest;
        leftGlove.localScale     = new Vector3(f, 1f, 1f);
    }

    // AI: hadouken at range, jab up close
    public override bool TryAttack()
    {
        if (owner != null && !owner.isPlayerControlled && owner.opponent != null)
        {
            float dist = Vector2.Distance(owner.transform.position, owner.opponent.position);
            if (dist > 2.5f) return TrySecondaryAttack();
        }
        return base.TryAttack();
    }

    protected override void DoAttack()
    {
        if (!punching) StartCoroutine(PunchAnim());
    }

    IEnumerator PunchAnim()
    {
        punching     = true;
        hitThisPunch = false;
        float facing = owner.transform.localScale.x;

        Vector3 lunge = rRest + new Vector3(facing * 0.52f,  0.02f, 0f);
        Vector3 lBack = lRest + new Vector3(-facing * 0.08f, 0.00f, 0f);

        const float outDur = 0.07f;
        const float retDur = 0.10f;

        for (float t = 0; t < outDur; t += Time.deltaTime)
        {
            float p = t / outDur;
            rightGlove.localPosition = Vector3.Lerp(rRest, lunge, p);
            leftGlove.localPosition  = Vector3.Lerp(lRest, lBack, p);
            HitScan(facing);
            yield return null;
        }
        rightGlove.localPosition = lunge;
        HitScan(facing);

        for (float t = 0; t < retDur; t += Time.deltaTime)
        {
            float p = t / retDur;
            rightGlove.localPosition = Vector3.Lerp(lunge, rRest, p);
            leftGlove.localPosition  = Vector3.Lerp(lBack, lRest, p);
            yield return null;
        }
        rightGlove.localPosition = rRest;
        leftGlove.localPosition  = lRest;
        punching = false;
    }

    void HitScan(float facing)
    {
        if (hitThisPunch) return;
        Vector2 origin = (Vector2)owner.transform.position + new Vector2(facing * 0.70f, 0.15f);
        foreach (var c in Physics2D.OverlapCircleAll(origin, 0.55f))
        {
            var fc = c.GetComponentInParent<FighterController>();
            if (fc == null || fc == owner) continue;
            hitThisPunch = true;
            fc.GetComponent<FighterHealth>()?.TakeDamage(punchDamage, rightGlove.position);
            SpawnHitSpark(rightGlove.position);
            break;
        }
    }

    void SpawnHitSpark(Vector3 pos)
    {
        var s = new GameObject("HitSpark");
        s.transform.position   = pos;
        s.transform.localScale = Vector3.one * 0.36f;
        var sr = s.AddComponent<SpriteRenderer>();
        sr.sprite       = RectSprite();
        sr.color        = new Color(1f, 0.90f, 0.15f);
        sr.sortingOrder = 8;
        Destroy(s, 0.07f);
    }

    // ── Hadouken ─────────────────────────────────────────────────────────────

    protected override void DoSecondaryAttack() => StartCoroutine(HadoukenAnim());

    IEnumerator HadoukenAnim()
    {
        float facing = owner.transform.localScale.x;

        Vector3 rBack = rRest + new Vector3(-facing * 0.24f, -0.04f, 0f);
        Vector3 lBack = lRest + new Vector3(-facing * 0.18f, -0.04f, 0f);
        Vector3 rFwd  = rRest + new Vector3( facing * 0.28f,  0.04f, 0f);
        Vector3 lFwd  = lRest + new Vector3( facing * 0.18f,  0.04f, 0f);

        const float windUp = 0.14f;
        const float thrust = 0.10f;
        const float ret    = 0.15f;

        for (float t = 0; t < windUp; t += Time.deltaTime)
        {
            float p = t / windUp;
            rightGlove.localPosition = Vector3.Lerp(rRest, rBack, p);
            leftGlove.localPosition  = Vector3.Lerp(lRest, lBack, p);
            yield return null;
        }
        for (float t = 0; t < thrust; t += Time.deltaTime)
        {
            float p = t / thrust;
            rightGlove.localPosition = Vector3.Lerp(rBack, rFwd, p);
            leftGlove.localPosition  = Vector3.Lerp(lBack, lFwd, p);
            yield return null;
        }

        SpawnHadouken(facing);

        for (float t = 0; t < ret; t += Time.deltaTime)
        {
            float p = t / ret;
            rightGlove.localPosition = Vector3.Lerp(rFwd, rRest, p);
            leftGlove.localPosition  = Vector3.Lerp(lFwd, lRest, p);
            yield return null;
        }
        rightGlove.localPosition = rRest;
        leftGlove.localPosition  = lRest;
    }

    void SpawnHadouken(float facing)
    {
        Vector2 spawnPos = (Vector2)owner.transform.position + new Vector2(facing * 0.8f, 0.18f);

        var go = new GameObject("Hadouken");
        go.transform.position   = spawnPos;
        go.transform.localScale = Vector3.one;

        // Layered orb: outer glow → mid ring → spinning arcs → bright core
        MakePart("GlowOuter", go.transform, Vector3.zero,              new Vector3(0.58f, 0.58f, 1f),  0f, new Color(1.0f, 0.55f, 0.05f), 3);
        MakePart("GlowMid",   go.transform, Vector3.zero,              new Vector3(0.40f, 0.40f, 1f),  0f, new Color(1.0f, 0.28f, 0.00f), 4);
        MakePart("Arc1",      go.transform, new Vector3( 0.10f, 0, 0), new Vector3(0.14f, 0.44f, 1f),  0f, new Color(1.0f, 0.80f, 0.05f), 5);
        MakePart("Arc2",      go.transform, new Vector3(-0.10f, 0, 0), new Vector3(0.14f, 0.44f, 1f),  0f, new Color(1.0f, 0.80f, 0.05f), 5);
        MakePart("Core",      go.transform, Vector3.zero,              new Vector3(0.20f, 0.20f, 1f),  0f, new Color(1.0f, 0.96f, 0.82f), 6);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.26f;

        var h = go.AddComponent<Hadouken>();
        h.damage    = hadoukenDamage;
        h.ownerRoot = owner.transform;

        rb.linearVelocity = new Vector2(facing * hadoukenSpeed, 0f);
        Destroy(go, 3.5f);
    }
}
