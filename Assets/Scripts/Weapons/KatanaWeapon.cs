using System.Collections;
using UnityEngine;

public class KatanaWeapon : Weapon
{
    public float damage    = 25f;
    public float lungeForce = 7f;

    public override float Cooldown    => 0.42f;
    public override float CombatRange => 2.3f;

    static readonly Color BladeColor  = new Color(0.88f, 0.96f, 1.00f);
    static readonly Color GuardColor  = new Color(0.20f, 0.20f, 0.25f);
    static readonly Color HandleColor = new Color(0.10f, 0.48f, 0.18f);

    Transform pivot;
    bool      hitThisSwing;

    protected override void BuildVisuals()
    {
        pivot = new GameObject("KatanaPivot").transform;
        pivot.SetParent(transform);

        MakePart("Blade",  pivot, new Vector3(0f,  0.58f, 0f), new Vector3(0.055f, 1.16f, 1f),  0f, BladeColor);
        MakePart("Guard",  pivot, new Vector3(0f,  0.02f, 0f), new Vector3(0.18f,  0.05f, 1f),  0f, GuardColor);
        MakePart("Handle", pivot, new Vector3(0f, -0.24f, 0f), new Vector3(0.052f, 0.38f, 1f),  0f, HandleColor);

        UpdatePivot();
    }

    void Update() { if (owner != null) UpdatePivot(); }

    void UpdatePivot()
    {
        float f = owner.transform.localScale.x;
        pivot.localPosition = new Vector3(f * 0.36f, 0.14f, -0.05f);
        pivot.localScale    = new Vector3(f, 1f, 1f);
    }

    protected override void DoAttack() => StartCoroutine(LungeSlash());

    IEnumerator LungeSlash()
    {
        hitThisSwing = false;
        float facing = owner.transform.localScale.x;

        // Blade flash (charge colour)
        SetBladeColor(Color.cyan);

        // Wind-up: pull blade back
        pivot.localEulerAngles = new Vector3(0f, 0f, facing * -75f);
        yield return new WaitForSeconds(0.07f);

        // Lunge impulse
        var rb = owner.GetComponent<Rigidbody2D>();
        if (rb != null) rb.AddForce(new Vector2(facing * lungeForce, 0f), ForceMode2D.Impulse);

        // Fast slash forward
        float slashTime = 0.10f;
        for (float t = 0f; t < 1f; t += Time.deltaTime / slashTime)
        {
            float angle = Mathf.Lerp(-75f, 80f, t) * facing;
            pivot.localEulerAngles = new Vector3(0f, 0f, angle);
            if (!hitThisSwing) HitScan(facing);
            yield return null;
        }

        pivot.localEulerAngles = Vector3.zero;
        SetBladeColor(BladeColor);
    }

    void HitScan(float facing)
    {
        Vector2 origin = (Vector2)owner.transform.position + new Vector2(facing * 0.5f, 0.2f);
        foreach (var c in Physics2D.OverlapCircleAll(origin, CombatRange))
        {
            var fc = c.GetComponentInParent<FighterController>();
            if (fc == null || fc == owner) continue;
            hitThisSwing = true;
            fc.GetComponent<FighterHealth>()?.TakeDamage(damage, pivot.position);
            break;
        }
    }

    void SetBladeColor(Color col)
    {
        var blade = pivot.Find("Blade");
        if (blade) blade.GetComponent<SpriteRenderer>().color = col;
    }
}
