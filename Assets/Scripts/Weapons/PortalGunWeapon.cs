using UnityEngine;

// First shot = blue portal, second = orange portal.
// When both exist, any fighter touching one exits the other.
public class PortalGunWeapon : Weapon
{
    public override float Cooldown    => 1.0f;
    public override float CombatRange => 10f;

    static readonly Color Blue       = new Color(0.20f, 0.55f, 1.00f);
    static readonly Color Orange     = new Color(1.00f, 0.50f, 0.08f);
    static readonly Color BodyColor  = new Color(0.18f, 0.18f, 0.22f);

    Transform gunRoot;
    Transform barrelTip;
    SpriteRenderer emitterSR;

    Portal portalA, portalB;
    bool   nextIsBlue = true;

    protected override void BuildVisuals()
    {
        gunRoot = new GameObject("PortalGunRoot").transform;
        gunRoot.SetParent(transform);

        MakePart("Body",   gunRoot, new Vector3( 0.00f,  0.00f, 0f), new Vector3(0.36f, 0.20f, 1f),  0f, BodyColor);
        MakePart("Barrel", gunRoot, new Vector3( 0.28f,  0.04f, 0f), new Vector3(0.38f, 0.10f, 1f),  0f, BodyColor);
        MakePart("Grip",   gunRoot, new Vector3(-0.02f, -0.16f, 0f), new Vector3(0.09f, 0.26f, 1f), 10f, BodyColor);

        var emitGO = MakePart("Emitter", gunRoot,
            new Vector3(0.50f, 0.04f, 0f), new Vector3(0.10f, 0.14f, 1f), 0f, Blue, 3);
        emitterSR = emitGO.GetComponent<SpriteRenderer>();

        var tip = new GameObject("BarrelTip");
        tip.transform.SetParent(gunRoot);
        tip.transform.localPosition = new Vector3(0.57f, 0.04f, 0f);
        barrelTip = tip.transform;

        UpdateRoot();
    }

    void Update() { if (owner != null) UpdateRoot(); }

    void UpdateRoot()
    {
        float f = owner.transform.localScale.x;
        gunRoot.localPosition = new Vector3(f * 0.28f, 0.10f, -0.05f);
        gunRoot.localScale    = new Vector3(f, 1f, 1f);
        if (emitterSR) emitterSR.color = nextIsBlue ? Blue : Orange;
    }

    protected override void DoAttack()
    {
        bool isBlue = nextIsBlue;
        nextIsBlue  = !nextIsBlue;

        Color col    = isBlue ? Blue : Orange;
        float facing = owner.transform.localScale.x;

        Vector2 spawnPos = barrelTip != null
            ? (Vector2)barrelTip.position
            : (Vector2)owner.transform.position + new Vector2(facing * 0.6f, 0.1f);

        // Muzzle flash
        SpawnFlash(spawnPos, col);

        // Projectile
        var go = new GameObject("PortalShot");
        go.transform.position   = spawnPos;
        go.transform.localScale = Vector3.one * 0.20f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = RectSprite(); sr.color = col; sr.sortingOrder = 5;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        var col2d = go.AddComponent<CircleCollider2D>();
        col2d.isTrigger = true;
        col2d.radius    = 0.45f;

        var shot = go.AddComponent<PortalShot>();
        shot.ownerRoot = owner.transform;
        shot.OnLand    = pos => PlacePortal(pos, col, isBlue);

        rb.linearVelocity = new Vector2(facing * 15f, 0f);
        Destroy(go, 2f);
    }

    void PlacePortal(Vector2 pos, Color col, bool isBlue)
    {
        if (isBlue  && portalA != null) Destroy(portalA.gameObject);
        if (!isBlue && portalB != null) Destroy(portalB.gameObject);

        var go = new GameObject(isBlue ? "PortalBlue" : "PortalOrange");
        go.transform.position = pos;

        var portal = go.AddComponent<Portal>();
        portal.portalColor = col;
        portal.BuildVisual();
        Destroy(go, 10f);

        if (isBlue)
        {
            portalA = portal;
            if (portalB != null && portalB != null)
            { portalA.linked = portalB; portalB.linked = portalA; }
        }
        else
        {
            portalB = portal;
            if (portalA != null)
            { portalA.linked = portalB; portalB.linked = portalA; }
        }
    }

    void SpawnFlash(Vector2 pos, Color col)
    {
        var f = new GameObject("PFlash");
        f.transform.position   = pos;
        f.transform.localScale = Vector3.one * 0.22f;
        var sr = f.AddComponent<SpriteRenderer>();
        sr.sprite = RectSprite(); sr.color = col; sr.sortingOrder = 6;
        Destroy(f, 0.06f);
    }

    void OnDestroy()
    {
        if (portalA != null) Destroy(portalA.gameObject);
        if (portalB != null) Destroy(portalB.gameObject);
    }
}
