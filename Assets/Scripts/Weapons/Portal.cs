using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    public Portal linked;
    public Color  portalColor;

    readonly HashSet<FighterController> onCooldown = new HashSet<FighterController>();

    public void BuildVisual()
    {
        // Outer glow
        var outer = new GameObject("Glow");
        outer.transform.SetParent(transform);
        outer.transform.localPosition = Vector3.zero;
        outer.transform.localScale    = new Vector3(0.50f, 1.30f, 1f);
        var osr = outer.AddComponent<SpriteRenderer>();
        osr.sprite       = MakeRect();
        osr.color        = new Color(portalColor.r, portalColor.g, portalColor.b, 0.35f);
        osr.sortingOrder = 5;

        // Inner bright ring
        var inner = new GameObject("Ring");
        inner.transform.SetParent(transform);
        inner.transform.localPosition = Vector3.zero;
        inner.transform.localScale    = new Vector3(0.30f, 1.10f, 1f);
        var isr = inner.AddComponent<SpriteRenderer>();
        isr.sprite       = MakeRect();
        isr.color        = new Color(portalColor.r, portalColor.g, portalColor.b, 0.85f);
        isr.sortingOrder = 6;

        // Dark void center
        var void_ = new GameObject("Void");
        void_.transform.SetParent(transform);
        void_.transform.localPosition = Vector3.zero;
        void_.transform.localScale    = new Vector3(0.14f, 0.95f, 1f);
        var vsr = void_.AddComponent<SpriteRenderer>();
        vsr.sprite       = MakeRect();
        vsr.color        = new Color(0.02f, 0.02f, 0.06f, 0.95f);
        vsr.sortingOrder = 7;

        // Trigger collider
        var col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = new Vector2(0.55f, 1.30f);

        // Pulse animation
        StartCoroutine(Pulse());
    }

    IEnumerator Pulse()
    {
        var glow = transform.Find("Glow")?.GetComponent<SpriteRenderer>();
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * 4f;
            if (glow) glow.color = new Color(portalColor.r, portalColor.g, portalColor.b,
                                             0.25f + Mathf.Sin(t) * 0.15f);
            yield return null;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (linked == null) return;
        var fc = other.GetComponentInParent<FighterController>();
        if (fc == null) return;
        if (onCooldown.Contains(fc) || linked.onCooldown.Contains(fc)) return;

        // Teleport to linked portal
        var rb = fc.GetComponent<Rigidbody2D>();
        fc.transform.position = linked.transform.position + Vector3.up * 0.1f;

        // Preserve speed, flip direction so they exit forward
        if (rb != null)
        {
            float spd = Mathf.Abs(rb.linearVelocity.x);
            rb.linearVelocity = new Vector2(fc.transform.localScale.x * Mathf.Max(spd, 3f), rb.linearVelocity.y);
        }

        // Both portals get cooldown for this fighter
        onCooldown.Add(fc);
        linked.onCooldown.Add(fc);
        StartCoroutine(ClearCooldown(fc));
        linked.StartCoroutine(linked.ClearCooldown(fc));

        SpawnTeleportEffect(linked.transform.position);
    }

    IEnumerator ClearCooldown(FighterController fc)
    {
        yield return new WaitForSeconds(0.9f);
        onCooldown.Remove(fc);
    }

    void SpawnTeleportEffect(Vector2 pos)
    {
        for (int i = 0; i < 5; i++)
        {
            var p = new GameObject("PortalParticle");
            p.transform.position   = pos;
            p.transform.localScale = Vector3.one * 0.14f;
            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = MakeRect();
            sr.color  = new Color(portalColor.r, portalColor.g, portalColor.b, 0.9f);
            sr.sortingOrder = 8;
            var prb = p.AddComponent<Rigidbody2D>();
            prb.gravityScale = 0.2f;
            float angle = i * 72f * Mathf.Deg2Rad;
            prb.linearVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 3f;
            Destroy(p, 0.4f);
        }
    }

    static Sprite MakeRect()
    {
        var t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var px = new Color32[16];
        for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
        t.SetPixels32(px); t.Apply();
        return Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f), 4f);
    }
}
