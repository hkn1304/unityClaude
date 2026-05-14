using System.Collections;
using UnityEngine;

public class BowWeapon : Weapon
{
    public float arrowSpeed  = 13f;
    public float arrowDamage = 22f;

    public override float Cooldown    => 1.3f;
    public override float CombatRange => 5.5f;

    Transform bowRoot;
    Transform nockedArrow;
    Transform bowstring;
    bool      drawing;

    static readonly Color WoodColor   = new Color(0.60f, 0.38f, 0.15f);
    static readonly Color StringColor = new Color(0.92f, 0.90f, 0.80f);
    static readonly Color ArrowColor  = new Color(0.78f, 0.58f, 0.20f);
    static readonly Color TipColor    = new Color(0.55f, 0.55f, 0.65f);

    protected override void BuildVisuals()
    {
        bowRoot = new GameObject("BowRoot").transform;
        bowRoot.SetParent(transform);

        // Bow limbs (slightly angled for a curved look)
        MakePart("LimbTop", bowRoot, new Vector3(-0.12f,  0.30f, 0), new Vector3(0.10f, 0.58f, 1), -12f, WoodColor);
        MakePart("LimbBot", bowRoot, new Vector3(-0.12f, -0.30f, 0), new Vector3(0.10f, 0.58f, 1),  12f, WoodColor);
        MakePart("LimbMid", bowRoot, new Vector3(-0.06f,  0.00f, 0), new Vector3(0.10f, 0.25f, 1),   0f, WoodColor);

        // Bowstring
        var strGO = MakePart("String", bowRoot, new Vector3(-0.22f, 0, 0), new Vector3(0.04f, 0.82f, 1), 0, StringColor);
        bowstring = strGO.transform;

        // Arrow sitting on the bow (nocked, waiting to fire)
        var nocked = MakePart("NockedArrow", bowRoot, new Vector3(0.04f, 0, -0.05f), new Vector3(0.07f, 0.80f, 1), 0, ArrowColor);
        MakePart("NockedTip", nocked.transform, new Vector3(0, 0.46f, 0), new Vector3(1.4f, 0.18f, 1), 0, TipColor);
        nockedArrow = nocked.transform;

        UpdateBowRoot();
    }

    void Update() { if (!drawing) UpdateBowRoot(); }

    void UpdateBowRoot()
    {
        if (owner == null) return;
        float f = owner.transform.localScale.x;
        bowRoot.localPosition = new Vector3(-f * 0.42f, 0.15f, -0.05f);
        bowRoot.localScale    = new Vector3(f, 1f, 1f);
    }

    protected override void DoAttack() => StartCoroutine(DrawAndRelease());

    IEnumerator DrawAndRelease()
    {
        drawing = true;
        if (nockedArrow) nockedArrow.gameObject.SetActive(true);

        // Pull string back
        float pullTime = 0.28f;
        Vector3 strBase = bowstring ? bowstring.localPosition : Vector3.zero;
        for (float t = 0; t < pullTime; t += Time.deltaTime)
        {
            float pull = Mathf.Sin(t / pullTime * Mathf.PI) * 0.18f;
            if (nockedArrow) nockedArrow.localPosition = new Vector3(0.04f - pull, 0f, -0.05f);
            if (bowstring)   bowstring.localPosition   = new Vector3(strBase.x - pull * 0.5f, 0f, 0f);
            yield return null;
        }

        // Release — hide nocked arrow, spawn real arrow
        if (nockedArrow) nockedArrow.gameObject.SetActive(false);
        if (bowstring)   bowstring.localPosition = strBase;
        SpawnArrow();
        drawing = false;
    }

    void SpawnArrow()
    {
        float   facing    = owner.transform.localScale.x;
        Vector2 spawnPos  = (Vector2)owner.transform.position + new Vector2(facing * 0.5f, 0.15f);
        Vector2 launchVel = ComputeLaunchVelocity(spawnPos, facing);

        // Build arrow
        var go = new GameObject("Arrow");
        go.transform.position = spawnPos;

        // Shaft
        MakePart("Shaft", go.transform, Vector3.zero, new Vector3(0.07f, 0.75f, 1), 0, ArrowColor, 3);

        // Metal tip (top)
        MakePart("Tip", go.transform, new Vector3(0, 0.43f, 0), new Vector3(0.13f, 0.18f, 1), 0, TipColor, 3);

        // Fletching (bottom notch)
        MakePart("FletchL", go.transform, new Vector3(-0.06f, -0.35f, 0), new Vector3(0.06f, 0.22f, 1),  20f, new Color(0.9f, 0.3f, 0.3f), 3);
        MakePart("FletchR", go.transform, new Vector3( 0.06f, -0.35f, 0), new Vector3(0.06f, 0.22f, 1), -20f, new Color(0.9f, 0.3f, 0.3f), 3);

        // Physics
        var rb             = go.AddComponent<Rigidbody2D>();
        rb.gravityScale    = 1.8f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints     = RigidbodyConstraints2D.None;

        // Trigger
        var col            = go.AddComponent<CapsuleCollider2D>();
        col.isTrigger      = true;
        col.size           = new Vector2(0.13f, 0.72f);

        // Script
        var arrow          = go.AddComponent<Arrow>();
        arrow.damage       = arrowDamage;
        arrow.ownerRoot    = owner.transform;

        // Set velocity then orient
        rb.linearVelocity  = launchVel;
        float angle        = Mathf.Atan2(launchVel.x, launchVel.y) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, -angle);

        Destroy(go, 5f);
    }

    Vector2 ComputeLaunchVelocity(Vector2 from, float facing)
    {
        // Aim toward upper-body of opponent if in range, else shoot forward
        Vector2 target = (owner.opponent != null)
            ? (Vector2)owner.opponent.position + new Vector2(0f, 0.4f)
            : from + new Vector2(facing * 8f, 0f);

        Vector2 diff      = target - from;
        float   hDist     = Mathf.Abs(diff.x);

        // Upward arc proportional to horizontal distance
        float arcBoost    = Mathf.Clamp(hDist * 0.28f, 0.6f, 3.2f);
        return diff.normalized * arrowSpeed + Vector2.up * arcBoost;
    }
}
