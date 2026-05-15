using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Player: hold J to wind up (hammer raises, glows), release for powered 360° spin.
// AI: press J for a standard spin.
public class HammerWeapon : Weapon
{
    public float damage       = 35f;
    public float spinDuration = 0.80f;

    public override float Cooldown    => 1.5f;
    public override float CombatRange => 1.9f;

    const float MaxCharge       = 1.2f;
    const float MaxDamageBonus  = 30f;   // 35 → 65 at full charge
    const float MaxDurBonus     = 0.9f;  // 0.8 → 1.7s at full charge

    static readonly Color HeadColor   = new Color(0.55f, 0.60f, 0.65f);
    static readonly Color HandleColor = new Color(0.35f, 0.22f, 0.10f);
    static readonly Color BandColor   = new Color(0.45f, 0.28f, 0.15f);
    static readonly Color GlowColor   = new Color(1.00f, 0.50f, 0.05f);

    Transform      pivot;
    SpriteRenderer headSR;
    bool           isSpinning;
    bool           hitThisSpin;

    bool  charging;
    float chargeStart;
    float lastUseTime = -99f;

    protected override void BuildVisuals()
    {
        pivot = new GameObject("HammerPivot").transform;
        pivot.SetParent(transform);

        var headGO = MakePart("Head",   pivot, new Vector3(0f, 0.78f, 0f), new Vector3(0.42f, 0.30f, 1f), 0f, HeadColor);
        MakePart("Handle", pivot, new Vector3(0f, 0.12f, 0f), new Vector3(0.07f, 0.88f, 1f), 0f, HandleColor);
        MakePart("Band",   pivot, new Vector3(0f, 0.56f, 0f), new Vector3(0.09f, 0.07f, 1f), 0f, BandColor);
        headSR = headGO.GetComponent<SpriteRenderer>();

        UpdatePivot();
    }

    public override bool TryAttack()
    {
        if (owner != null && owner.isPlayerControlled) return false;
        return base.TryAttack();
    }

    void Update()
    {
        if (owner != null) UpdatePivot();
        HandleCharge();
    }

    void UpdatePivot()
    {
        float f = owner.transform.localScale.x;
        if (!isSpinning && !charging)
        {
            pivot.localPosition    = new Vector3(f * 0.32f, 0.20f, -0.05f);
            pivot.localScale       = new Vector3(f, 1f, 1f);
            pivot.localEulerAngles = Vector3.zero;
        }
    }

    void HandleCharge()
    {
        if (owner == null || !owner.isPlayerControlled || isSpinning) return;

        if (owner.inputFrozen)
        {
            if (charging) { charging = false; ResetVisual(); }
            return;
        }

        var kb  = Keyboard.current;
        if (kb == null) return;
        var key = FighterController.Map(owner.punchKey);
        if (key == Key.None) return;

        if (kb[key].wasPressedThisFrame && !charging && Time.time >= lastUseTime + Cooldown)
        {
            charging    = true;
            chargeStart = Time.time;
        }

        if (!charging) return;

        float t = Mathf.Clamp01((Time.time - chargeStart) / MaxCharge);
        UpdateChargeVisual(t);

        if (kb[key].wasReleasedThisFrame)
        {
            charging    = false;
            lastUseTime = Time.time;
            ResetVisual();
            StartCoroutine(Spin(damage + t * MaxDamageBonus, spinDuration + t * MaxDurBonus));
        }
    }

    void UpdateChargeVisual(float t)
    {
        if (headSR) headSR.color = Color.Lerp(HeadColor, GlowColor, t);
        // Raise hammer as charge increases (rotate pivot upward)
        float f = owner.transform.localScale.x;
        pivot.localPosition    = new Vector3(f * 0.32f, 0.20f, -0.05f);
        pivot.localScale       = new Vector3(f, 1f, 1f);
        pivot.localEulerAngles = new Vector3(0f, 0f, -80f * t * f);  // raise toward overhead
    }

    void ResetVisual()
    {
        if (headSR) headSR.color = HeadColor;
    }

    protected override void DoAttack() => StartCoroutine(Spin(damage, spinDuration));

    IEnumerator Spin(float dmg, float dur)
    {
        isSpinning  = true;
        hitThisSpin = false;

        pivot.SetParent(owner.transform.parent ?? owner.transform);
        pivot.position   = owner.transform.position;
        pivot.localScale = Vector3.one;

        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float angle = (elapsed / dur) * -360f;
            pivot.position = owner.transform.position;
            pivot.rotation = Quaternion.Euler(0f, 0f, angle);

            HitScan(dmg);
            DestroyNearbyProjectiles();

            yield return null;
        }

        pivot.SetParent(transform);
        pivot.localPosition    = Vector3.zero;
        pivot.localEulerAngles = Vector3.zero;
        isSpinning = false;
    }

    void HitScan(float dmg)
    {
        Vector2 headPos = pivot.Find("Head")?.position ?? (Vector2)owner.transform.position;
        foreach (var c in Physics2D.OverlapCircleAll(headPos, 0.5f))
        {
            var fc = c.GetComponentInParent<FighterController>();
            if (fc == null || fc == owner) continue;
            if (!hitThisSpin)
            {
                hitThisSpin = true;
                StartCoroutine(ResetHit());
            }
            fc.GetComponent<FighterHealth>()?.TakeDamage(dmg, headPos);
            break;
        }
    }

    IEnumerator ResetHit()
    {
        yield return new WaitForSeconds(0.25f);
        hitThisSpin = false;
    }

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
