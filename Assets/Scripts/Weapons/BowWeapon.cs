using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Player: hold J to draw the bow, release to fire. Longer draw = faster, harder arrow.
// AI: press J for a standard auto-drawn shot.
public class BowWeapon : Weapon
{
    public float arrowSpeed  = 13f;
    public float arrowDamage = 22f;

    public override float Cooldown    => 1.3f;
    public override float CombatRange => 7f;

    const float MaxCharge    = 1.2f;
    const float MaxSpeedAdd  = 10f;
    const float MaxDamageAdd = 14f;

    static readonly Color WoodColor   = new Color(0.60f, 0.38f, 0.15f);
    static readonly Color StringColor = new Color(0.92f, 0.90f, 0.80f);
    static readonly Color ArrowColor  = new Color(0.78f, 0.58f, 0.20f);
    static readonly Color TipColor    = new Color(0.55f, 0.55f, 0.65f);

    Transform bowRoot;
    Transform nockedArrow;
    Transform bowstring;
    Vector3   stringBase;
    bool      drawing;

    bool  charging;
    float chargeStart;
    float lastFireTime = -99f;

    protected override void BuildVisuals()
    {
        bowRoot = new GameObject("BowRoot").transform;
        bowRoot.SetParent(transform);

        MakePart("LimbTop", bowRoot, new Vector3(-0.12f,  0.30f, 0), new Vector3(0.10f, 0.58f, 1), -12f, WoodColor);
        MakePart("LimbBot", bowRoot, new Vector3(-0.12f, -0.30f, 0), new Vector3(0.10f, 0.58f, 1),  12f, WoodColor);
        MakePart("LimbMid", bowRoot, new Vector3(-0.06f,  0.00f, 0), new Vector3(0.10f, 0.25f, 1),   0f, WoodColor);

        var strGO = MakePart("String", bowRoot, new Vector3(-0.22f, 0, 0), new Vector3(0.04f, 0.82f, 1), 0, StringColor);
        bowstring  = strGO.transform;
        stringBase = bowstring.localPosition;

        var nocked = MakePart("NockedArrow", bowRoot, new Vector3(0.04f, 0, -0.05f), new Vector3(0.07f, 0.80f, 1), 0, ArrowColor);
        MakePart("NockedTip", nocked.transform, new Vector3(0, 0.46f, 0), new Vector3(1.4f, 0.18f, 1), 0, TipColor);
        nockedArrow = nocked.transform;

        UpdateBowRoot();
    }

    public override bool TryAttack()
    {
        if (owner != null && owner.isPlayerControlled) return false;
        return base.TryAttack();
    }

    void Update()
    {
        if (!drawing && !charging) UpdateBowRoot();
        HandleCharge();
    }

    void UpdateBowRoot()
    {
        if (owner == null) return;
        float f = owner.transform.localScale.x;
        bowRoot.localPosition = new Vector3(-f * 0.42f, 0.15f, -0.05f);
        bowRoot.localScale    = new Vector3(f, 1f, 1f);
    }

    void HandleCharge()
    {
        if (owner == null || !owner.isPlayerControlled) return;

        if (owner.inputFrozen)
        {
            if (charging) { charging = false; ResetDraw(); }
            return;
        }

        var kb  = Keyboard.current;
        if (kb == null) return;
        var key = FighterController.Map(owner.punchKey);
        if (key == Key.None) return;

        if (kb[key].wasPressedThisFrame && !charging && Time.time >= lastFireTime + Cooldown)
        {
            charging    = true;
            chargeStart = Time.time;
            if (nockedArrow) nockedArrow.gameObject.SetActive(true);
        }

        if (!charging) return;

        float t = Mathf.Clamp01((Time.time - chargeStart) / MaxCharge);
        UpdateDrawVisual(t);

        if (kb[key].wasReleasedThisFrame)
        {
            charging      = false;
            lastFireTime  = Time.time;
            ResetDraw();
            SpawnArrow(arrowDamage + t * MaxDamageAdd, arrowSpeed + t * MaxSpeedAdd, t);
        }
    }

