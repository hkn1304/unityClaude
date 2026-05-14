using System.Collections;
using UnityEngine;

// Rapid-fire pistol: burst of 4 bullets with muzzle flash.
public class GunWeapon : Weapon
{
    public float bulletDamage = 9f;
    public float bulletSpeed  = 22f;
    public int   burstCount   = 4;

    public override float Cooldown    => 0.55f;
    public override float CombatRange => 8f;

    static readonly Color BodyColor    = new Color(0.22f, 0.22f, 0.26f);
    static readonly Color BarrelColor  = new Color(0.32f, 0.32f, 0.36f);
    static readonly Color GripColor    = new Color(0.18f, 0.12f, 0.10f);
    static readonly Color BulletColor  = new Color(1.00f, 0.88f, 0.35f);
    static readonly Color MuzzleColor  = new Color(1.00f, 0.75f, 0.20f);

    Transform gunRoot;
    Transform barrelTip;

    protected override void BuildVisuals()
    {
        gunRoot = new GameObject("GunRoot").transform;
        gunRoot.SetParent(transform);

        // Body
        MakePart("Body",    gunRoot, new Vector3(0.04f,  0.02f, 0f), new Vector3(0.38f, 0.22f, 1f),  0f, BodyColor);
        // Barrel (extends forward)
        MakePart("Barrel",  gunRoot, new Vector3(0.32f,  0.06f, 0f), new Vector3(0.52f, 0.09f, 1f),  0f, BarrelColor);
        // Grip (angled downward)
        MakePart("Grip",    gunRoot, new Vector3(0.00f, -0.16f, 0f), new Vector3(0.10f, 0.26f, 1f), 10f, GripColor);
        // Trigger guard hint
        MakePart("Trigger", gunRoot, new Vector3(0.06f, -0.06f, 0f), new Vector3(0.06f, 0.10f, 1f),  0f, BarrelColor);

        // Invisible marker at barrel tip for spawn point
        var tip = new GameObject("BarrelTip");
        tip.transform.SetParent(gunRoot);
        tip.transform.localPosition = new Vector3(0.60f, 0.06f, 0f);
        barrelTip = tip.transform;

        UpdateRoot();
    }

    void Update() { if (owner != null) UpdateRoot(); }

    void UpdateRoot()
    {
        float f = owner.transform.localScale.x;
        gunRoot.localPosition = new Vector3(f * 0.30f, 0.08f, -0.05f);
        gunRoot.localScale    = new Vector3(f, 1f, 1f);
    }

    protected override void DoAttack() => StartCoroutine(Burst());

    IEnumerator Burst()
    {
        float facing = owner.transform.localScale.x;
        for (int i = 0; i < burstCount; i++)
        {
            FireBullet(facing);
            MuzzleFlash();
            yield return new WaitForSeconds(0.07f);
        }
    }

    void FireBullet(float facing)
    {
        Vector2 spawnPos = barrelTip != null
            ? (Vector2)barrelTip.position
            : (Vector2)owner.transform.position + new Vector2(facing * 0.7f, 0.08f);

        var go = new GameObject("Bullet");
        go.transform.position   = spawnPos;
        go.transform.localScale = new Vector3(0.28f, 0.06f, 1f);
        // Rotate so the long axis faces the travel direction
        go.transform.rotation = Quaternion.Euler(0f, 0f, facing > 0 ? 0f : 180f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = RectSprite();
        sr.color        = BulletColor;
        sr.sortingOrder = 4;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = Vector2.one;

        var dp = go.AddComponent<DamageProjectile>();
        dp.damage    = bulletDamage;
        dp.ownerRoot = owner.transform;

        rb.linearVelocity = new Vector2(facing * bulletSpeed, 0f);
        Destroy(go, 1.2f);
    }

    void MuzzleFlash()
    {
        Vector2 pos = barrelTip != null
            ? (Vector2)barrelTip.position
            : (Vector2)owner.transform.position + new Vector2(owner.transform.localScale.x * 0.8f, 0.08f);

        var go = new GameObject("MuzzleFlash");
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * 0.22f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = RectSprite();
        sr.color        = MuzzleColor;
        sr.sortingOrder = 5;
        Destroy(go, 0.05f);
    }
}
