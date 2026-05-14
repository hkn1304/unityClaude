using System.Collections;
using UnityEngine;

// Hammer spins 360° around the user with a large hitbox — matches original game mechanic.
// The spinning head also destroys incoming projectiles (DamageProjectile / Arrow / MagicOrb).
public class HammerWeapon : Weapon
{
    public float damage       = 35f;
    public float spinDuration = 0.80f;

    public override float Cooldown    => 1.5f;
    public override float CombatRange => 1.9f;

    static readonly Color HeadColor   = new Color(0.55f, 0.60f, 0.65f);
    static readonly Color HandleColor = new Color(0.35f, 0.22f, 0.10f);
    static readonly Color BandColor   = new Color(0.45f, 0.28f, 0.15f);

    Transform pivot;
    bool      isSpinning;
    bool      hitThisSpin;

    protected override void BuildVisuals()
    {
        pivot = new GameObject("HammerPivot").transform;
        pivot.SetParent(transform);

        MakePart("Head",   pivot, new Vector3(0f,  0.78f, 0f), new Vector3(0.42f, 0.30f, 1f), 0f, HeadColor);
        MakePart("Handle", pivot, new Vector3(0f,  0.12f, 0f), new Vector3(0.07f, 0.88f, 1f), 0f, HandleColor);
        MakePart("Band",   pivot, new Vector3(0f,  0.56f, 0f), new Vector3(0.09f, 0.07f, 1f), 0f, BandColor);

        UpdatePivot();
    }

    void Update() { if (owner != null) UpdatePivot(); }

    void UpdatePivot()
    {
        float f = owner.transform.localScale.x;
        if (!isSpinning)
        {
            pivot.localPosition = new Vector3(f * 0.32f, 0.20f, -0.05f);
            pivot.localScale    = new Vector3(f, 1f, 1f);
        }
    }

    protected override void DoAttack() => StartCoroutine(Spin());

    IEnumerator Spin()
    {
        isSpinning = true;
        hitThisSpin = false;

        // Reset to world-space pivot at owner centre
        pivot.SetParent(owner.transform.parent ?? owner.transform);
        pivot.position = owner.transform.position;
        pivot.localScale = Vector3.one;

        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;
            float angle = (elapsed / spinDuration) * -360f;
            pivot.position = owner.transform.position;
            pivot.rotation = Quaternion.Euler(0f, 0f, angle);

            HitScan();
            DestroyNearbyProjectiles();

            yield return null;
        }

        // Return pivot to weapon
        pivot.SetParent(transform);
        pivot.localPosition    = Vector3.zero;
        pivot.localEulerAngles = Vector3.zero;
        isSpinning = false;
    }

    void HitScan()
    {
        Vector2 headPos = pivot.Find("Head")?.position ?? (Vector2)owner.transform.position;
        foreach (var c in Physics2D.OverlapCircleAll(headPos, 0.5f))
        {
            var fc = c.GetComponentInParent<FighterController>();
            if (fc == null || fc == owner) continue;
            if (!hitThisSpin)
            {
                hitThisSpin = true;
                // Can hit multiple times during full spin
                StartCoroutine(ResetHit());
            }
            fc.GetComponent<FighterHealth>()?.TakeDamage(damage, headPos);
            break;
        }
    }

    IEnumerator ResetHit()
    {
        yield return new WaitForSeconds(0.25f);
        hitThisSpin = false;
    }

    // Deflect incoming projectiles — hammer head destroys ranged attacks while spinning
    void DestroyNearbyProjectiles()
    {
        Transform headT = pivot.Find("Head");
        if (headT == null) return;

        foreach (var c in Physics2D.OverlapCircleAll(headT.position, 0.6f))
        {
            if (c.transform.IsChildOf(owner.transform)) continue;
            bool isProjectile = c.GetComponent<DamageProjectile>() != null
                             || c.GetComponent<Arrow>()             != null
                             || c.GetComponent<MagicOrb>()          != null;
            if (isProjectile) Destroy(c.gameObject);
        }
    }
}
