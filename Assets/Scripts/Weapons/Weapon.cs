using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    protected FighterController owner;
    float nextAttack;
    bool  visualsBuilt;

    public abstract float Cooldown           { get; }
    public virtual  float SecondaryCooldown { get; } = 1.0f;
    public virtual  float CombatRange       { get; } = 2f;

    float nextAttackSecondary;

    // Called from Editor setup OR WeaponFactory at runtime
    public void Equip(FighterController fighter)
    {
        owner = fighter;
        fighter.equippedWeapon = this;
        transform.SetParent(fighter.transform);
        transform.localPosition = Vector3.zero;
        // When created at runtime (WeaponFactory), Awake already ran without a parent,
        // so BuildVisuals was skipped — do it now.
        if (Application.isPlaying && !visualsBuilt)
        {
            visualsBuilt = true;
            BuildVisuals();
        }
    }

    // Visuals built at runtime so Texture2D sprites aren't lost on scene save
    protected virtual void Awake()
    {
        if (owner == null)
            owner = GetComponentInParent<FighterController>();
        if (owner != null && !visualsBuilt)
        {
            owner.equippedWeapon = this;
            visualsBuilt = true;
            BuildVisuals();
        }
    }

    protected abstract void BuildVisuals();

    public virtual bool TryAttack()
    {
        if (owner != null && owner.inputFrozen) return false;
        if (Time.time < nextAttack) return false;
        nextAttack = Time.time + Cooldown;
        DoAttack();
        return true;
    }

    protected abstract void DoAttack();

    public bool TrySecondaryAttack()
    {
        if (owner != null && owner.inputFrozen) return false;
        if (Time.time < nextAttackSecondary) return false;
        nextAttackSecondary = Time.time + SecondaryCooldown;
        DoSecondaryAttack();
        return true;
    }

    protected virtual void DoSecondaryAttack() { }

    // ── Shared sprite factories ───────────────────────────────────────────────

    protected static Sprite RectSprite()
    {
        var t  = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var px = new Color32[16];
        for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
        t.SetPixels32(px); t.Apply();
        return Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f), 4f);
    }

    protected static GameObject MakePart(string name, Transform parent,
        Vector3 localPos, Vector3 localScale, float zRot, Color color, int order = 2)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition    = localPos;
        go.transform.localScale       = localScale;
        go.transform.localEulerAngles = new Vector3(0, 0, zRot);
        var sr    = go.AddComponent<SpriteRenderer>();
        sr.sprite = RectSprite();
        sr.color  = color;
        sr.sortingOrder = order;
        return go;
    }
}
