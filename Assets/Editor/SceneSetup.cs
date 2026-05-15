using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class SceneSetup
{
    [MenuItem("Supreme Duelist/Setup Scene %#s")]
    public static void SetupScene()
    {
        EnsureTag("Ground");
        CleanScene();
        SetupCamera();
        CreateArena();

        var p1 = CreateFighter("Player1", new Vector3(-3f, -1.1f, 0f), new Color(0.2f, 0.8f, 1f),
            KeyCode.A, KeyCode.D, KeyCode.W, KeyCode.J, KeyCode.K, aiControlled: false);

        var p2 = CreateFighter("Player2", new Vector3(3f, -1.1f, 0f), new Color(1f, 0.3f, 0.3f),
            KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.UpArrow,
            KeyCode.Comma, KeyCode.Period, aiControlled: true);

        SetupCameraController(p1.transform, p2.transform);

        var gm = CreateGameManager(p1, p2);
        CreateUI(gm);

        CreateMusicManager();

        EditorUtility.SetDirty(p1.gameObject);
        EditorUtility.SetDirty(p2.gameObject);
        EditorUtility.SetDirty(gm.gameObject);
        Debug.Log("[Supreme Duelist] Scene setup complete! Press Play to start.");
    }

    // ─── Music ────────────────────────────────────────────────────────────────

    static void CreateMusicManager()
    {
        var go = new GameObject("MusicManager");
        go.AddComponent<AudioSource>();
        go.AddComponent<MusicManager>();
        EditorUtility.SetDirty(go);
    }

    // ─── Tag registration ─────────────────────────────────────────────────────

    static void EnsureTag(string tagName)
    {
        var asset = AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset");
        var so    = new SerializedObject(asset);
        var tags  = so.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tagName) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
        so.ApplyModifiedProperties();
    }

    // ─── Cleanup ──────────────────────────────────────────────────────────────

    static readonly string[] ManagedObjects =
        { "Ground", "WallLeft", "WallRight", "Player1", "Player2", "GameManager", "Canvas", "MusicManager" };

    static void CleanScene()
    {
        foreach (var name in ManagedObjects)
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    // ─── Camera ───────────────────────────────────────────────────────────────

    static void SetupCamera()
    {
        var cam = Camera.main;
        cam.orthographic     = true;
        cam.orthographicSize = 5f;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.08f, 0.08f, 0.15f);
        cam.transform.position = new Vector3(0f, -0.5f, -10f);
        EditorUtility.SetDirty(cam);
    }

    static void SetupCameraController(Transform p1, Transform p2)
    {
        var cam = Camera.main;
        var cc  = cam.gameObject.GetComponent<CameraController>();
        if (cc == null) cc = cam.gameObject.AddComponent<CameraController>();
        cc.p1 = p1;
        cc.p2 = p2;
        EditorUtility.SetDirty(cam.gameObject);
    }

    // ─── Arena ────────────────────────────────────────────────────────────────

    static void CreateArena()
    {
        // Ground platform
        var ground = new GameObject("Ground");
        ground.tag = "Ground";
        ground.transform.position   = new Vector3(0f, -2.5f, 0f);
        ground.transform.localScale = new Vector3(16f, 1f, 1f);

        var sr = ground.AddComponent<SpriteRenderer>();
        sr.sprite = SquareSprite();
        sr.color  = new Color(0.25f, 0.25f, 0.35f);
        sr.sortingOrder = -1;
        ground.AddComponent<BoxCollider2D>();

        // Invisible boundary walls
        CreateWall("WallLeft",  new Vector3(-8.5f, 0f, 0f));
        CreateWall("WallRight", new Vector3( 8.5f, 0f, 0f));
    }

    static void CreateWall(string name, Vector3 pos)
    {
        var go = new GameObject(name);
        // No "Ground" tag — walls must not affect grounded detection
        go.transform.position   = pos;
        go.transform.localScale = new Vector3(1f, 12f, 1f);
        go.AddComponent<BoxCollider2D>();
    }

    // ─── Fighters ─────────────────────────────────────────────────────────────

    static FighterController CreateFighter(string objName, Vector3 pos, Color color,
        KeyCode left, KeyCode right, KeyCode jump, KeyCode punch, KeyCode kick,
        bool aiControlled)
    {
        var root = new GameObject(objName);
        root.transform.position = pos;

        // 2D physics
        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var cap = root.AddComponent<CapsuleCollider2D>();
        cap.size      = new Vector2(0.75f, 1.8f);
        cap.direction = CapsuleDirection2D.Vertical;

        // Stickman visuals
        AddPart(root.transform, "Body", new Vector3(0f, -0.1f, 0f), new Vector3(0.28f, 0.75f, 1f), 0f, color, SquareSprite());
        AddPart(root.transform, "Head", new Vector3(0f,  0.78f, 0f), new Vector3(0.52f, 0.52f, 1f), 0f, color, CircleSprite());
        AddPart(root.transform, "ArmL", new Vector3(-0.36f, 0.18f, 0f), new Vector3(0.13f, 0.48f, 1f), -25f, color, SquareSprite());
        AddPart(root.transform, "ArmR", new Vector3( 0.36f, 0.18f, 0f), new Vector3(0.13f, 0.48f, 1f),  25f, color, SquareSprite());
        AddPart(root.transform, "LegL", new Vector3(-0.19f, -0.68f, 0f), new Vector3(0.13f, 0.52f, 1f), -12f, color, SquareSprite());
        AddPart(root.transform, "LegR", new Vector3( 0.19f, -0.68f, 0f), new Vector3(0.13f, 0.52f, 1f),  12f, color, SquareSprite());

        // FighterHealth before FighterController so Awake() can find it via GetComponent.
        // FloatingHealthBar subscribes to FighterHealth events in its own Awake().
        root.AddComponent<FighterHealth>();
        root.AddComponent<FloatingHealthBar>();

        var fc = root.AddComponent<FighterController>();
        fc.leftKey  = left;
        fc.rightKey = right;
        fc.jumpKey  = jump;
        fc.punchKey = punch;
        fc.kickKey  = kick;
        fc.isPlayerControlled = !aiControlled;

        if (aiControlled) root.AddComponent<AIBrain>();

        return fc;
    }

    static void AddPart(Transform parent, string partName, Vector3 localPos, Vector3 localScale,
                        float zRot, Color color, Sprite sprite)
    {
        var go = new GameObject(partName);
        go.transform.SetParent(parent);
        go.transform.localPosition    = localPos;
        go.transform.localScale       = localScale;
        go.transform.localEulerAngles = new Vector3(0f, 0f, zRot);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color  = color;
    }

    // ─── Weapons ──────────────────────────────────────────────────────────────

    static void EquipWeapon<T>(FighterController fighter) where T : Weapon
    {
        var go = new GameObject(typeof(T).Name);
        var w  = go.AddComponent<T>();
        w.Equip(fighter);
    }

    // ─── GameManager ──────────────────────────────────────────────────────────

    static GameManager CreateGameManager(FighterController p1, FighterController p2)
    {
        var go = new GameObject("GameManager");
        var gm = go.AddComponent<GameManager>();
        gm.player1 = p1;
        gm.player2 = p2;
        gm.p2IsAI  = p2.GetComponent<AIBrain>() != null;
        return gm;
    }

    // ─── UI ───────────────────────────────────────────────────────────────────

    static void CreateUI(GameManager gm)
    {
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Health bars are floating world-space bars on each fighter (FloatingHealthBar component).

        // Round score
        gm.roundInfoText = BuildTMP(canvasGO.transform, "RoundInfoText",
            new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(240f, 46f),
            "P1: 0   P2: 0", 28, Color.white, TextAlignmentOptions.Center);

        // Winner banner
        gm.winnerText = BuildTMP(canvasGO.transform, "WinnerText",
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 110f),
            "", 64, Color.yellow, TextAlignmentOptions.Center);
        gm.winnerText.fontStyle = FontStyles.Bold;
        gm.winnerText.gameObject.SetActive(false);

        // Controls hint
        BuildTMP(canvasGO.transform, "ControlsHint",
            new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(780f, 36f),
            "P1: A/D  W jump  J attack  K alt     P2: ←/→  ↑ jump  , attack  . alt",
            16, new Color(1f, 1f, 1f, 0.5f), TextAlignmentOptions.Center);

        gm.weaponSelectUI = canvasGO.AddComponent<WeaponSelectUI>();
        gm.modeSelectUI   = canvasGO.AddComponent<ModeSelectUI>();
    }

    static HealthBar BuildHealthBar(Transform parent, string name,
        Vector2 anchor, Vector2 anchoredPos, Vector2 sizeDelta, string label, bool invert)
    {
        // Background — fully opaque dark panel
        var bg = new GameObject(name);
        bg.transform.SetParent(parent, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = bgRT.anchorMax = bgRT.pivot = anchor;
        bgRT.anchoredPosition = anchoredPos;
        bgRT.sizeDelta         = sizeDelta;

        // Fill — Simple image scaled via anchors (no transparency tricks)
        var fill = new GameObject("Fill");
        fill.transform.SetParent(bg.transform, false);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = Color.green;
        fillImg.type  = Image.Type.Simple;
        fillImg.preserveAspect = false;
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(3f, 3f);
        fillRT.offsetMax = new Vector2(-3f, -3f);

        // Label
        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(bg.transform, false);
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text      = label;
        lbl.fontSize  = 20;
        lbl.fontStyle = FontStyles.Bold;
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.color     = Color.white;
        var lrt = lblGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.sizeDelta = Vector2.zero;

        var hb = bg.AddComponent<HealthBar>();
        hb.fillImage  = fillImg;
        hb.invertFill = invert;
        return hb;
    }

    static TextMeshProUGUI BuildTMP(Transform parent, string name,
        Vector2 anchor, Vector2 anchoredPos, Vector2 sizeDelta,
        string text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = alignment;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta         = sizeDelta;
        return tmp;
    }

    // ─── Sprite helpers ───────────────────────────────────────────────────────

    static Sprite SquareSprite()
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var px  = new Color32[16];
        for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    static Sprite CircleSprite()
    {
        const int S = 64;
        var tex  = new Texture2D(S, S, TextureFormat.RGBA32, false);
        float r  = S * 0.5f;
        var  px  = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = x - r + 0.5f, dy = y - r + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                byte  a    = dist <= r ? (byte)255 : (byte)0;
                px[y * S + x] = new Color32(255, 255, 255, a);
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), (float)S);
    }
}
