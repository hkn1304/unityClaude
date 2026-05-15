using System.Collections;
using UnityEngine;

public class SwordWeapon : Weapon
{
    public float damage = 28f;

    public override float Cooldown     => 0.45f;
    public override float CombatRange  => 2.0f;

    Transform pivot;
    bool      swinging;
    bool      hitThisSwing;

    static readonly Color BladeColor  = new Color(0.80f, 0.92f, 1.00f);
    static readonly Color GuardColor  = new Color(0.90f, 0.75f, 0.10f);
    static readonly Color HandleColor = new Color(0.55f, 0.35f, 0.15f);

    protected override void BuildVisuals()
    {
        var pivotGO = new GameObject("SwordPivot");
        pivot = pivotGO.transform;
        pivot.SetParent(transform);

        // Blade (points upward in local space)
        MakePart("Blade",  pivot, new Vector3(0, 0.65f, 0), new Vector3(0.11f, 1.3f,  1), 0, BladeColor);
        MakePart("Guard",  pivot, new Vector3(0, 0.00f, 0), new Vector3(0.45f, 0.09f, 1), 0, GuardColor);
        MakePart("Handle", pivot, new Vector3(0,-0.30f, 0), new Vector3(0.10f, 0.45f, 1), 0, HandleColor);

        UpdatePivot();
    }

    void Update() { if (!swinging) UpdatePivot(); }

    void UpdatePivot()
    {
        if (owner == null) return;
        float f = owner.transform.localScale.x;
        pivot.localPosition    = new Vector3(f * 0.38f, 0.12f, -0.05f);
        pivot.localScale       = new Vector3(f, 1, 1);
        pivot.localEulerAngles = Vector3.zero;
    }

    protected override void DoAttack() => StartCoroutine(Swing());

    IEnumerator Swing()
    {
        swinging     = true;
        hitThisSwing = false;
        float facing = owner.transform.localScale.x;

        // Angles stay entirely on the forward side: upper-forward → lower-forward
        float from    = facing > 0 ? -25f :  25f;
        float to      = facing > 0 ? -105f : 105f;
        float duration = 0.18f;

        Vector3 restPos  = new Vector3(facing * 0.38f, 0.12f, -0.05f);
        Vector3 lungePos = new Vector3(facing * 0.60f, 0.12f, -0.05f);

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float prog = t / duration;
            // Lunge peaks at 40% through the swing, then retracts
            float lp = prog < 0.4f ? prog / 0.4f : 1f - (prog - 0.4f) / 0.6f;
            pivot.localPosition    = Vector3.Lerp(restPos, lungePos, lp);
            pivot.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(from, to, prog));
            HitScan(facing);
            yield return null;
        }

        pivot.localPosition    = restPos;
        pivot.localEulerAngles = Vector3.zero;
        swinging = false;
    }

    void HitScan(float facing)
    {
        if (hitThisSwing) return;
        Vector2 origin = (Vector2)owner.transform.position + new Vector2(facing * 0.5f, 0.2f);
        var hits = Physics2D.OverlapCircleAll(origin, 1.4f);
        foreach (var c in hits)
        {
            var fc = c.GetComponentInParent<FighterController>();
            if (fc == null || fc == owner) continue;
            hitThisSwing = true;
            fc.GetComponent<FighterHealth>()?.TakeDamage(damage, pivot.position);
            break;
        }
    }
}
