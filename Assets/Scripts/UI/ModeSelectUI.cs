using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class ModeSelectUI : MonoBehaviour
{
    public Action<bool> OnModeChosen;   // true = AI, false = 2P

    GameObject      overlay;
    Image[]         optionBg   = new Image[2];
    TextMeshProUGUI titleTxt;
    int             cursor;

    static readonly string[] Labels = { "1 PLAYER   vs   AI", "2 PLAYERS   (same keyboard)" };
    static readonly string[] Descs  = {
        "You vs computer opponent",
        "P1: A/D/W/J     P2: ←/→/↑/,",
    };
    static readonly Color SelColor  = new Color(0.20f, 0.20f, 0.32f, 1f);
    static readonly Color IdleColor = new Color(0.10f, 0.10f, 0.16f, 1f);
    static readonly Color AccentSel = new Color(0.35f, 0.70f, 1.00f);
    static readonly Color AccentIdle= new Color(0.22f, 0.38f, 0.55f);

    // ── Public ───────────────────────────────────────────────────────────────

    public void Show(Action<bool> callback)
    {
        OnModeChosen = callback;
        cursor = 0;
        if (overlay == null) BuildOverlay();
        overlay.SetActive(true);
        Refresh();
    }

    public void Hide() { if (overlay) overlay.SetActive(false); }

    // ── Input ────────────────────────────────────────────────────────────────

    void Update()
    {
        if (overlay == null || !overlay.activeSelf) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb[Key.W].wasPressedThisFrame || kb[Key.UpArrow].wasPressedThisFrame)
        { cursor = (cursor - 1 + 2) % 2; Refresh(); }

        if (kb[Key.S].wasPressedThisFrame || kb[Key.DownArrow].wasPressedThisFrame)
        { cursor = (cursor + 1) % 2; Refresh(); }

        if (kb[Key.J].wasPressedThisFrame || kb[Key.Enter].wasPressedThisFrame)
        {
            Hide();
            OnModeChosen?.Invoke(cursor == 0);   // 0 = AI, 1 = 2P
        }
    }

    // ── Build UI ─────────────────────────────────────────────────────────────

    void BuildOverlay()
    {
        overlay = new GameObject("ModeSelectOverlay");
        overlay.transform.SetParent(transform, false);
        var img = overlay.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.92f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        // Game title
        MkTMP(overlay.transform, "GameTitle",
            new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(900f, 80f),
            "SUPREME STICKMAN DUELIST", 58, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);

        MkTMP(overlay.transform, "Sub",
            new Vector2(0.5f, 1f), new Vector2(0f, -148f), new Vector2(700f, 40f),
            "SELECT GAME MODE", 28, new Color(0.55f, 0.75f, 1f, 1f), TextAlignmentOptions.Center, FontStyles.Normal);

        // Two option cards
        float[] yPos = { 80f, -20f };
        for (int i = 0; i < 2; i++)
        {
            var card = new GameObject("Option" + i);
            card.transform.SetParent(overlay.transform, false);
            var cb = card.AddComponent<Image>();
            cb.color = IdleColor;
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta        = new Vector2(580f, 82f);
            crt.anchoredPosition = new Vector2(0f, yPos[i]);
            optionBg[i] = cb;

            // Accent bar on left
            var bar = new GameObject("Bar");
            bar.transform.SetParent(card.transform, false);
            var bi = bar.AddComponent<Image>();
            bi.color = AccentIdle;
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(0f, 1f);
            brt.pivot      = new Vector2(0f, 0.5f);
            brt.anchoredPosition = new Vector2(4f, 0f);
            brt.sizeDelta        = new Vector2(8f, -8f);

            // Main label
            MkTMP(card.transform, "Label",
                new Vector2(0.5f, 1f), new Vector2(8f, -10f), new Vector2(540f, 36f),
                Labels[i], 24, AccentIdle, TextAlignmentOptions.Left, FontStyles.Bold);

            // Description
            MkTMP(card.transform, "Desc",
                new Vector2(0.5f, 0f), new Vector2(8f, 12f), new Vector2(540f, 26f),
                Descs[i], 16, new Color(0.55f, 0.55f, 0.55f, 1f), TextAlignmentOptions.Left, FontStyles.Normal);
        }

        MkTMP(overlay.transform, "Hint",
            new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(600f, 30f),
            "W/S: select     J / Enter: confirm", 17,
            new Color(0.45f, 0.45f, 0.45f, 1f), TextAlignmentOptions.Center, FontStyles.Normal);
    }

    void Refresh()
    {
        for (int i = 0; i < 2; i++)
        {
            bool sel = (i == cursor);
            if (optionBg[i]) optionBg[i].color = sel ? SelColor : IdleColor;

            // Update accent bar colour + label colour
            var bar   = optionBg[i]?.transform.Find("Bar")?.GetComponent<Image>();
            var label = optionBg[i]?.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (bar)   bar.color   = sel ? AccentSel  : AccentIdle;
            if (label) label.color = sel ? AccentSel  : AccentIdle;
        }
    }

    static TextMeshProUGUI MkTMP(Transform parent, string name,
        Vector2 anchor, Vector2 pos, Vector2 size,
        string text, float fs, Color col, TextAlignmentOptions align, FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = fs;
        t.color     = col;
        t.alignment = align;
        t.fontStyle = style;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        return t;
    }
}
