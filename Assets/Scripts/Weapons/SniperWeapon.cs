using System.Collections;
using UnityEngine;

// Sniper: shows a red laser sight for 0.4s, then fires one devastating fast bullet.
public class SniperWeapon : Weapon
{
    public float damage      = 48f;
    public float bulletSpeed = 36f;
    public float aimTime     = 0.40f;

    public override float Cooldown    => 2.2f;
    public override float CombatRange => 12f;

    static readonly Color StockColor  = new Color(0.35f, 0.22f, 0.10f);
    static readonly Color BarrelColor = new Color(0.28f, 0.28f, 0.32f);
    static readonly Color ScopeColor  = new Color(0.15f, 0.55f, 0.25f);
    static readonly Color LaserColor  = new Color(1.00f, 0.05f, 0.05f);
    static readonly Color BulletColor = new Color(0.92f, 0.96f, 1.00f);

    Transform rifleRoot;
    Transform barrelTip;
    GameObject laserSight;

    protected override void BuildVisuals()
    {
        rifleRoot = new GameObject("RifleRoot").transform;
        rifleRoot.SetParent(transform);

        // Long barrel
        MakePart("Barrel",  rifleRoot, new Vector3( 0.42f,  0.04f, 0f), new Vector3(0.88f, 0.08f, 1f),  0f, BarrelColor);
        // Stock / body
        MakePart("Stock",   rifleRoot, new Vector3(-0.12f,  0.00f, 0f), new Vector3(0.44f, 0.16f, 1f),  0f, StockColor);
        // Cheek piece / butt
        MakePart("Butt",    rifleRoot, new Vector3(-0.38f, -0.08f, 0f), new Vector3(0.14f, 0.24f, 1f),  0f, StockColor);
        // Scope on top
        MakePart("Scope",   rifleRoot, new Vector3( 0.10f,  0.10f, 0f), new Vector3(0.22f, 0.09f, 1f),  0f, ScopeColor);
        MakePart("ScopeLens",rifleRoot,new Vector3( 0.22f,  0.10f, 0f), new Vector3(0.06f, 0.10f, 1f),  0f, new Color(0.4f, 0.8f, 1f));
        // Grip
        MakePart("Grip",    rifleRoot, new Vector3( 0.04f, -0.14f, 0f), new Vector3(0.09f, 0.22f, 1f), 12f, StockColor);

        var tip = new GameObject("BarrelTip");
        tip.transform.SetParent(rifleRoot);
        tip.transform.localPosition = new Vector3(0.88f, 0.04f, 0f);
        barrelTip = tip.transform;

        UpdateRoot();
    }

    void Update() { if (owner != null) UpdateRoot(); }

    void UpdateRoot()
    {
        float f = owner.transform.localScale.x;
        rifleRoot.localPosition = new Vector3(f * 0.22f, 0.10f, -0.05f);
        rifleRoot.localScale    = new Vector3(f, 1f, 1f);
    }

    protected override void DoAttack() => StartCoroutine(AimAndFire());

    IEnumerator AimAndFire()
    {
        float facing = owner.transform.localScale.x;

        // Show laser sight
        ShowLaser(facing);

        // Pulsing aim time
        float t = 0f;
        while (t < aimTime)
        {
            t += Time.deltaTime;
            if (laserSight != null)
            {
                float alpha = 0.5f + Mathf.Sin(t * 30f) * 0.4f;
                var sr = laserSight.GetComponent<SpriteRenderer>();
                if (sr) sr.color = new Color(LaserColor.r, LaserColor.g, LaserColor.b, alpha);
            }
            yield return null;
        }

        HideLaser();
        FireShot(facing);
    }

    void ShowLaser(float facing)
    {
        HideLaser();

        Vector2 origin = barrelTip != null
            ? (Vector2)barrelTip.position
            : (Vector2)owner.transform.position + new Vector2(facing * 0.9f, 0.1f);

        const float beamLength = 14f;
        laserSight = new GameObject("LaserSight");
        laserSight.transform.position   = origin + new Vector2(facing * beamLength * 0.5f, 0f);
        laserSight.transform.localScale = new Vector3(beamLength, 0.04f, 1f);

        var sr = laserSight.AddComponent<SpriteRenderer>();
        sr.sprite       = RectSprite();
        sr.color        = new Color(LaserColor.r, LaserColor.g, LaserColor.b, 0.7f);
        sr.sortingOrder = 6;
    }

    void HideLaser()
    {
        if (laserSight != null) { Destroy(laserSight); laserSight = null; }
    }

    void FireShot(float facing)
    {
        Vector2 spawnPos = barrelTip != null
            ? (Vector2)barrelTip.position
            : (Vector2)owner.transform.position + new Vector2(facing * 0.9f, 0.1f);

        // Muzzle flash
        var flash = new GameObject("SniperFlash");
        flash.transform.position   = spawnPos;
        flash.transform.localScale = new Vector3(0.40f, 0.18f, 1f);
        var fsr = flash.AddComponent<SpriteRenderer>();
        fsr.sprite       = RectSprite();
        fsr.color        = new Color(1f, 0.9f, 0.6f, 0.9f);
        fsr.sortingOrder = 6;
        Destroy(flash, 0.06f);

        // Bullet — long thin streak
        var go = new GameObject("SniperBullet");
        go.transform.position   = spawnPos;
        go.transform.localScale = new Vector3(0.55f, 0.05f, 1f);
        go.transform.rotation   = Quaternion.Euler(0f, 0f, facing > 0 ? 0f : 180f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = RectSprite();
        sr.color        = BulletColor;
        sr.sortingOrder = 5;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = Vector2.one;

        var dp = go.AddComponent<DamageProjectile>();
        dp.damage    = damage;
        dp.ownerRoot = owner.transform;

        rb.linearVelocity = new Vector2(facing * bulletSpeed, 0f);
        Destroy(go, 0.8f);
    }

    void OnDestroy() => HideLaser();
}
