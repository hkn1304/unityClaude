using UnityEngine;

// World-space health bar that floats above its fighter.
// Attach to a fighter GameObject alongside FighterHealth.
public class FloatingHealthBar : MonoBehaviour
{
    const float BarW    = 1.2f;
    const float BarH    = 0.14f;
    const float YOffset = 1.55f;

    GameObject     barRoot;
    Transform      fill;
    SpriteRenderer fillSR;

    void Awake()
    {
        BuildBar();
        var fh = GetComponent<FighterHealth>();
        if (fh != null) fh.onHealthChanged.AddListener(SetHealth);
    }

    void LateUpdate()
    {
        if (barRoot != null)
            barRoot.transform.position = transform.position + new Vector3(0f, YOffset, -0.1f);
    }

    void OnDestroy()
    {
        if (barRoot != null) Destroy(barRoot);
    }

    public void SetHealth(float percent)
    {
        if (fill == null) return;
        percent = Mathf.Clamp01(percent);
        float newW = BarW * percent;
        fill.localScale    = new Vector3(newW, BarH * 0.7f, 1f);
        fill.localPosition = new Vector3((newW - BarW) * 0.5f, 0f, -0.01f);

        fillSR.color = percent > 0.5f
            ? Color.Lerp(Color.yellow, Color.green,  (percent - 0.5f) * 2f)
            : Color.Lerp(Color.red,    Color.yellow,  percent * 2f);
    }

    void BuildBar()
    {
        barRoot = new GameObject("FloatingHPBar_" + gameObject.name);

        var bg = new GameObject("Bg");
        bg.transform.SetParent(barRoot.transform);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale    = new Vector3(BarW, BarH, 1f);
        var bgSR = bg.AddComponent<SpriteRenderer>();
        bgSR.sprite       = WhiteSprite();
        bgSR.color        = new Color(0.12f, 0.12f, 0.16f, 0.92f);
        bgSR.sortingOrder = 10;

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(barRoot.transform);
        fillGO.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        fillGO.transform.localScale    = new Vector3(BarW, BarH * 0.7f, 1f);
        fillSR = fillGO.AddComponent<SpriteRenderer>();
        fillSR.sprite       = WhiteSprite();
        fillSR.color        = Color.green;
        fillSR.sortingOrder = 11;
        fill = fillGO.transform;
    }

    static Sprite WhiteSprite()
    {
        var t  = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var px = new Color32[16];
        for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
        t.SetPixels32(px); t.Apply();
        return Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f), 4f);
    }
}
