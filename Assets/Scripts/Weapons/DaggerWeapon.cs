using System.Collections;
using UnityEngine;

public class DaggerWeapon : Weapon
{
    public float damage = 13f;

    public override float Cooldown    => 0.28f;
    public override float CombatRange => 1.4f;

    static readonly Color BladeColor  = new Color(1.00f, 0.95f, 0.60f);
    static readonly Color HandleColor = new Color(0.40f, 0.25f, 0.10f);
    static readonly Color WrapColor   = new Color(0.70f, 0.15f, 0.15f);

    Transform daggerRoot;
    int stabCount;

    protected override void BuildVisuals()
    {
        daggerRoot = new GameObject("DaggerRoot").transform;
        daggerRoot.SetParent(transform);

        // Short blade
        MakePart("Blade",    daggerRoot, new Vector3(0,  0.28f, 0), new Vector3(0.10f, 0.55f, 1), 0, BladeColor);
        MakePart("Guard",    daggerRoot, new Vector3(0,  0.00f, 0), new Vector3(0.30f, 0.07f, 1), 0, BladeColor);
        MakePart("Handle",   daggerRoot, new Vector3(0, -0.18f, 0), new Vector3(0.09f, 0.28f, 1), 0, HandleColor);
        MakePart("Wrap",     daggerRoot, new Vector3(0, -0.12f, 0), new Vector3(0.11f, 0.10f, 1), 0, WrapColor);

        UpdateRoot();
    }

    void Update() { if (owner != null) UpdateRoot(); }

    void UpdateRoot()
    {
        float f = owner.transform.localScale.x;
        daggerRoot.localPosition    = new Vector3(f * 0.30f, 0.10f, -0.05f);
        daggerRoot.localScale       = new Vector3(f, 1f, 1f);
    }

    protected override void DoAttack() => StartCoroutine(DoubleStab());

    IEnumerator DoubleStab()
    {
        yield return Stab( 0.28f,  0.05f);
        yield return new WaitForSeconds(0.08f);
        yield return Stab(-0.18f, -0.05f);
    }

    IEnumerator Stab(float fwdOffset, float yOffset)
    {
        float facing = owner.transform.localScale.x;
        Vector3 orig = daggerRoot.localPosition;

        daggerRoot.localPosition = orig + new Vector3(facing * fwdOffset, yOffset, 0);
        HitScan(facing);
        yield return new WaitForSeconds(0.07f);
        daggerRoot.localPosition = orig;
    }

    void HitScan(float facing)
    {
        Vector2 origin = (Vector2)owner.transform.position + new Vector2(facing * 0.5f, 0.1f);
        foreach (var c in Physics2D.OverlapCircleAll(origin, CombatRange))
        {
            var fc = c.GetComponentInParent<FighterController>();
            if (fc == null || fc == owner) continue;
            fc.GetComponent<FighterHealth>()?.TakeDamage(damage, transform.position);
            break;
        }
    }
}
