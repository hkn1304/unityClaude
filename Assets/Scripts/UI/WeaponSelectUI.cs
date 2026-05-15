using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class WeaponSelectUI : MonoBehaviour
{
    public Action<WeaponType, WeaponType> OnSelectionComplete;

    const int VISIBLE = 4;   // cards shown per panel at once

    static readonly WeaponType[] AllWeapons = {
        WeaponType.Sword,   WeaponType.Bow,       WeaponType.Dagger,    WeaponType.Staff,
        WeaponType.Katana,  WeaponType.Hammer,    WeaponType.Shuriken,  WeaponType.Boomerang,
        WeaponType.Gun,     WeaponType.Sniper,    WeaponType.PortalGun, WeaponType.FightGlove,
    };

    static readonly string[] WepNames = {
        "SWORD",    "BOW",     "DAGGER",      "STAFF",
        "KATANA",   "HAMMER",  "SHURIKEN",    "BOOMERANG",
        "GUN",      "SNIPER",  "PORTAL GUN",  "FIGHT GLOVE",
    };

    static readonly string[] WepStats = {
        "Balanced melee  |  22 dmg  |  0.45s",
        "Hold J to draw  |  22–36 dmg  |  13–23 speed",
        "Thrown + returns  |  18 dmg out+back  |  1.0s",
        "Hold J to charge  |  20–40 dmg  |  1.0s",
        "Thrown glowing  |  28 dmg out+back  |  1.2s",
        "Hold J to wind up  |  35–65 dmg  |  0.8–1.7s spin",
        "Stars stick in enemy  |  12×3 dmg + fireball",
        "Hold J to charge  |  16–32 dmg  |  2.0s",
        "Rapid burst x4  |  9×4 dmg  |  0.55s",
        "Laser sight + 1 shot  |  48 dmg  |  2.2s",
        "J: gun 20dmg 0.25s  |  K: portal shot 1.0s",
        "J: jab 14dmg 0.3s  |  K: hadouken 30dmg 1.0s",
    };

    static readonly Color[] WepColors = {
        new Color(1.0f, 0.85f, 0.2f),    // Sword      – gold
        new Color(0.3f, 0.9f, 0.3f),     // Bow        – green
        new Color(0.9f, 0.3f, 0.3f),     // Dagger     – red
        new Color(0.75f, 0.35f, 1.0f),   // Staff      – purple
        new Color(0.2f, 0.85f, 1.0f),    // Katana     – ice blue
        new Color(0.95f, 0.55f, 0.15f),  // Hammer     – orange
        new Color(0.95f, 0.95f, 0.3f),   // Shuriken   – yellow
        new Color(0.35f, 0.95f, 0.65f),  // Boomerang  – teal
        new Color(0.70f, 0.70f, 0.75f),  // Gun        – steel gray
        new Color(0.55f, 0.90f, 0.40f),  // Sniper     – military green
        new Color(0.20f, 0.55f, 1.00f),  // PortalGun  – portal blue
        new Color(0.90f, 0.12f, 0.12f),  // FightGlove – fighting red
    };

    // Per-panel state
    class Panel
    {
        public int    cursor, scroll;
        public bool   locked;
        public Image[]           cardBg   = new Image[VISIBLE];
        public Image[]           colorBar = new Image[VISIBLE];
        public TextMeshProUGUI[] nameLbl  = new TextMeshProUGUI[VISIBLE];
        public TextMeshProUGUI[] statsLbl = new TextMeshProUGUI[VISIBLE];
        public TextMeshProUGUI   hint, arrowUp, arrowDown;
    }

    readonly Panel p1 = new Panel();
    readonly Panel p2 = new Panel();

    bool  p2AI, selecting;

    GameObject      overlay;
    TextMeshProUGUI countdownTxt;

    // ── Public API ───────────────────────────────────────────────────────────

    public void Show(bool ai2, Action<WeaponType, WeaponType> callback)
    {
        p2AI = ai2;
        p1.cursor = p1.scroll = 0;  p1.locked = false;
        p2.cursor = p2.scroll = 0;  p2.locked = false;
        selecting = true;
        OnSelectionComplete = callback;

        if (overlay == null) BuildOverlay();
        overlay.SetActive(true);
        if (countdownTxt) countdownTxt.text = "";

        if (p1.hint) p1.hint.text = "W/S: navigate   J: confirm";
        if (p2.hint) p2.hint.text = p2AI ? "" : "↑/↓: navigate   ,: confirm";

        Refresh(p1);
        Refresh(p2);

        if (p2AI) StartCoroutine(AIRoutine());
    }

    public void Hide() { if (overlay) overlay.SetActive(false); }

    // ── Input ────────────────────────────────────────────────────────────────

    void Update()
    {
        if (!selecting || overlay == null || !overlay.activeSelf) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        if (!p1.locked)
        {
            if (kb[Key.W].wasPressedThisFrame) { p1.cursor = Mathf.Max(0, p1.cursor - 1); Refresh(p1); }
            if (kb[Key.S].wasPressedThisFrame) { p1.cursor = Mathf.Min(AllWeapons.Length - 1, p1.cursor + 1); Refresh(p1); }
            if (kb[Key.J].wasPressedThisFrame) { p1.locked = true; Refresh(p1); TryBegin(); }
        }
        else if (kb[Key.J].wasPressedThisFrame && selecting)
        {
            // J again before countdown starts = unlock and re-select
            p1.locked = false;
            if (p1.hint) p1.hint.text = "W/S: navigate   J: confirm";
            Refresh(p1);
        }

        if (!p2AI && !p2.locked)
        {
            if (kb[Key.UpArrow].wasPressedThisFrame)   { p2.cursor = Mathf.Max(0, p2.cursor - 1); Refresh(p2); }
            if (kb[Key.DownArrow].wasPressedThisFrame) { p2.cursor = Mathf.Min(AllWeapons.Length - 1, p2.cursor + 1); Refresh(p2); }
            if (kb[Key.Comma].wasPressedThisFrame)     { p2.locked = true; Refresh(p2); TryBegin(); }
        }
        else if (!p2AI && kb[Key.Comma].wasPressedThisFrame && selecting)
        {
            // comma again before countdown starts = unlock and re-select
            p2.locked = false;
            if (p2.hint) p2.hint.text = "↑/↓: navigate   ,: confirm";
            Refresh(p2);
        }
    }

    void TryBegin()
    {
        if (p1.locked && p2.locked && selecting)
        {
            selecting = false;
            StartCoroutine(Countdown());
        }
    }

    IEnumerator AIRoutine()
    {
        if (p2.hint) p2.hint.text = "AI is choosing...";
        int total = AllWeapons.Length;
        for (int t = 0; t < total * 3; t++)
        {
            p2.cursor = t % total;
            Refresh(p2);
            yield return new WaitForSeconds(0.07f);
        }
        p2.cursor = UnityEngine.Random.Range(0, total);
        p2.locked = true;
        Refresh(p2);
        TryBegin();
    }

    IEnumerator Countdown()
    {
        for (int n = 3; n >= 1; n--)
        {
            if (countdownTxt) countdownTxt.text = n.ToString();
            yield return new WaitForSeconds(1f);
        }
        if (countdownTxt) countdownTxt.text = "FIGHT!";
        yield return new WaitForSeconds(0.5f);
        Hide();
        OnSelectionComplete?.Invoke(AllWeapons[p1.cursor], AllWeapons[p2.cursor]);
    }

    // ── Refresh card contents ────────────────────────────────────────────────

    void Refresh(Panel ps)
    {
        // Keep cursor visible in the 4-card viewport
        if (ps.cursor < ps.scroll) ps.scroll = ps.cursor;
        if (ps.cursor >= ps.scroll + VISIBLE) ps.scroll = ps.cursor - VISIBLE + 1;

        int n = AllWeapons.Length;
        for (int slot = 0; slot < VISIBLE; slot++)
        {
            int  wi  = ps.scroll + slot;
            bool ok  = wi < n;
            bool sel = ok && (wi == ps.cursor);

            if (ps.cardBg[slot])
                ps.cardBg[slot].color = !ok
                    ? new Color(0.06f, 0.06f, 0.09f, 1f)
                    : (ps.locked && sel)
                        ? new Color(WepColors[wi].r * 0.22f, WepColors[wi].g * 0.22f, WepColors[wi].b * 0.22f, 1f)
                        : sel
                            ? new Color(0.20f, 0.20f, 0.30f, 1f)
                            : new Color(0.11f, 0.11f, 0.17f, 1f);

            if (ps.colorBar[slot])
                ps.colorBar[slot].color = ok ? WepColors[wi] : Color.clear;

            if (ps.nameLbl[slot])
            {
                ps.nameLbl[slot].text  = !ok ? "" :
                    (sel ? (ps.locked ? "✓ " : "▶ ") : "   ") + WepNames[wi];
                ps.nameLbl[slot].color = ok
                    ? (sel ? WepColors[wi]
                           : new Color(WepColors[wi].r * 0.5f, WepColors[wi].g * 0.5f, WepColors[wi].b * 0.5f, 1f))
                    : Color.clear;
            }

            if (ps.statsLbl[slot])
                ps.statsLbl[slot].text = ok ? WepStats[wi] : "";
        }

        if (ps.arrowUp)   ps.arrowUp.text   = ps.scroll > 0             ? "▲" : "";
        if (ps.arrowDown) ps.arrowDown.text = ps.scroll + VISIBLE < n   ? "▼" : "";

        if (ps.hint != null && ps.locked) ps.hint.text = "READY  ✓";
    }

    // ── Build UI ─────────────────────────────────────────────────────────────

    void BuildOverlay()
    {
        overlay = new GameObject("WeaponSelectOverlay");
        overlay.transform.SetParent(transform, false);
        var img = overlay.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.88f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        MkTMP(overlay.transform, "Title",
            new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(800f, 68f),
            "CHOOSE YOUR WEAPON", 52, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);

        countdownTxt = MkTMP(overlay.transform, "Countdown",
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 120f),
            "", 100, Color.yellow, TextAlignmentOptions.Center, FontStyles.Bold);

        BuildPanel(overlay.transform, "P1", new Vector2(0.25f, 0.5f),
            "PLAYER 1", "W/S: navigate   J: confirm", p1);
        BuildPanel(overlay.transform, "P2", new Vector2(0.75f, 0.5f),
            p2AI ? "PLAYER 2  [AI]" : "PLAYER 2",
            p2AI ? "" : "↑/↓: navigate   ,: confirm", p2);
    }

    void BuildPanel(Transform parent, string id, Vector2 anchor,
                    string title, string hintText, Panel ps)
    {
        var go = new GameObject("Panel" + id);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.14f, 1f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.sizeDelta        = new Vector2(380f, 430f);
        rt.anchoredPosition = Vector2.zero;

        MkTMP(go.transform, "PanelTitle",
            new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(360f, 44f),
            title, 26, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);

        ps.arrowUp = MkTMP(go.transform, "ArrowUp",
            new Vector2(0.5f, 1f), new Vector2(0f, -74f), new Vector2(360f, 18f),
            "", 16, new Color(0.65f, 0.65f, 0.65f, 1f), TextAlignmentOptions.Center, FontStyles.Normal);

        // Card slots (fixed 4 positions, content scrolls)
        float[] yOff = { -96f, -160f, -224f, -288f };
        for (int slot = 0; slot < VISIBLE; slot++)
        {
            var card = new GameObject("Slot" + slot);
            card.transform.SetParent(go.transform, false);
            var cb = card.AddComponent<Image>();
            cb.color = new Color(0.11f, 0.11f, 0.17f, 1f);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 1f);
            crt.sizeDelta        = new Vector2(352f, 54f);
            crt.anchoredPosition = new Vector2(0f, yOff[slot]);
            ps.cardBg[slot] = cb;

            // Colour bar on left
            var bar = new GameObject("Bar");
            bar.transform.SetParent(card.transform, false);
            var bi = bar.AddComponent<Image>();
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(0f, 1f);
            brt.pivot      = new Vector2(0f, 0.5f);
            brt.anchoredPosition = new Vector2(3f, 0f);
            brt.sizeDelta        = new Vector2(7f, -6f);
            ps.colorBar[slot] = bi;

            // Weapon name
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(card.transform, false);
            var nt = nameGO.AddComponent<TextMeshProUGUI>();
            nt.fontSize  = 20;
            nt.fontStyle = FontStyles.Bold;
            nt.alignment = TextAlignmentOptions.Left;
            var nrt = nameGO.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0f, 0.45f);
            nrt.anchorMax = new Vector2(1f, 1f);
            nrt.offsetMin = new Vector2(14f, 2f);
            nrt.offsetMax = new Vector2(-6f, -2f);
            ps.nameLbl[slot] = nt;

            // Stats line
            var stGO = new GameObject("Stats");
            stGO.transform.SetParent(card.transform, false);
            var st = stGO.AddComponent<TextMeshProUGUI>();
            st.fontSize  = 12;
            st.color     = new Color(0.50f, 0.50f, 0.50f, 1f);
            st.alignment = TextAlignmentOptions.Left;
            var srt = stGO.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 0.45f);
            srt.offsetMin = new Vector2(14f, 3f);
            srt.offsetMax = new Vector2(-6f, -2f);
            ps.statsLbl[slot] = st;
        }

        ps.arrowDown = MkTMP(go.transform, "ArrowDown",
            new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(360f, 18f),
            "", 16, new Color(0.65f, 0.65f, 0.65f, 1f), TextAlignmentOptions.Center, FontStyles.Normal);

        ps.hint = MkTMP(go.transform, "Hint",
            new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(360f, 34f),
            hintText, 15, new Color(0.55f, 0.55f, 0.55f, 1f), TextAlignmentOptions.Center, FontStyles.Normal);
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
