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

    // ---------- scene fade ----------

    const string FaderPrefab = Prefabs + "Fader Screen.prefab";
    const string ManagerPrefab = Prefabs + "Transition Manager.prefab";

    [MenuItem("Tools/RoboDoc/Fix Scene Fade")]
    public static void RunFixFade()
    {
        var mat = FadeMaterial();
        FixFaderPrefab(mat);

        var menu = EditorSceneManager.OpenScene(MenuScenePath);
        var menuFader = Object.FindAnyObjectByType<FadeScreen>(FindObjectsInactive.Include);
        // The start scene had swapped in an opaque sample material, so the alpha never did anything
        var prop = new SerializedObject(menuFader.GetComponent<MeshRenderer>()).FindProperty("m_Materials.Array.data[0]");
        PrefabUtility.RevertPropertyOverride(prop, InteractionMode.AutomatedAction);
        var menuManager = Object.FindAnyObjectByType<SceneTransitionManager>(FindObjectsInactive.Include);
        Debug.Log("RoboDocSetup: menu fader material=" + menuFader.GetComponent<MeshRenderer>().sharedMaterial.name
            + " manager.fadeScreen=" + (menuManager.fadeScreen ? menuManager.fadeScreen.name : "null")
            + " camera near=" + Camera.main.nearClipPlane);
        EditorSceneManager.MarkSceneDirty(menu);
        EditorSceneManager.SaveScene(menu);

        var game = EditorSceneManager.OpenScene(ScenePath);
        foreach (var old in Object.FindObjectsByType<FadeScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.DestroyImmediate(old.gameObject);
        foreach (var old in Object.FindObjectsByType<SceneTransitionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.DestroyImmediate(old.gameObject);

        var cam = Camera.main.transform;
        var fader = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FaderPrefab), cam);
        fader.transform.localPosition = new Vector3(0, 0, 0.015f);
        fader.transform.localRotation = Quaternion.identity;
        fader.transform.localScale = Vector3.one;

        var manager = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ManagerPrefab));
        var so = new SerializedObject(manager.GetComponent<SceneTransitionManager>());
        so.FindProperty("fadeScreen").objectReferenceValue = fader.GetComponent<FadeScreen>();
        so.ApplyModifiedProperties();
        Debug.Log("RoboDocSetup: game fader parent=" + fader.transform.parent.name + " manager.fadeScreen=" + manager.GetComponent<SceneTransitionManager>().fadeScreen.name
            + " camera near=" + Camera.main.nearClipPlane);
        EditorSceneManager.MarkSceneDirty(game);
        EditorSceneManager.SaveScene(game);
        AssetDatabase.SaveAssets();
    }

    static Material FadeMaterial()
    {
        const string path = Mats + "Fade Screen.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader = Shader.Find("RoboDoc/Fade Unlit");
        if (!shader) throw new System.Exception("Shader RoboDoc/Fade Unlit not found");
        if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        m.shader = shader;
        m.SetColor("_Color", new Color(0, 0, 0, 0));
        EditorUtility.SetDirty(m);
        return m;
    }

    static void FixFaderPrefab(Material mat)
    {
        var root = PrefabUtility.LoadPrefabContents(FaderPrefab);
        var rend = root.GetComponent<MeshRenderer>();
        rend.sharedMaterial = mat;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // a quad on the head must not cast a shadow into the room
        rend.receiveShadows = false;
        var fade = root.GetComponent<FadeScreen>();
        fade.colorPropertyName = "_Color";
        fade.fadeColor = new Color(0, 0, 0, 0);
        fade.fadeDuration = 1f;
        PrefabUtility.SaveAsPrefabAsset(root, FaderPrefab);
        PrefabUtility.UnloadPrefabContents(root);
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
    static Sprite TexSprite(string path, System.Func<int, int, int, bool> inside, Vector4 border = default(Vector4))
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
        if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single || imp.spriteBorder != border)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.spriteBorder = border;   // left, bottom, right, top: the part that is not stretched
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

    [MenuItem("Tools/RoboDoc/Build Scanner Screen")]
    public static void RunScannerScreen()
    {
        BuildScanner();
        AssetDatabase.SaveAssets();
        Debug.Log("RoboDocSetup: scanner screen done");
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c ? c : go.AddComponent<T>();
    }

    // Placeholder scanner until the real model arrives: a wide slab with a pistol grip, the lens at the
    // front and the screen on the back. The model only has to keep a Tip, a ScanCamera at the lens, an Attach
    // point for the hand, and a ScannerScreen prefab on its display (the canvas faces the player, so +Z points away).
    // Edited in place when it exists, so the ToolStock entry in the scene keeps pointing at it.
    static GameObject BuildScanner()
    {
        var screenPrefab = BuildScannerScreen();
        const string path = Prefabs + "Scanner.prefab";
        bool exists = System.IO.File.Exists(path);
        var root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject("Scanner");
        for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        foreach (var c in root.GetComponents<Collider>()) Object.DestroyImmediate(c);

        root.AddComponent<BoxCollider>().size = new Vector3(0.22f, 0.11f, 0.06f);
        GetOrAdd<Rigidbody>(root);
        var grab = GetOrAdd<XRGrabInteractable>(root);
        grab.colliders.Clear();             // XRI fills it in itself; a stale entry would make the scanner ungrabbable
        grab.useDynamicAttach = false;      // held by the grip, so the screen faces the player

        Visual(PrimitiveType.Cube, "Body", root.transform, Vector3.zero, new Vector3(0.22f, 0.11f, 0.06f), Mat("Scanner Body", Hex("E8EEF5")));
        Visual(PrimitiveType.Cube, "Bezel", root.transform, new Vector3(0, 0, -0.031f), new Vector3(0.2f, 0.1f, 0.002f), Mat("Scanner Bezel", Hex("0A0A0A")));
        var grip = Visual(PrimitiveType.Cube, "Grip", root.transform, new Vector3(0, -0.095f, 0), new Vector3(0.04f, 0.09f, 0.045f), Mat("Scanner Grip", Hex("2B3A5C")));
        grip.AddComponent<BoxCollider>();
        var lens = Visual(PrimitiveType.Cylinder, "Lens", root.transform, new Vector3(0, 0, 0.031f), new Vector3(0.05f, 0.002f, 0.05f), Mat("Scanner Emitter", Cyan, true));
        lens.transform.localRotation = Quaternion.Euler(90, 0, 0);

        var attach = new GameObject("Attach").transform;
        attach.SetParent(root.transform, false);
        attach.localPosition = new Vector3(0, -0.095f, 0);
        grab.attachTransform = attach;

        var tip = new GameObject("Tip").transform;
        tip.SetParent(root.transform, false);
        tip.localPosition = new Vector3(0, 0, 0.034f);
        var beam = tip.gameObject.AddComponent<LineRenderer>();
        beam.positionCount = 2;
        beam.widthMultiplier = 0.004f;
        beam.useWorldSpace = true;
        beam.sharedMaterial = Mat("Scanner Beam", Cyan, true);
        beam.enabled = false;

        // The screen's picture. Drawn by the ScannerScreen a few times a second while the scanner is held.
        var camGo = new GameObject("ScanCamera");
        camGo.transform.SetParent(root.transform, false);
        camGo.transform.localPosition = new Vector3(0, 0, 0.034f);
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Ink;
        cam.fieldOfView = 40f;
        cam.nearClipPlane = 0.03f;
        cam.farClipPlane = 2.5f;
        cam.allowMSAA = false;
        cam.allowHDR = false;
        cam.stereoTargetEye = StereoTargetEyeMask.None;
        cam.enabled = false;

        var screen = (GameObject)PrefabUtility.InstantiatePrefab(screenPrefab, root.transform);
        screen.transform.localPosition = new Vector3(0, 0, -0.0325f);
        screen.transform.localRotation = Quaternion.identity;

        var s = GetOrAdd<Scanner>(root);
        s.tip = tip;
        s.beam = beam;
        s.scanCamera = cam;
        s.range = 2f;
        s.scanTime = 0.8f;
        GetOrAdd<TutorialGrabEvent>(root).id = "Scanner";

        PrefabUtility.SaveAsPrefabAsset(root, path);
        if (exists) PrefabUtility.UnloadPrefabContents(root); else Object.DestroyImmediate(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    // ---------- scanner screen ----------

    // Corner brackets: 2 px lines with 14 px arms and 16 px slices, so any size keeps the same corners
    static Sprite FrameSprite()
    {
        return TexSprite("Assets/Textures/ScannerFrame.png", (x, y, n) =>
        {
            bool onH = y < 2 || y >= n - 2, onV = x < 2 || x >= n - 2;
            bool nearX = x < 16 || x >= n - 16, nearY = y < 16 || y >= n - 16;
            return (onH && nearX) || (onV && nearY);
        }, new Vector4(16, 16, 16, 16));
    }

    static Sprite WhiteSprite() { return TexSprite("Assets/Textures/White.png", (x, y, n) => true); }

    // The display: 400 x 240 at 0.4 mm a pixel (16 x 9.6 cm). Edited in place when it exists.
    static GameObject BuildScannerScreen()
    {
        const string path = Prefabs + "ScannerScreen.prefab";
        bool exists = System.IO.File.Exists(path);
        var root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject("ScannerScreen");
        for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

        var canvas = GetOrAdd<Canvas>(root);
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)root.transform;
        rt.sizeDelta = new Vector2(400, 240);
        rt.localScale = Vector3.one * 0.0004f;
        var screen = GetOrAdd<ScannerScreen>(root);

        var title = Font("ChakraPetch-Regular SDF");
        var mono = Font("ShareTechMono-Regular SDF");
        var frameSprite = FrameSprite();
        var white = WhiteSprite();
        var green = Hex("3DDC84");
        var dim = Hex("8FA6C7");
        const int W = 384, H = 176;                      // the picture
        float marginX = 0.08f * W, marginY = 0.08f * H;  // the scan area: Scanner.viewMargin

        var bg = Rect("Background", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bg.gameObject.AddComponent<Image>().color = Ink;

        var feedRt = Rect("Feed", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-W / 2f, 2 - H / 2f), new Vector2(W / 2f, 2 + H / 2f));
        var feed = feedRt.gameObject.AddComponent<RawImage>();
        feed.raycastTarget = false;
        var overlay = Rect("Overlay", feedRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        overlay.gameObject.AddComponent<RectMask2D>();

        var reticle = Rect("Reticle", overlay, Vector2.zero, Vector2.one, new Vector2(marginX, marginY), new Vector2(-marginX, -marginY));
        var reticleImg = reticle.gameObject.AddComponent<Image>();
        reticleImg.sprite = frameSprite;
        reticleImg.type = Image.Type.Sliced;
        reticleImg.color = new Color(green.r, green.g, green.b, 0.9f);
        reticleImg.raycastTarget = false;
        var fill = Rect("ScanFill", reticle, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var fillImg = fill.gameObject.AddComponent<Image>();
        fillImg.sprite = white;
        fillImg.color = new Color(green.r, green.g, green.b, 0.15f);
        fillImg.raycastTarget = false;
        var sweep = Rect("Sweep", reticle, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -1), new Vector2(0, 1));
        var sweepImg = sweep.gameObject.AddComponent<Image>();
        sweepImg.sprite = white;
        sweepImg.color = new Color(green.r, green.g, green.b, 0.85f);
        sweepImg.raycastTarget = false;

        // One label for a fault: corner brackets around it, its name above, and below it the tool it needs (or a progress bar)
        var marker = Rect("Marker", overlay, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-28, -28), new Vector2(28, 28));
        var mFrame = Rect("Frame", marker, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var mImg = mFrame.gameObject.AddComponent<Image>();
        mImg.sprite = frameSprite;
        mImg.type = Image.Type.Sliced;
        mImg.color = Cyan;
        mImg.raycastTarget = false;
        var mLabel = Label("Label", marker, "SYMPTOM", mono, 18, Cyan, TextAlignmentOptions.Bottom);
        Place(mLabel.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-90, 3), new Vector2(90, 21));
        mLabel.overflowMode = TextOverflowModes.Overflow;
        var mBar = Rect("Bar", marker, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-28, -9), new Vector2(28, -4));
        var mBarImg = mBar.gameObject.AddComponent<Image>();
        mBarImg.color = Raised;
        mBarImg.raycastTarget = false;
        var mFill = Rect("Fill", mBar, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1));
        var mFillImg = mFill.gameObject.AddComponent<Image>();
        mFillImg.sprite = white;
        mFillImg.type = Image.Type.Filled;
        mFillImg.fillMethod = Image.FillMethod.Horizontal;
        mFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        mFillImg.color = Text;
        mFillImg.raycastTarget = false;
        var mSub = Label("Sub", marker, "TOOL", mono, 14, Cyan, TextAlignmentOptions.Top);
        Place(mSub.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-90, -26), new Vector2(90, -10));
        mSub.overflowMode = TextOverflowModes.Overflow;
        marker.gameObject.SetActive(false);

        var titleT = Label("Title", root.transform, "ROBODOC//SCAN", title, 18, Cyan, TextAlignmentOptions.Left);
        Place(titleT.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -28), new Vector2(220, -4));
        var status = Label("Status", root.transform, "STANDBY", mono, 18, dim, TextAlignmentOptions.Right);
        Place(status.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-170, -28), new Vector2(-12, -4));
        var hint = Label("Hint", root.transform, "", mono, 16, dim, TextAlignmentOptions.Left);
        Place(hint.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(12, 6), new Vector2(260, 30));
        var count = Label("Count", root.transform, "", mono, 16, Text, TextAlignmentOptions.Right);
        Place(count.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-150, 6), new Vector2(-12, 30));
        var standby = Label("Standby", root.transform, "STANDBY", mono, 30, dim, TextAlignmentOptions.Center);
        Place(standby.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-150, -14), new Vector2(150, 18));

        screen.scanner = null;                          // found in the parents at runtime
        screen.feed = feed;
        screen.overlay = overlay;
        screen.markerTemplate = marker;
        screen.reticle = reticle;
        screen.scanFill = fillImg;
        screen.sweepLine = sweep;
        screen.statusText = status;
        screen.hintText = hint;
        screen.countText = count;
        screen.standbyText = standby;
        screen.feedSize = new Vector2Int(W, H);
        EditorUtility.SetDirty(screen);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        if (exists) PrefabUtility.UnloadPrefabContents(root); else Object.DestroyImmediate(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
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