    void UpdateDrawVisual(float t)
    {
        float pull = t * 0.22f;
        if (nockedArrow) nockedArrow.localPosition = new Vector3(0.04f - pull, 0f, -0.05f);
        if (bowstring)   bowstring.localPosition   = new Vector3(stringBase.x - pull * 0.5f, 0f, 0f);
    }

    void ResetDraw()
    {
        if (nockedArrow) { nockedArrow.localPosition = new Vector3(0.04f, 0f, -0.05f); nockedArrow.gameObject.SetActive(false); }
        if (bowstring)   bowstring.localPosition = stringBase;
    }

    // AI uses coroutine-based draw-and-release
    protected override void DoAttack() => StartCoroutine(DrawAndRelease());

    IEnumerator DrawAndRelease()
    {
        drawing = true;
        if (nockedArrow) nockedArrow.gameObject.SetActive(true);

        float    pullTime = 0.28f;
        for (float t = 0; t < pullTime; t += Time.deltaTime)
        {
            float pull = Mathf.Sin(t / pullTime * Mathf.PI) * 0.18f;
            if (nockedArrow) nockedArrow.localPosition = new Vector3(0.04f - pull, 0f, -0.05f);
            if (bowstring)   bowstring.localPosition   = new Vector3(stringBase.x - pull * 0.5f, 0f, 0f);
            yield return null;
        }

        if (nockedArrow) nockedArrow.gameObject.SetActive(false);
        if (bowstring)   bowstring.localPosition = stringBase;
        SpawnArrow(arrowDamage, arrowSpeed, 0f);
        drawing = false;
    }

    void SpawnArrow(float dmg, float spd, float chargeT)
    {
        float   facing   = owner.transform.localScale.x;
        Vector2 spawnPos = (Vector2)owner.transform.position + new Vector2(facing * 0.5f, 0.15f);
        Vector2 launchVel = ComputeLaunchVelocity(spawnPos, facing, spd, chargeT);

        var go = new GameObject("Arrow");
        go.transform.position = spawnPos;

        MakePart("Shaft",  go.transform, Vector3.zero,             new Vector3(0.07f, 0.75f, 1), 0,   ArrowColor, 3);
        MakePart("Tip",    go.transform, new Vector3(0, 0.43f, 0), new Vector3(0.13f, 0.18f, 1), 0,   TipColor, 3);
        MakePart("FletchL",go.transform, new Vector3(-0.06f,-0.35f,0),new Vector3(0.06f,0.22f,1), 20f, new Color(0.9f,0.3f,0.3f), 3);
        MakePart("FletchR",go.transform, new Vector3( 0.06f,-0.35f,0),new Vector3(0.06f,0.22f,1),-20f, new Color(0.9f,0.3f,0.3f), 3);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale           = Mathf.Lerp(1.8f, 0.6f, chargeT);  // flatter arc at full charge
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints            = RigidbodyConstraints2D.None;

        var col       = go.AddComponent<CapsuleCollider2D>();
        col.isTrigger = true;
        col.size      = new Vector2(0.13f, 0.72f);

        var arrow       = go.AddComponent<Arrow>();
        arrow.damage    = dmg;
        arrow.ownerRoot = owner.transform;

        rb.linearVelocity     = launchVel;
        float angle           = Mathf.Atan2(launchVel.x, launchVel.y) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, -angle);

        Destroy(go, 5f);
    }

    Vector2 ComputeLaunchVelocity(Vector2 from, float facing, float spd, float chargeT)
    {
        Vector2 target = (owner.opponent != null)
            ? (Vector2)owner.opponent.position + new Vector2(0f, 0.4f)
            : from + new Vector2(facing * 8f, 0f);

        Vector2 diff     = target - from;
        float   hDist    = Mathf.Abs(diff.x);
        float   arcBoost = Mathf.Lerp(
            Mathf.Clamp(hDist * 0.28f, 0.6f, 3.2f),
            0.2f,   // near-flat at full charge
            chargeT);
        return diff.normalized * spd + Vector2.up * arcBoost;
    }
}
