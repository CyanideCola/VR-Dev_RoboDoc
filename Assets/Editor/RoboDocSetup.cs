using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Builds the scanner, spare part, missing-antenna socket and the store screen.
// Safe to run again: each step replaces what it made last time.
public static class RoboDocSetup
{
    const string Prefabs = "Assets/PreFabs/";
    const string Mats = "Assets/Materials/";
    const string ScenePath = "Assets/Main Game.unity";

    // Interface palette (section 11 of the design doc)
    static readonly Color Ink = Hex("0B1220"), Panel = Hex("131C2E"), Raised = Hex("1B2740"), Line = Hex("2B3A5C"),
        Text = Hex("D9EEFF"), Cyan = Hex("19D3F5"), Amber = Hex("FFC53D"), Hover = Hex("1E6F8A");

    [MenuItem("Tools/RoboDoc/Build Scanner, Parts and Store")]
    public static void RunAll()
    {
        var scanner = BuildScanner();
        var part = BuildSparePart();
        AddAntennaSocket();
        AddDatabaseEntry();
        BuildStore(scanner, part);
        AssetDatabase.SaveAssets();
        Debug.Log("RoboDocSetup: done");
    }

    [MenuItem("Tools/RoboDoc/Build HUD, Teleport Facing and Tutorial Arrow")]
    public static void RunHudTeleportTutorial()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var cam = Camera.main.transform;
        BuildStatusHud(cam);
        KeepFacingOnTeleport();
        WireTutorialArrow(cam);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("RoboDocSetup: HUD, teleport and tutorial arrow done");
    }

    [MenuItem("Tools/RoboDoc/Build Challenge and Menu")]
    public static void RunChallengeAndMenu()
    {
        var game = EditorSceneManager.OpenScene(ScenePath);
        var hud = Object.FindAnyObjectByType<StatusHud>(FindObjectsInactive.Include);
        if (hud)
        {
            var label = hud.transform.Find("TimerLabel");
            hud.timerLabel = label ? label.GetComponent<TMP_Text>() : null;
            EditorUtility.SetDirty(hud);
        }
        BuildChallengeResults(Camera.main.transform);
        EditorSceneManager.MarkSceneDirty(game);
        EditorSceneManager.SaveScene(game);

        var menu = EditorSceneManager.OpenScene(MenuScenePath);
        BuildMenu();
        EditorSceneManager.MarkSceneDirty(menu);
        EditorSceneManager.SaveScene(menu);
        AssetDatabase.SaveAssets();
        Debug.Log("RoboDocSetup: challenge and menu done");
    }

    const string MenuScenePath = "Assets/Scenes/1 Start Scene.unity";

    // ---------- challenge results ----------

    static void BuildChallengeResults(Transform cam)
    {
        var old = GameObject.Find("ChallengeResultsCanvas");
        if (old) Object.DestroyImmediate(old);
        var store = GameObject.Find("StoreCanvas");

        var go = new GameObject("ChallengeResultsCanvas", typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(460, 440);
        rt.localScale = Vector3.one * 0.001f;
        rt.position = new Vector3(0, 1.4f, 1.2f);   // PanelFollow moves it in front of the player
        // Same raycasters as the store, so the controller rays can press the buttons
        foreach (var c in store.GetComponents<Component>())
            if (c.GetType().Name.Contains("Raycaster") && !go.GetComponent(c.GetType())) go.AddComponent(c.GetType());

        var follow = go.AddComponent<PanelFollow>();
        follow.head = cam;
        follow.distance = 1.1f;
        follow.height = -0.05f;

        var title = Font("ChakraPetch-Regular SDF");
        var mono = Font("ShareTechMono-Regular SDF");
        var results = go.AddComponent<ChallengeResults>();

        var panel = Rect("Panel", go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        panel.gameObject.AddComponent<Image>().color = new Color(Ink.r, Ink.g, Ink.b, 0.95f);
        var head = Label("Title", panel, "CHALLENGE CLEAR", title, 30, Cyan, TextAlignmentOptions.Center);
        Place(head.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -50), new Vector2(-10, -10));
        var time = Label("Time", panel, "0:00.00", mono, 48, Amber, TextAlignmentOptions.Center);
        Place(time.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -110), new Vector2(-10, -54));

        // Initials: an up button, the letter and a down button per slot, then SAVE
        var entry = Rect("Entry", panel, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -116));
        var prompt = Label("Prompt", entry, "ENTER YOUR INITIALS", mono, 18, Text, TextAlignmentOptions.Center);
        Place(prompt.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -30), new Vector2(-10, -4));
        var letters = new TMP_Text[3];
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 90f;
            var col = Rect("Slot" + i, entry, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(x - 36, -230), new Vector2(x + 36, -36));
            var up = SmallBtn(col, "Up", "^", mono, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -44), Vector2.zero);
            UnityEventTools.AddIntPersistentListener(up.onClick, results.Up, i);
            letters[i] = Label("Letter", col, "A", mono, 56, Amber, TextAlignmentOptions.Center);
            Place(letters[i].rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -50), new Vector2(0, 50));
            var down = SmallBtn(col, "Down", "v", mono, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 44));
            UnityEventTools.AddIntPersistentListener(down.onClick, results.Down, i);
        }
        var save = SmallBtn(entry, "Save", "SAVE", mono, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-80, 18), new Vector2(80, 64));
        UnityEventTools.AddPersistentListener(save.onClick, results.Save);

        // After saving: rank and top times, and a way back to the menu
        var board = Label("Board", panel, "", mono, 22, Text, TextAlignmentOptions.Top);
        board.textWrappingMode = TextWrappingModes.Normal;
        board.overflowMode = TextOverflowModes.Overflow;
        board.richText = true;
        Place(board.rectTransform, Vector2.zero, Vector2.one, new Vector2(20, 84), new Vector2(-20, -120));
        var menuBtn = SmallBtn(panel, "Menu", "MAIN MENU", mono, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-100, 18), new Vector2(100, 64));
        UnityEventTools.AddPersistentListener(menuBtn.onClick, results.BackToMenu);

        results.root = panel.gameObject;
        results.timeText = time;
        results.letters = letters;
        results.entryGroup = entry.gameObject;
        results.boardText = board;
        results.menuButton = menuBtn.gameObject;
        panel.gameObject.SetActive(false);
        EditorUtility.SetDirty(results);

        var gp = Object.FindAnyObjectByType<GameProgress>(FindObjectsInactive.Include);
        gp.results = results;
        EditorUtility.SetDirty(gp);
    }

    // A button placed by anchors, in the store's button style
    static Button SmallBtn(Transform parent, string name, string text, TMP_FontAsset font, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        var tmp = Rect("tmp", parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        var b = Btn(tmp, name, text, font, Text, out var label);
        b.transform.SetParent(parent, false);
        Object.DestroyImmediate(b.GetComponent<LayoutElement>());
        Object.DestroyImmediate(tmp.gameObject);
        Place((RectTransform)b.transform, aMin, aMax, oMin, oMax);
        label.fontSize = 24;
        return b;
    }

    // ---------- main menu ----------

    // Row 1: DEMO | CHALLENGE, row 2: OPTIONS | LEADERBOARD, row 3: ABOUT | QUIT (unchanged)
    static void BuildMenu()
    {
        var menu = Object.FindAnyObjectByType<GameStartMenu>(FindObjectsInactive.Include);
        var buttons = menu.mainMenu.transform;
        foreach (var n in new[] { "Challenge", "Leaderboard" })
        {
            var oldBtn = buttons.Find(n);
            if (oldBtn) Object.DestroyImmediate(oldBtn.gameObject);
        }
        var oldPage = menu.transform.Find("Leaderboard");
        if (oldPage) { menu.returnButtons.RemoveAll(b => !b || b.transform.IsChildOf(oldPage)); Object.DestroyImmediate(oldPage.gameObject); }

        Vector2 half = new Vector2(443.5f, 215f);
        var demo = (RectTransform)menu.startButton.transform;
        demo.sizeDelta = half; demo.anchoredPosition = new Vector2(-237.2f, 289f);
        SetButtonText(demo, "DEMO");

        var challenge = (RectTransform)Object.Instantiate(demo.gameObject, buttons).transform;
        challenge.name = "Challenge";
        challenge.anchoredPosition = new Vector2(241.8f, 289f);
        SetButtonText(challenge, "CHALLENGE");

        var options = (RectTransform)menu.optionButton.transform;
        options.sizeDelta = half; options.anchoredPosition = new Vector2(-237.2f, 18f);
        SetButtonText(options, "OPTIONS");

        var lb = (RectTransform)Object.Instantiate(options.gameObject, buttons).transform;
        lb.name = "Leaderboard";
        lb.anchoredPosition = new Vector2(241.8f, 18f);
        SetButtonText(lb, "LEADERBOARD");

        // Leaderboard page: a copy of About (BACK button and text), retitled
        var page = Object.Instantiate(menu.about, menu.transform);
        page.name = "Leaderboard";
        page.SetActive(false);
        var mono = Font("ShareTechMono-Regular SDF");
        var oldText = page.GetComponentsInChildren<TMP_Text>(true).First(t => t.text.StartsWith("Credits"));
        var titleText = Label("Title", page.transform, "FASTEST CHALLENGE TIMES", mono, 80, Color.white, TextAlignmentOptions.Center);
        Place(titleText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -170), new Vector2(-40, -40));
        var list = oldText;
        list.name = "List";
        list.text = "No times yet. Be the first!";
        list.fontSize = 46;
        list.fontStyle = FontStyles.Normal;
        list.alignment = TextAlignmentOptions.Top;
        list.richText = true;
        Place(list.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-460, -190), new Vector2(460, 340));
        var view = page.AddComponent<LeaderboardView>();
        view.list = list;
        var back = page.GetComponentInChildren<Button>(true);
        menu.returnButtons.Add(back);

        SetButtonText(menu.aboutButton.transform, "ABOUT");
        SetButtonText(menu.quitButton.transform, "QUIT");
        // The BACK buttons had white text on white buttons
        foreach (var b in menu.returnButtons)
            if (b) b.GetComponentInChildren<TMP_Text>(true).color = Hex("323232");

        menu.challengeButton = challenge.GetComponent<Button>();
        menu.leaderboardButton = lb.GetComponent<Button>();
        menu.leaderboard = page;
        EditorUtility.SetDirty(menu);
    }

    static void SetButtonText(Transform button, string text)
    {
        var t = button.GetComponentInChildren<TMP_Text>(true);
        t.text = text;
        t.enableAutoSizing = true;
        t.fontSizeMin = 30;
        t.fontSizeMax = 100;
    }

    // ---------- HUD ----------

    // Energy bar and timer at the bottom right of the view, mirroring the notification feed on the left
    static void BuildStatusHud(Transform cam)
    {
        var old = GameObject.Find("StatusHudCanvas");
        if (old) Object.DestroyImmediate(old);

        var feed = GameObject.Find("NotificationCanvas");
        var go = new GameObject("StatusHudCanvas", typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(300, 84);
        rt.localScale = Vector3.one * 0.001f;
        rt.position = new Vector3(0.45f, 1.2f, 1.3f);   // PanelFollow moves it at runtime
        if (feed)
        {
            go.layer = feed.layer;
            canvas.sortingOrder = feed.GetComponent<Canvas>().sortingOrder;
            rt.localScale = feed.transform.localScale;
        }

        var follow = go.AddComponent<PanelFollow>();
        follow.head = cam;
        follow.distance = 1.3f;
        follow.height = -0.4f;
        follow.side = 0.45f;

        var mono = Font("ShareTechMono-Regular SDF");

        var bg = Rect("Background", go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bg.gameObject.AddComponent<Image>().color = new Color(Ink.r, Ink.g, Ink.b, 0.75f);

        var label = Label("EnergyLabel", go.transform, "ENERGY", mono, 16, Text, TextAlignmentOptions.Left);
        Place(label.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -30), new Vector2(120, -6));

        var barBg = Rect("EnergyBar", go.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(12, 14), new Vector2(160, 40));
        var barImg = barBg.gameObject.AddComponent<Image>();
        barImg.color = Raised;
        var fillRt = Rect("Fill", barBg, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
        var fill = fillRt.gameObject.AddComponent<Image>();
        fill.sprite = TexSprite("Assets/Textures/White.png", (x, y, n) => true);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        fill.color = Hex("3DDC84");

        var energyText = Label("EnergyText", go.transform, "100", mono, 22, Text, TextAlignmentOptions.Left);
        Place(energyText.rectTransform, Vector2.zero, Vector2.zero, new Vector2(166, 12), new Vector2(206, 42));

        var timerLabel = Label("TimerLabel", go.transform, "TIME", mono, 16, Text, TextAlignmentOptions.Right);
        Place(timerLabel.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-90, -30), new Vector2(-12, -6));
        var timer = Label("TimerText", go.transform, "4:00", mono, 28, Cyan, TextAlignmentOptions.Right);   // Chakra Petch's atlas lacks some digits
        Place(timer.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-90, 8), new Vector2(-12, 46));

        var hud = go.AddComponent<StatusHud>();
        hud.energyFill = fill;
        hud.energyText = energyText;
        hud.timerText = timer;
    }

    // ---------- teleport ----------

    // Keep the player's current facing when they land, so the reticle arrow follows the camera
    static void KeepFacingOnTeleport()
    {
        foreach (var a in Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            a.matchOrientation = UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.MatchOrientation.WorldSpaceUp;
            a.matchDirectionalInput = false;
            EditorUtility.SetDirty(a);
            PrefabUtility.RecordPrefabInstancePropertyModifications(a);
        }
    }

    // ---------- tutorial arrow ----------

    static void WireTutorialArrow(Transform cam)
    {
        var tm = Object.FindAnyObjectByType<TutorialManager>(FindObjectsInactive.Include);
        var arrow = tm.panel.GetComponent<ObjectiveArrow>();
        if (!arrow) arrow = tm.panel.AddComponent<ObjectiveArrow>();

        // Panel arrow: sits in the panel's left margin and turns toward the objective
        var panel = tm.panel.transform.Find("Panel");
        var oldArrow = panel.Find("ObjectiveArrow");
        if (oldArrow) Object.DestroyImmediate(oldArrow.gameObject);
        var arrowRt = Rect("ObjectiveArrow", panel, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, -28), new Vector2(78, 28));
        var img = arrowRt.gameObject.AddComponent<Image>();
        img.sprite = ArrowSprite();
        img.color = Cyan;
        img.raycastTarget = false;
        arrowRt.gameObject.SetActive(false);

        // World marker: a cyan pyramid that bobs above the objective when it is in view
        var oldMarker = GameObject.Find("ObjectiveMarker");
        if (oldMarker) Object.DestroyImmediate(oldMarker);
        var marker = new GameObject("ObjectiveMarker");
        marker.AddComponent<MeshFilter>().sharedMesh = MarkerMesh();
        marker.AddComponent<MeshRenderer>().sharedMaterial = Mat("Objective Marker", Cyan, true);
        marker.SetActive(false);

        arrow.head = cam;
        arrow.panelArrow = arrowRt;
        arrow.marker = marker.transform;
        EditorUtility.SetDirty(arrow);

        // The shop and computer targets pointed at the Home anchor on the floor
        tm.arrow = arrow;
        tm.head = cam;
        var store = GameObject.Find("StoreCanvas");
        var screen = GameObject.Find("ScreenCanvas");
        if (store) tm.shopTarget = store.transform;
        if (screen) tm.computerTarget = screen.transform;
        EditorUtility.SetDirty(tm);
    }

    // Upward chevron: a triangle with a notch cut from its base
    static Sprite ArrowSprite()
    {
        return TexSprite("Assets/Textures/ObjectiveArrow.png", (x, y, n) =>
        {
            float half = (n - 1 - y) * 0.5f, cx = Mathf.Abs(x - (n - 1) * 0.5f);
            return y >= 4 && cx <= half && !(y < 26 && cx <= (26 - y) * 0.9f);
        });
    }

    // A 64 x 64 white sprite with the given shape, imported as a single sprite
    static Sprite TexSprite(string path, System.Func<int, int, int, bool> inside)
    {
        if (!System.IO.File.Exists(path))
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    tex.SetPixel(x, y, inside(x, y, n) ? Color.white : Color.clear);
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
        }
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Mesh MarkerMesh()
    {
        const string path = Prefabs + "ObjectiveMarker.asset";
        var m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (m) return m;
        float w = 0.06f, h = 0.12f;
        var v = new[] { new Vector3(0, 0, 0), new Vector3(-w, h, -w), new Vector3(w, h, -w), new Vector3(w, h, w), new Vector3(-w, h, w) };
        var tris = new List<int> { 0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 1, 1, 4, 3, 1, 3, 2 };
        for (int i = 0, c = tris.Count; i < c; i += 3) tris.AddRange(new[] { tris[i], tris[i + 2], tris[i + 1] });   // both sides, so culling never hides it
        m = new Mesh { name = "ObjectiveMarker", vertices = v, triangles = tris.ToArray() };
        m.RecalculateNormals();
        m.RecalculateBounds();
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // ---------- prefabs ----------

    static GameObject BuildScanner()
    {
        var root = new GameObject("Scanner");
        var box = root.AddComponent<BoxCollider>();
        box.size = new Vector3(0.05f, 0.04f, 0.16f);
        root.AddComponent<Rigidbody>();
        var grab = root.AddComponent<XRGrabInteractable>();
        grab.useDynamicAttach = true;

        Visual(PrimitiveType.Cube, "Body", root.transform, Vector3.zero, new Vector3(0.05f, 0.04f, 0.16f), Mat("Scanner Body", Hex("2B3A5C")));
        Visual(PrimitiveType.Cube, "Emitter", root.transform, new Vector3(0, 0, 0.085f), new Vector3(0.03f, 0.02f, 0.01f), Mat("Scanner Emitter", Cyan, true));

        var tip = new GameObject("Tip").transform;
        tip.SetParent(root.transform, false);
        tip.localPosition = new Vector3(0, 0, 0.09f);
        var beam = tip.gameObject.AddComponent<LineRenderer>();
        beam.positionCount = 2;
        beam.widthMultiplier = 0.004f;
        beam.useWorldSpace = true;
        beam.sharedMaterial = Mat("Scanner Beam", Cyan, true);
        beam.enabled = false;

        var s = root.AddComponent<Scanner>();
        s.tip = tip;
        s.beam = beam;
        root.AddComponent<TutorialGrabEvent>().id = "Scanner";
        return Save(root, "Scanner");
    }

    static GameObject BuildSparePart()
    {
        var root = new GameObject("SparePart");
        root.AddComponent<BoxCollider>().size = Vector3.one * 0.08f;
        root.AddComponent<Rigidbody>();
        root.AddComponent<XRGrabInteractable>().useDynamicAttach = true;
        root.AddComponent<RobotPart>();
        Visual(PrimitiveType.Cylinder, "Visual", root.transform, Vector3.zero, new Vector3(0.05f, 0.04f, 0.05f), Mat("Spare Part", Amber));
        return Save(root, "SparePart");
    }

    // ---------- robot ----------

    static void AddAntennaSocket()
    {
        string path = Prefabs + "RobotRoot.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        var old = root.transform.Find("MissingAntenna");
        var faults = root.GetComponent<RobotFaults>();
        if (old) { faults.possibleFaults.Remove(old.GetComponent<Fault>()); Object.DestroyImmediate(old.gameObject); }

        var spot = new GameObject("MissingAntenna");
        spot.transform.SetParent(root.transform, false);
        spot.transform.localPosition = new Vector3(0, 1.98f, 0);    // top of the head; move it in the prefab if it floats
        spot.transform.localScale = Vector3.one * 0.05f;
        var col = spot.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = Vector3.one * 2f;

        var antenna = Visual(PrimitiveType.Cylinder, "Fixed Visual", spot.transform, new Vector3(0, 2f, 0), new Vector3(0.4f, 2f, 0.4f), Mat("Spare Part", Amber));
        var socket = Visual(PrimitiveType.Cube, "Broken Visual", spot.transform, Vector3.zero, new Vector3(1.2f, 0.3f, 1.2f), Mat("Empty Socket", Hex("FF5A4D"), true));

        var f = spot.AddComponent<Fault>();
        f.region = RepairRegion.Head;
        f.symptom = "Missing antenna";
        f.requiredTool = ToolType.Part;
        f.workNeeded = 1f;
        f.pay = 150;
        f.overHitGrace = 0;
        f.fixedVisual = antenna;
        f.brokenVisual = socket;
        faults.possibleFaults.Add(f);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void AddDatabaseEntry()
    {
        var db = AssetDatabase.LoadAssetAtPath<RepairDatabase>("Assets/RepairDatabase.asset");
        if (!db || db.repairs.Any(r => r.tool == ToolType.Part)) return;
        db.repairs.Add(new RepairEntry { symptom = "Missing antenna", region = RepairRegion.Head, tool = ToolType.Part, workNeeded = 1f, minLevel = 1, pay = 150 });
        EditorUtility.SetDirty(db);
    }

    // ---------- store screen ----------

    static void BuildStore(GameObject scannerPrefab, GameObject partPrefab)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var stock = Object.FindAnyObjectByType<ToolStock>();
        var energy = Object.FindAnyObjectByType<EnergySystem>();
        var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(c => c.name == "StoreCanvas");

        var list = stock.entries.ToList();
        if (!list.Any(e => e.tool == ToolType.Scanner))
            list.Add(new ToolStock.Entry { label = "Scanner", tool = ToolType.Scanner, prefab = scannerPrefab, price = 75, starter = false });
        foreach (var e in list) if (e.label == "Can") e.label = "Spray Can";
        stock.entries = list.ToArray();
        stock.partPrefab = partPrefab;
        EditorUtility.SetDirty(stock);

        if (ReferencedFromOutside(canvas.transform)) return;

        var oldShop = canvas.GetComponent<ShopScreen>();
        if (oldShop) Object.DestroyImmediate(oldShop);
        for (int i = canvas.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(canvas.transform.GetChild(i).gameObject);

        var title = Font("ChakraPetch-Regular SDF");
        var mono = Font("ShareTechMono-Regular SDF");
        var ct = canvas.transform;

        var bg = Rect("Background", ct, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bg.gameObject.AddComponent<Image>().color = new Color(Ink.r, Ink.g, Ink.b, 0.9f);

        var header = Label("Header", ct, "ROBODOC//STORE", title, 28, Cyan, TextAlignmentOptions.Left);
        Place(header.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -44), new Vector2(290, -6));
        var money = Label("MoneyText", ct, "0 G", title, 28, Amber, TextAlignmentOptions.Right);
        Place(money.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-100, -44), new Vector2(-16, -6));

        var divider = Rect("Divider", ct, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -50), new Vector2(-10, -48));
        divider.gameObject.AddComponent<Image>().color = Line;

        var listRt = Rect("Rows", ct, Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -58));
        var v = listRt.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 6;
        v.childControlHeight = v.childControlWidth = true;
        v.childForceExpandHeight = false;
        v.childForceExpandWidth = true;

        var shop = canvas.gameObject.AddComponent<ShopScreen>();
        shop.stock = stock;
        shop.moneyText = money;
        var rows = new List<ShopScreen.Row>();
        for (int i = 0; i < stock.entries.Length; i++)
        {
            var e = stock.entries[i];
            var row = Row(listRt, e.label.ToUpper(), mono);
            var r = new ShopScreen.Row();
            r.buy = Btn(row, "Buy", e.price + "G", mono, Amber, out r.buyLabel);
            UnityEventTools.AddIntPersistentListener(r.buy.onClick, stock.BuyByIndex, i);
            r.respawn = Btn(row, "Respawn", "RESPAWN", mono, Text, out _);
            UnityEventTools.AddIntPersistentListener(r.respawn.onClick, stock.RespawnByIndex, i);
            rows.Add(r);
        }
        shop.rows = rows.ToArray();

        var refillRow = Row(listRt, "+" + energy.refillAmount + " ENERGY", mono);
        shop.refill = Btn(refillRow, "Refill", energy.refillPrice + "G", mono, Amber, out shop.refillLabel);
        UnityEventTools.AddPersistentListener(shop.refill.onClick, energy.BuyRefill);
        Spacer(refillRow);

        var partRow = Row(listRt, "SPARE PART", mono);
        shop.takePart = Btn(partRow, "TakePart", "TAKE (0)", mono, Text, out shop.takePartLabel);
        UnityEventTools.AddPersistentListener(shop.takePart.onClick, stock.TakePart);
        Spacer(partRow);

        EditorUtility.SetDirty(canvas.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // The store's children are about to be deleted: stop if anything outside the canvas points at them
    static bool ReferencedFromOutside(Transform canvas)
    {
        var doomed = new HashSet<Object>();
        foreach (Transform child in canvas)
            foreach (var t in child.GetComponentsInChildren<Transform>(true))
            {
                doomed.Add(t.gameObject);
                foreach (var c in t.GetComponents<Component>()) doomed.Add(c);
            }

        bool found = false;
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!mb || doomed.Contains(mb) || mb.transform == canvas) continue;
            var it = new SerializedObject(mb).GetIterator();
            while (it.Next(true))
                if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue && doomed.Contains(it.objectReferenceValue))
                {
                    Debug.LogError("RoboDocSetup: " + mb.name + "." + mb.GetType().Name + " " + it.propertyPath + " points at " + it.objectReferenceValue.name + " in the store. Store not rebuilt.");
                    found = true;
                }
        }
        return found;
    }

    static RectTransform Row(Transform parent, string label, TMP_FontAsset font)
    {
        var rt = new GameObject(label, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.gameObject.AddComponent<Image>().color = Panel;
        rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 38;
        var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(10, 4, 4, 4);
        h.spacing = 6;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlHeight = h.childControlWidth = true;
        h.childForceExpandHeight = true;
        h.childForceExpandWidth = false;
        var t = Label("Label", rt, label, font, 20, Text, TextAlignmentOptions.Left);
        t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        return rt;
    }

    static Button Btn(RectTransform row, string name, string text, TMP_FontAsset font, Color textColor, out TMP_Text label)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(row, false);
        go.AddComponent<LayoutElement>().preferredWidth = 104;
        var img = go.AddComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        var b = go.AddComponent<Button>();
        b.targetGraphic = img;
        b.colors = new ColorBlock
        {
            normalColor = Raised,
            highlightedColor = Hover,               // pointer over it
            pressedColor = Cyan,                    // trigger pulled
            selectedColor = Raised,                 // don't stay lit after a click
            disabledColor = new Color(Panel.r, Panel.g, Panel.b, 0.6f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f,
        };
        label = Label("Text", go.transform, text, font, 18, textColor, TextAlignmentOptions.Center);
        Place(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return b;
    }

    static void Spacer(RectTransform row)
    {
        var go = new GameObject("Spacer", typeof(RectTransform));
        go.transform.SetParent(row, false);
        go.AddComponent<LayoutElement>().preferredWidth = 104;
    }

    static TMP_Text Label(string name, Transform parent, string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        if (font) t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.raycastTarget = false;
        return t;
    }

    static RectTransform Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        Place(rt, aMin, aMax, oMin, oMax);
        return rt;
    }

    static void Place(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
    }

    // ---------- helpers ----------

    static GameObject Visual(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go;
    }

    static Material Mat(string name, Color c, bool unlit = false)
    {
        string path = Mats + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m)
        {
            m = new Material(Shader.Find(unlit ? "Unlit/Color" : "Standard"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = c;
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject Save(GameObject root, string name)
    {
        var p = PrefabUtility.SaveAsPrefabAsset(root, Prefabs + name + ".prefab");
        Object.DestroyImmediate(root);
        return p;
    }

    static TMP_FontAsset Font(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
        {
            var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (f && f.name == name) return f;
        }
        return null;
    }

    static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }
}
