using System.Collections;
using UnityEngine;

// Stars STICK in enemies. After 3 throws the owner enters a brief invincible fireball state.
public class ShurikenWeapon : Weapon
{
    public float starDamage   = 12f;
    public float starSpeed    = 16f;
    public float fireballTime = 1.8f;

    public override float Cooldown    => 1.3f;
    public override float CombatRange => 7f;

    static readonly Color StarColor   = new Color(0.88f, 0.88f, 0.96f);
    static readonly Color HandleColor = new Color(0.28f, 0.28f, 0.34f);
    static readonly Color FireColor   = new Color(1.0f, 0.45f, 0.05f);

    Transform shurikenRoot;

    protected override void BuildVisuals()
    {
        shurikenRoot = new GameObject("ShurikenRoot").transform;
        shurikenRoot.SetParent(transform);

        // 3-spoke star
        MakePart("Spoke1", shurikenRoot, Vector3.zero, new Vector3(0.26f, 0.07f, 1f),   0f, StarColor);
        MakePart("Spoke2", shurikenRoot, Vector3.zero, new Vector3(0.26f, 0.07f, 1f),  60f, StarColor);
        MakePart("Spoke3", shurikenRoot, Vector3.zero, new Vector3(0.26f, 0.07f, 1f), -60f, StarColor);
        MakePart("Grip",   shurikenRoot, new Vector3(0f, -0.24f, 0f), new Vector3(0.06f, 0.18f, 1f), 0f, HandleColor);

        UpdateRoot();
    }

    void Update()
    {
        if (owner == null) return;
        UpdateRoot();
        shurikenRoot.Rotate(0f, 0f, 220f * Time.deltaTime);
    }

    void UpdateRoot()
    {
        float f = owner.transform.localScale.x;
        shurikenRoot.localPosition = new Vector3(f * 0.28f, 0.14f, -0.05f);
        shurikenRoot.localScale    = new Vector3(f, 1f, 1f);
    }

    protected override void DoAttack() => StartCoroutine(ThrowBurst());

    IEnumerator ThrowBurst()
    {
        float facing = owner.transform.localScale.x;

        for (int i = 0; i < 3; i++)
        {
            // Slight vertical spread per star
            SpawnStar(facing, i * 0.12f);
            yield return new WaitForSeconds(0.13f);
        }

        // After 3 stars thrown → fireball mode
        StartCoroutine(FireballMode());
    }

    void SpawnStar(float facing, float yOffset)
    {
        Vector2 spawnPos = (Vector2)owner.transform.position
            + new Vector2(facing * 0.5f, 0.18f + yOffset);

        var go = new GameObject("ThrowingStar");
        go.transform.position   = spawnPos;
        go.transform.localScale = Vector3.one * 0.22f;

        // 3-spoke star shape
        MakePart("A", go.transform, Vector3.zero, new Vector3(1f, 0.22f, 1f),   0f, StarColor, 4);
        MakePart("B", go.transform, Vector3.zero, new Vector3(1f, 0.22f, 1f),  60f, StarColor, 4);
        MakePart("C", go.transform, Vector3.zero, new Vector3(1f, 0.22f, 1f), -60f, StarColor, 4);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.5f;

        var dp = go.AddComponent<DamageProjectile>();
        dp.damage    = starDamage;
        dp.spinSpeed = 720f;
        dp.sticky    = true;        // stick into enemy
        dp.ownerRoot = owner.transform;

        rb.linearVelocity = new Vector2(facing * starSpeed, 0f);
        Destroy(go, 3.5f);
    }

    IEnumerator FireballMode()
    {
        var health = owner.GetComponent<FighterHealth>();
        var rb     = owner.GetComponent<Rigidbody2D>();
        if (health == null) yield break;

        // Go invincible + reduce gravity (floaty fireball feel)
        health.isInvincible     = true;
        float origGravity       = rb != null ? rb.gravityScale : 3f;
        if (rb) rb.gravityScale = 0.5f;

        // Spawn fire aura
        var fire = SpawnFireball();

        float timer = 0f;
        while (timer < fireballTime)
        {
            timer += Time.deltaTime;

            // Fire aura pulses
            if (fire != null)
            {
                float s = 0.8f + Mathf.Sin(timer * 12f) * 0.15f;
                fire.transform.localScale = Vector3.one * s;
                var sr = fire.GetComponent<SpriteRenderer>();
                if (sr) sr.color = new Color(FireColor.r, FireColor.g, FireColor.b,
                    0.55f + Mathf.Sin(timer * 10f) * 0.25f);
            }

            // Damage enemy on contact during fireball
            DamageOnTouch(6f);

            yield return null;
        }

        if (fire != null) Destroy(fire);
        health.isInvincible     = false;
        if (rb) rb.gravityScale = origGravity;
    }

    GameObject SpawnFireball()
    {
        var go = new GameObject("FireAura");
        go.transform.SetParent(owner.transform);
        go.transform.localPosition = new Vector3(0f, 0.1f, -0.1f);
        go.transform.localScale    = Vector3.one * 0.8f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = RectSprite();
        sr.color        = new Color(FireColor.r, FireColor.g, FireColor.b, 0.65f);
        sr.sortingOrder = 6;
        return go;
    }

    void DamageOnTouch(float dmg)
    {
        foreach (var c in Physics2D.OverlapCircleAll(owner.transform.position, 0.6f))
        {
            var fc = c.GetComponentInParent<FighterController>();
            if (fc == null || fc == owner) continue;
            fc.GetComponent<FighterHealth>()?.TakeDamage(dmg, owner.transform.position);
        }
    }
}
