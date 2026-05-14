using UnityEngine;

public class BoomerangWeapon : Weapon
{
    public float damage = 16f;
    public float speed  = 11f;

    public override float Cooldown    => 2.0f;
    public override float CombatRange => 7f;

    static readonly Color WoodColor = new Color(0.65f, 0.40f, 0.15f);
    static readonly Color BandColor = new Color(0.92f, 0.72f, 0.26f);

    Transform bRoot;

    protected override void BuildVisuals()
    {
        bRoot = new GameObject("BoomerangRoot").transform;
        bRoot.SetParent(transform);

        // V-shape: two angled arms
        MakePart("Arm1", bRoot, new Vector3( 0.13f,  0.10f, 0f), new Vector3(0.10f, 0.50f, 1f),  26f, WoodColor);
        MakePart("Arm2", bRoot, new Vector3(-0.13f,  0.10f, 0f), new Vector3(0.10f, 0.50f, 1f), -26f, WoodColor);
        MakePart("Band", bRoot, new Vector3( 0.00f, -0.10f, 0f), new Vector3(0.16f, 0.07f, 1f),   0f, BandColor);

        UpdateRoot();
    }

    void Update() { if (owner != null) UpdateRoot(); }

    void UpdateRoot()
    {
        float f = owner.transform.localScale.x;
        bRoot.localPosition = new Vector3(f * 0.30f, 0.14f, -0.05f);
        bRoot.localScale    = new Vector3(f, 1f, 1f);
    }

    protected override void DoAttack() => Throw();

    void Throw()
    {
        float   facing   = owner.transform.localScale.x;
        Vector2 spawnPos = (Vector2)owner.transform.position + new Vector2(facing * 0.5f, 0.3f);

        var go = new GameObject("Boomerang");
        go.transform.position   = spawnPos;
        go.transform.localScale = Vector3.one * 0.30f;

        MakePart("Arm1", go.transform, new Vector3( 0.13f,  0.10f, 0f), new Vector3(0.10f, 0.50f, 1f),  26f, WoodColor, 4);
        MakePart("Arm2", go.transform, new Vector3(-0.13f,  0.10f, 0f), new Vector3(0.10f, 0.50f, 1f), -26f, WoodColor, 4);
        MakePart("Band", go.transform, new Vector3( 0.00f, -0.10f, 0f), new Vector3(0.16f, 0.07f, 1f),   0f, BandColor, 4);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.5f;

        var bp = go.AddComponent<BoomerangProjectile>();
        bp.damage    = damage;
        bp.speed     = speed;
        bp.ownerRoot = owner.transform;

        rb.linearVelocity = new Vector2(facing * speed, 2.5f);
        Destroy(go, 5f);
    }
}
