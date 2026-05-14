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
        swinging      = true;
        hitThisSwing  = false;
        float facing  = owner.transform.localScale.x;
        float from    = facing > 0 ? 55f : -55f;
        float to      = facing > 0 ? -65f : 65f;
        float duration = 0.18f;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            pivot.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(from, to, t / duration));
            HitScan(facing);
            yield return null;
        }

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
