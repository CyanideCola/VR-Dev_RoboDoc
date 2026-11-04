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
    const string Prefabs = "Assets/Prefabs/";
    const string Mats = "Assets/Art/Materials/";
    const string ScenePath = "Assets/Scenes/2 Main Game.unity";

    // Interface palette (section 11 of the design doc)
    static readonly Color Ink = Hex("0B1220"), Panel = Hex("131C2E"), Raised = Hex("1B2740"), Line = Hex("2B3A5C"),
        Text = Hex("D9EEFF"), Cyan = Hex("19D3F5"), Amber = Hex("FFC53D"), Hover = Hex("1E6F8A");

    [MenuItem("Tools/RoboDoc/Build Scanner, Parts and Store")]
    public static void RunAll()
    {
        // This rebuilds the shop screen and would replace any edits made to it by hand
        if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Rebuild the shop screen?", "This replaces the shop screen and any edits made to it by hand.", "Rebuild", "Cancel")) return;
        var scanner = BuildScanner();
        var part = BuildSparePartPlaceholder();
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
        follow.height = -0.5f;
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
        fill.sprite = TexSprite("Assets/Art/Textures/White.png", (x, y, n) => true);
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
        return TexSprite("Assets/Art/Textures/ObjectiveArrow.png", (x, y, n) =>
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

    // ---------- printer, new tools, antenna model, teleport facing, tutorial text ----------

    const string ModelDir = "Assets/Art/Models/";

    static Bounds RenderBounds(GameObject go)
    {
        var b = new Bounds(go.transform.position, Vector3.zero);
        bool first = true;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
        }
        return b;
    }

    static GameObject LoadModel(string file)
    {
        var m = AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + file);
        if (!m) throw new System.Exception("Model not found or not imported: " + ModelDir + file);
        return m;
    }

    // ---------- Nov 4: printer walls, tool grips, antenna, progress bars, tutorial panel, spawn time ----------
    // Targeted on purpose: it does not rebuild the shop screen, which has been edited by hand since.

    [MenuItem("Tools/RoboDoc/Apply Nov 4 Fixes")]
    public static void RunNov4()
    {
        var antennaModel = LoadModel("proto_antenna.blend");
        BuildHandTool("Screwdriver", LoadModel("proto_screwdriver.blend"), ToolType.Screwdriver, 0.22f, new Vector3(0f, 0f, -0.33f));
        BuildHandTool("Drill", LoadModel("proto_drill.blend"), ToolType.Drill, 0.26f, new Vector3(0f, 1.92f, -2.64f));
        FixSparePartGrab();
        FixRobotPrefab(antennaModel);

        var scene = EditorSceneManager.OpenScene(ScenePath);
        var log = new System.Text.StringBuilder();
        AddPrinterColliders(log);

        // The tutorial panel: bigger, with a text area that keeps clear of the arrow on the left
        var tm = Object.FindAnyObjectByType<TutorialManager>(FindObjectsInactive.Include);
        ((RectTransform)tm.panel.transform).sizeDelta = new Vector2(640f, 220f);
        tm.text.rectTransform.offsetMin = new Vector2(90f, 14f);
        tm.text.rectTransform.offsetMax = new Vector2(-20f, -14f);
        tm.text.enableAutoSizing = true;
        tm.text.fontSizeMin = 18f;
        tm.text.fontSizeMax = 32f;
        EditorUtility.SetDirty(tm.text);
        EditorUtility.SetDirty(tm.panel);

        var spawner = Object.FindAnyObjectByType<Spawner>(FindObjectsInactive.Include);
        spawner.spawnDelay = 5f;
        EditorUtility.SetDirty(spawner);

        // The results screen has buttons, so it stays on the layer the controller rays use
        var results = GameObject.Find("ChallengeResultsCanvas");
        if (results)
        {
            var pf = results.GetComponent<PanelFollow>();
            if (pf) { pf.hideFromScanner = false; EditorUtility.SetDirty(pf); }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        System.IO.File.WriteAllText((System.Environment.GetEnvironmentVariable("SHOT_DIR") ?? ".") + "/nov4.txt", log.ToString());
        Debug.Log("RoboDocSetup: Nov 4 fixes done");
    }

    // The printer walls as boxes in the printer own space, so they follow it when it is moved, turned or resized.
    // The front (the model -Z side, where the opening is) stays open so printed items can be taken out.
    static void AddPrinterColliders(System.Text.StringBuilder log)
    {
        GameObject printer = null;   // GameObject.Find skips inactive objects, and the printer may be switched off
        foreach (var tr in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (tr.name == "Printer" && tr.parent == null) { printer = tr.gameObject; break; }
        if (!printer) { log.Append("no Printer in the scene\n"); return; }
        log.Append("printer active in the scene: " + printer.activeSelf + "\n");
        var old = printer.transform.Find("Colliders");
        if (old) Object.DestroyImmediate(old.gameObject);

        var toLocal = printer.transform.worldToLocalMatrix;
        Bounds b = default(Bounds);
        bool first = true;
        foreach (var r in printer.GetComponentsInChildren<MeshRenderer>(true))
        {
            var lb = r.localBounds;
            var m = toLocal * r.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                var corner = lb.center + Vector3.Scale(lb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = m.MultiplyPoint3x4(corner);
                if (first) { b = new Bounds(p, Vector3.zero); first = false; } else b.Encapsulate(p);
            }
        }

        const float t = 0.3f;   // wall thickness in the printer own units, about 3 cm at its size
        var root = new GameObject("Colliders");
        root.transform.SetParent(printer.transform, false);
        void Wall(string n, Vector3 center, Vector3 size)
        {
            var g = new GameObject(n);
            g.transform.SetParent(root.transform, false);
            g.transform.localPosition = center;
            g.AddComponent<BoxCollider>().size = size;
        }
        var c0 = b.center; var s0 = b.size;
        Wall("Bottom", new Vector3(c0.x, b.min.y + t / 2f, c0.z), new Vector3(s0.x, t, s0.z));
        Wall("Top", new Vector3(c0.x, b.max.y - t / 2f, c0.z), new Vector3(s0.x, t, s0.z));
        Wall("Left", new Vector3(b.min.x + t / 2f, c0.y, c0.z), new Vector3(t, s0.y, s0.z));
        Wall("Right", new Vector3(b.max.x - t / 2f, c0.y, c0.z), new Vector3(t, s0.y, s0.z));
        Wall("Back", new Vector3(c0.x, c0.y, b.max.z - t / 2f), new Vector3(s0.x, s0.y, t));
        log.Append($"printer colliders in its own space: center={c0} size={s0}; world scale={printer.transform.lossyScale.x:F4} rotY={printer.transform.eulerAngles.y:F0}\n");
    }

    // Built the way the tool is held: the tip points forward (+Z, the model own axis) and the hand takes the grip point.
    // Edited in place when it exists, so the shop entry in the scene keeps pointing at it.
    static GameObject BuildHandTool(string name, GameObject model, ToolType type, float length, Vector3 gripInModelUnits)
    {
        string path = Prefabs + name + ".prefab";
        bool exists = System.IO.File.Exists(path);
        var root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(name);
        for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        foreach (var c in root.GetComponents<Collider>()) Object.DestroyImmediate(c);

        var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;    // the model own tilt is dropped
        Vector3 nativeScale = visual.transform.localScale;       // some models are stretched on purpose: keep their proportions

        // A mesh with no usable material draws magenta, so give it one (the longer part of a two-material mesh is the metal)
        foreach (var r in visual.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                bool broken = !m || !m.shader || m.shader.name == "Hidden/InternalErrorShader" || !m.shader.isSupported;
                if (!broken) continue;
                bool handle = mats.Length > 1 && i == 0;
                string part = r.name.ToLowerInvariant();
                mats[i] = handle ? Mat("Tool Handle", Hex("FF8A3D"))
                        : part.Contains("battery") || part.Contains("body") ? Mat("Tool Dark", Hex("2A2F3A"))
                        : part.Contains("head") ? Mat("Tool Blue", Hex("2F6BFF"))
                        : Mat("Tool Steel", Hex("9AA7B8"));
            }
            r.sharedMaterials = mats;
        }
        var b = RenderBounds(visual);
        float k = length / Mathf.Max(b.size.x, b.size.y, b.size.z);
        visual.transform.localScale = nativeScale * k;
        var nb = RenderBounds(visual);

        var box = root.AddComponent<BoxCollider>();
        box.center = nb.center - root.transform.position;
        box.size = nb.size;
        GetOrAdd<Rigidbody>(root);
        var grab = GetOrAdd<XRGrabInteractable>(root);
        grab.colliders.Clear();
        var attach = new GameObject("Attach").transform;
        attach.SetParent(root.transform, false);
        attach.localPosition = gripInModelUnits * k;
        attach.localRotation = Quaternion.identity;
        grab.attachTransform = attach;
        grab.useDynamicAttach = false;
        var so = new SerializedObject(grab);
        so.FindProperty("m_FarAttachMode").enumValueIndex = 1;   // Near, like the other tools
        so.ApplyModifiedPropertiesWithoutUndo();
        var head = GetOrAdd<ToolHead>(root);
        head.type = type;
        head.mode = ToolHead.Mode.Rub;      // work builds up while the tool stays on the fault
        head.minSpeed = 0f;
        head.hitSoundName = "";
        GetOrAdd<TutorialGrabEvent>(root).id = name;

        PrefabUtility.SaveAsPrefabAsset(root, path);
        if (exists) PrefabUtility.UnloadPrefabContents(root); else Object.DestroyImmediate(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    static void FixSparePartGrab()
    {
        const string path = Prefabs + "SparePart.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        var so = new SerializedObject(root.GetComponent<XRGrabInteractable>());
        so.FindProperty("m_FarAttachMode").enumValueIndex = 1;   // Near
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // The antenna stands up on its socket (the model lies along its Z axis), with its base on the socket and not off to one side,
    // and every fault spot gets a progress bar
    static void FixRobotPrefab(GameObject antennaModel)
    {
        string path = Prefabs + "RobotRoot.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);

        // the stem base and the length, measured in the model own axes
        var probe = (GameObject)PrefabUtility.InstantiatePrefab(antennaModel);
        probe.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        var verts = new List<Vector3>();
        foreach (var mf in probe.GetComponentsInChildren<MeshFilter>())
            foreach (var v in mf.sharedMesh.vertices) verts.Add(mf.transform.TransformPoint(v));
        Object.DestroyImmediate(probe);
        float zMin = verts.Min(v => v.z), zMax = verts.Max(v => v.z), len = zMax - zMin;
        var stem = verts.Where(v => v.z < zMin + 0.1f * len).ToList();
        var baseNative = new Vector3(stem.Average(v => v.x), stem.Average(v => v.y), zMin);

        var spot = root.transform.Find("MissingAntenna");
        var fault = spot.GetComponent<Fault>();

        // The socket sits on the top of the head, not above it
        Bounds head = default(Bounds);
        bool haveHead = false;
        foreach (var tr in root.GetComponentsInChildren<Transform>(true))
        {
            if (tr.name != "main head") continue;
            foreach (var r in tr.GetComponentsInChildren<Renderer>(true))
            {
                if (!haveHead) { head = r.bounds; haveHead = true; } else head.Encapsulate(r.bounds);
            }
        }
        if (haveHead) spot.position = new Vector3(head.center.x, head.max.y, head.center.z);
        Debug.Log("RoboDocSetup: head top at y=" + (haveHead ? head.max.y.ToString("F3") : "not found") + ", socket now at " + spot.localPosition);

        var oldVisual = spot.Find("Fixed Visual");
        if (oldVisual) Object.DestroyImmediate(oldVisual.gameObject);
        var holder = new GameObject("Fixed Visual");
        holder.transform.SetParent(spot, false);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(antennaModel, holder.transform);
        var upright = Quaternion.Euler(-90f, 0f, 0f);                 // Z becomes up
        float s = 0.30f / (len * spot.lossyScale.x);                  // 30 cm tall
        model.transform.localRotation = upright;
        model.transform.localScale = Vector3.one * s;
        model.transform.localPosition = -(upright * (baseNative * s));   // the stem base sits on the socket
        fault.fixedVisual = holder;

        foreach (var f in root.GetComponentsInChildren<Fault>(true)) AddProgressBar(f);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // A small bar above the spot that fills while the repair is being made (the FaultProgressBar script does the rest)
    static void AddProgressBar(Fault f)
    {
        foreach (var n in new[] { "Bar", "ProgressBar" })
        {
            var old = f.transform.Find(n);
            if (old) Object.DestroyImmediate(old.gameObject);
        }
        var holder = new GameObject("ProgressBar");
        holder.transform.SetParent(f.transform, false);
        holder.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        var visual = new GameObject("Visual", typeof(RectTransform));
        visual.transform.SetParent(holder.transform, false);
        var canvas = visual.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)visual.transform;
        rt.sizeDelta = new Vector2(100f, 14f);
        rt.localScale = Vector3.one * (0.0012f / f.transform.lossyScale.x);   // 12 cm wide
        var back = Rect("Back", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var backImg = back.gameObject.AddComponent<Image>();
        backImg.color = new Color(Ink.r, Ink.g, Ink.b, 0.9f);
        backImg.raycastTarget = false;
        backImg.material = OnTopMaterial();   // never hidden by a part of the robot
        var fillRt = Rect("Fill", rt, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
        var fill = fillRt.gameObject.AddComponent<Image>();
        fill.sprite = WhiteSprite();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.color = Cyan;
        fill.raycastTarget = false;
        fill.material = OnTopMaterial();
        var bar = holder.AddComponent<FaultProgressBar>();
        bar.fault = f;
        bar.visual = visual;
        bar.fill = fill;
        visual.SetActive(false);
    }

    static GameObject BuildSparePart(GameObject antennaModel)
    {
        const string path = Prefabs + "SparePart.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(antennaModel, root.transform);
        visual.transform.localPosition = Vector3.zero;
        var b = RenderBounds(visual);
        float s = 0.16f / Mathf.Max(b.size.x, b.size.y, b.size.z);
        visual.transform.localScale = Vector3.one * s;
        var nb = RenderBounds(visual);
        visual.transform.localPosition -= nb.center - root.transform.position;
        nb = RenderBounds(visual);
        var box = root.GetComponent<BoxCollider>();
        box.center = nb.center - root.transform.position;
        box.size = nb.size;
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    // The robot's antenna socket gets the real antenna model, and two new repairs get their own spots
    static void AddToolSpots(GameObject antennaModel)
    {
        string path = Prefabs + "RobotRoot.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        var faults = root.GetComponent<RobotFaults>();

        var antennaSpot = root.transform.Find("MissingAntenna");
        var oldFixed = antennaSpot.Find("Fixed Visual");
        var fault = antennaSpot.GetComponent<Fault>();
        Object.DestroyImmediate(oldFixed.gameObject);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(antennaModel, antennaSpot);
        model.name = "Fixed Visual";
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;
        var mb = RenderBounds(model);                    // at the spot's scale of 0.05
        float want = 0.30f / 0.05f;                      // 30 cm tall in the spot's own units
        float s = want / Mathf.Max(0.0001f, mb.size.y) * antennaSpot.lossyScale.x;
        model.transform.localScale = Vector3.one * s;
        mb = RenderBounds(model);
        model.transform.position += Vector3.up * (antennaSpot.position.y - mb.min.y);   // base on the socket
        fault.fixedVisual = model;

        foreach (var n in new[] { "LooseScrews", "JammedJoint" })
        {
            var oldSpot = root.transform.Find(n);
            if (oldSpot) { faults.possibleFaults.Remove(oldSpot.GetComponent<Fault>()); Object.DestroyImmediate(oldSpot.gameObject); }
        }
        MakeSpot(root, faults, "LooseScrews", root.transform.Find("Rust").localPosition + new Vector3(0f, -0.32f, 0.14f),
                 "Loose screws", ToolType.Screwdriver, RepairRegion.Torso, 120);
        MakeSpot(root, faults, "JammedJoint", root.transform.Find("ArmDent").localPosition + new Vector3(0f, 0.38f, 0f),
                 "Jammed joint", ToolType.Drill, RepairRegion.Arm, 140);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void MakeSpot(GameObject root, RobotFaults faults, string name, Vector3 pos, string symptom, ToolType tool, RepairRegion region, int pay)
    {
        var spot = new GameObject(name);
        spot.transform.SetParent(root.transform, false);
        spot.transform.localPosition = pos;
        spot.transform.localScale = Vector3.one * 0.05f;
        var col = spot.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = Vector3.one * 2f;
        var good = Visual(PrimitiveType.Cube, "Fixed Visual", spot.transform, Vector3.zero, Vector3.one * 0.8f, Mat("Fixed Bolt", Hex("3DDC84")));
        var bad = Visual(PrimitiveType.Cube, "Broken Visual", spot.transform, Vector3.zero, new Vector3(1.2f, 1.2f, 1.2f), Mat("Empty Socket", Hex("FF5A4D"), true));
        var f = spot.AddComponent<Fault>();
        f.region = region;
        f.symptom = symptom;
        f.requiredTool = tool;
        f.workNeeded = 3f;
        f.pay = pay;
        f.overHitGrace = 0;
        f.fixedVisual = good;
        f.brokenVisual = bad;
        faults.possibleFaults.Add(f);
    }

    // Level 3 brings loose screws, level 4 a jammed joint
    static void AddToolRepairs()
    {
        var db = AssetDatabase.LoadAssetAtPath<RepairDatabase>("Assets/Data/RepairDatabase.asset");
        if (!db.repairs.Any(r => r.tool == ToolType.Screwdriver))
            db.repairs.Add(new RepairEntry { symptom = "Loose screws", region = RepairRegion.Torso, tool = ToolType.Screwdriver, workNeeded = 3f, minLevel = 2, pay = 120 });
        if (!db.repairs.Any(r => r.tool == ToolType.Drill))
            db.repairs.Add(new RepairEntry { symptom = "Jammed joint", region = RepairRegion.Arm, tool = ToolType.Drill, workNeeded = 3f, minLevel = 3, pay = 140 });
        EditorUtility.SetDirty(db);
    }

    // ---------- Nov 5: progress bars drawn on top, and the room walls from the art team ----------

    [MenuItem("Tools/RoboDoc/Apply Nov 5 Fixes")]
    public static void RunNov5()
    {
        // Progress bars again, now drawn over everything
        string path = Prefabs + "RobotRoot.prefab";
        var robot = PrefabUtility.LoadPrefabContents(path);
        foreach (var f in robot.GetComponentsInChildren<Fault>(true)) AddProgressBar(f);
        PrefabUtility.SaveAsPrefabAsset(robot, path);
        PrefabUtility.UnloadPrefabContents(robot);

        PlaceRoom();
        AssetDatabase.SaveAssets();
    }

    static Material RoomMaterial(string wall, Texture2D picture)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art/Materials/Room")) AssetDatabase.CreateFolder("Assets/Art/Materials", "Room");
        string path = "Assets/Art/Materials/Room/" + wall + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(m, path); }
        m.shader = Shader.Find("Standard");
        m.mainTexture = picture;
        m.color = Color.white;
        m.SetFloat("_Glossiness", 0.1f);
        m.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material OnTopMaterial()
    {
        const string path = Mats + "Progress Bar.mat";
        var shader = Shader.Find("RoboDoc/UI On Top");
        if (!shader) throw new System.Exception("Shader RoboDoc/UI On Top not found");
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        m.shader = shader;
        EditorUtility.SetDirty(m);
        return m;
    }

    // proto_room is four walls with no floor (the scene already has one). It is placed so the spawn wall sits just behind the
    // door panel, the room is centred on the play area from side to side, and the wall bottoms are on the floor.
    // The old placeholder walls are switched off, not deleted.
    static void PlaceRoom()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var log = new System.Text.StringBuilder();

        foreach (var tr in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (tr.name == "Room Walls" && tr.parent == null) { Object.DestroyImmediate(tr.gameObject); break; }

        var room = (GameObject)PrefabUtility.InstantiatePrefab(LoadModel("proto_room.blend"));
        room.name = "Room Walls";
        room.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        var b = RenderBounds(room);
        var spawner = Object.FindAnyObjectByType<Spawner>(FindObjectsInactive.Include);
        const float wallThickness = 0.2f;
        float spawnInnerFace = b.max.x - wallThickness;                      // in the model
        float dx = (spawner.transform.position.x + 0.1f) - spawnInnerFace;   // just behind the door panel
        float dz = -b.center.z;                                              // centred side to side
        float dy = -b.min.y;                                                 // wall bottoms on the floor
        room.transform.position = new Vector3(dx, dy, dz);
        log.Append($"model bounds min={b.min:F2} max={b.max:F2}; placed at offset {room.transform.position:F2}\n");

        // The model comes with its materials but not the pictures: each wall material named "X Texture" gets X.png
        foreach (var r in room.GetComponentsInChildren<MeshRenderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (!mats[i] || !mats[i].name.EndsWith(" Texture")) continue;
                string wall = mats[i].name.Substring(0, mats[i].name.Length - " Texture".Length);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Mats + "Models/" + wall + ".png");
                if (!tex) { log.Append("no picture for " + mats[i].name + "\n"); continue; }
                mats[i] = RoomMaterial(wall, tex);
                log.Append($"{r.name}: material {mats[i].name} uses {tex.name}.png\n");
            }
            r.sharedMaterials = mats;
        }

        foreach (var r in room.GetComponentsInChildren<MeshRenderer>())
        {
            if (r.GetComponent<Collider>()) continue;
            var bc = r.gameObject.AddComponent<BoxCollider>();
            bc.center = r.localBounds.center;
            bc.size = r.localBounds.size;
        }
        var nb = RenderBounds(room);
        log.Append($"room now spans x {nb.min.x:F2}..{nb.max.x:F2}, z {nb.min.z:F2}..{nb.max.z:F2}, y {nb.min.y:F2}..{nb.max.y:F2}\n");

        var old = GameObject.Find("Room");
        if (old)
            foreach (Transform c in old.transform)
                if (c.name.StartsWith("Wall (") && c.gameObject.activeSelf) { c.gameObject.SetActive(false); log.Append("switched off " + c.name + "\n"); }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        System.IO.File.WriteAllText((System.Environment.GetEnvironmentVariable("SHOT_DIR") ?? ".") + "/room.txt", log.ToString());
    }

    // ---------- Nov 6: reach for short players, control options, scrolling credits ----------

    [MenuItem("Tools/RoboDoc/Apply Nov 6 Fixes")]
    public static void RunNov6()
    {
        var game = EditorSceneManager.OpenScene(ScenePath);
        var origin = Object.FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsInactive.Include);
        var assist = origin.GetComponent<PlayerHeightAssist>();
        if (!assist) assist = origin.gameObject.AddComponent<PlayerHeightAssist>();
        assist.origin = origin;
        EditorSceneManager.MarkSceneDirty(game);
        EditorSceneManager.SaveScene(game);

        var start = EditorSceneManager.OpenScene(MenuScenePath);
        var menu = Object.FindAnyObjectByType<GameStartMenu>(FindObjectsInactive.Include);
        AddTeleportOption(menu);
        AddScrollingCredits(menu);
        EditorSceneManager.MarkSceneDirty(start);
        EditorSceneManager.SaveScene(start);
        AssetDatabase.SaveAssets();
    }

    // A second row on the options page, copied from the Grab row
    static void AddTeleportOption(GameStartMenu menu)
    {
        var opts = menu.options.GetComponent<SetOptionFromUI>();
        var row = opts.grabDropdown.transform.parent;
        var old = row.parent.Find("Teleport Options");
        if (old) Object.DestroyImmediate(old.gameObject);

        var copy = Object.Instantiate(row.gameObject, row.parent);
        copy.name = "Teleport Options";
        var label = copy.GetComponentInChildren<TMP_Text>(true);
        label.text = "Teleport";
        var labelRt = (RectTransform)label.transform;
        var drop = copy.GetComponentInChildren<TMP_Dropdown>(true);
        var dropRt = (RectTransform)drop.transform;
        const float rowGap = 150f;
        labelRt.anchoredPosition += Vector2.down * rowGap;
        dropRt.anchoredPosition += Vector2.down * rowGap;
        opts.teleportDropdown = drop;
        EditorUtility.SetDirty(opts);

        var grabLabel = row.GetComponentInChildren<TMP_Text>(true);
        grabLabel.text = "Grab";
    }

    // The credits text goes inside a masked window that stops above the BACK button, and rolls
    static void AddScrollingCredits(GameStartMenu menu)
    {
        var page = menu.about.transform;
        var old = page.Find("Credits Window");
        var text = page.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.text.StartsWith("Credits") || t.transform.parent.name == "Credits Window");
        if (old) { text = old.GetComponentInChildren<TMP_Text>(true); }

        GameObject window = old ? old.gameObject : new GameObject("Credits Window", typeof(RectTransform), typeof(RectMask2D));
        var wrt = (RectTransform)window.transform;
        wrt.SetParent(page, false);
        wrt.SetAsFirstSibling();
        // from just above the BACK button (its top is at -219) up to the top of the page
        wrt.anchorMin = wrt.anchorMax = new Vector2(0.5f, 0.5f);
        wrt.pivot = new Vector2(0.5f, 0.5f);
        wrt.sizeDelta = new Vector2(1074f, 660f);
        wrt.anchoredPosition = new Vector2(0f, 120f);   // spans y -210 .. +450

        var trt = text.rectTransform;
        trt.SetParent(wrt, false);
        trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = Vector2.zero;
        trt.sizeDelta = new Vector2(0f, 100f);
        text.alignment = TextAlignmentOptions.Top;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        var fit = text.GetComponent<ContentSizeFitter>();
        if (!fit) fit = text.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var scroll = window.GetComponent<CreditsScroll>();
        if (!scroll) scroll = window.AddComponent<CreditsScroll>();
        scroll.content = trt;
        EditorUtility.SetDirty(scroll);
    }

    // ---------- Hand support: pinch teleport in the game, the hands rig in the menu ----------

    const string HandsRigPrefab = "Assets/Samples/XR Interaction Toolkit/3.3.2/Hands Interaction Demo/Prefabs/XR Origin Hands (XR Rig).prefab";

    [MenuItem("Tools/RoboDoc/Apply Hand Support")]
    public static void RunHands()
    {
        var log = new System.Text.StringBuilder();

        // Game: the hand rays can select teleport pads, and HandTeleport makes a pinch on one teleport
        var game = EditorSceneManager.OpenScene(ScenePath);
        int teleportLayer = UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask.GetMask("Teleport");
        if (teleportLayer == 0) teleportLayer = 1 << 31;
        int padPhysics = 0;
        foreach (var pad in Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.BaseTeleportationInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            foreach (var c in pad.colliders) if (c) padPhysics |= 1 << c.gameObject.layer;
        foreach (var nf in Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!HandUnder(nf.transform)) continue;
            nf.interactionLayers = nf.interactionLayers.value | teleportLayer;
            EditorUtility.SetDirty(nf);
            var caster = nf.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.Casters.CurveInteractionCaster>();
            if (caster && (caster.raycastMask.value & padPhysics) != padPhysics)
            {
                caster.raycastMask = caster.raycastMask.value | padPhysics;
                EditorUtility.SetDirty(caster);
            }
            log.Append($"hand {nf.transform.parent.name}: layers={nf.interactionLayers.value} caster mask={(caster ? caster.raycastMask.value : -1)}\n");
        }
        var origin = Object.FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsInactive.Include);
        GetOrAdd<HandTeleport>(origin.gameObject);
        EditorSceneManager.MarkSceneDirty(game);
        EditorSceneManager.SaveScene(game);

        // Menu: the old controller-only rig is replaced by the same hands rig the game uses
        var menu = EditorSceneManager.OpenScene(MenuScenePath);
        var old = GameObject.Find("Simple XR Player with Ray Interactor");
        if (old)
        {
            var oldOrigin = old.GetComponentInChildren<Unity.XR.CoreUtils.XROrigin>(true);
            var oldCam = oldOrigin.Camera;
            // these live inside the old rig but serve the whole scene
            foreach (var n in new[] { "EventSystem", "InteractionManager" })
            {
                var t = old.transform.Find(n);
                if (t) t.SetParent(null, true);
            }
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HandsRigPrefab));
            rig.transform.SetPositionAndRotation(oldOrigin.transform.position, oldOrigin.transform.rotation);
            var newOrigin = rig.GetComponent<Unity.XR.CoreUtils.XROrigin>();
            newOrigin.RequestedTrackingOriginMode = oldOrigin.RequestedTrackingOriginMode;
            newOrigin.CameraYOffset = oldOrigin.CameraYOffset;
            var loco = rig.transform.Find("Locomotion");
            if (loco) loco.gameObject.SetActive(false);    // no moving or turning in the menu
            foreach (var cim in rig.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets.ControllerInputActionManager>(true))
                cim.smoothMotionEnabled = true;            // and no teleport ray (the move it would enable has no provider)

            var fader = oldCam.GetComponentInChildren<FadeScreen>(true);
            if (fader)
            {
                fader.transform.SetParent(newOrigin.Camera.transform, false);
                fader.transform.localPosition = new Vector3(0, 0, 0.015f);
                fader.transform.localRotation = Quaternion.identity;
                fader.transform.localScale = Vector3.one;
            }

            // anything else in the scene pointing into the old rig: the camera maps to the new camera, the rest is reported
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!mb || mb.transform.IsChildOf(old.transform)) continue;
                var so = new SerializedObject(mb);
                var it = so.GetIterator();
                bool changed = false;
                while (it.Next(true))
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference || !it.objectReferenceValue) continue;
                    var target = it.objectReferenceValue;
                    var go = target is Component comp ? comp.gameObject : target as GameObject;
                    if (!go || !go.transform.IsChildOf(old.transform)) continue;
                    if (go == oldCam.gameObject)
                    {
                        it.objectReferenceValue = target is Camera ? newOrigin.Camera : target is Transform ? newOrigin.Camera.transform : (Object)newOrigin.Camera.gameObject;
                        changed = true;
                        log.Append($"menu: {mb.name}.{mb.GetType().Name} {it.propertyPath} -> new camera\n");
                    }
                    else log.Append($"menu: WARNING {mb.name}.{mb.GetType().Name} {it.propertyPath} still points at {go.name} in the old rig\n");
                }
                if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            }
            old.SetActive(false);   // switched off, not deleted
            log.Append("menu: hands rig placed at " + rig.transform.position + "\n");
        }
        else log.Append("menu: old rig not found (already replaced?)\n");
        EditorSceneManager.MarkSceneDirty(menu);
        EditorSceneManager.SaveScene(menu);
        System.IO.File.WriteAllText((System.Environment.GetEnvironmentVariable("SHOT_DIR") ?? ".") + "/handsetup.txt", log.ToString());
    }

    static bool HandUnder(Transform t)
    {
        for (; t; t = t.parent) if (t.name == "Left Hand" || t.name == "Right Hand") return true;
        return false;
    }

    // ---------- Nov 8: fault colours and spread, BUY PART button ----------

    // The spots a robot can be damaged at, as positions on its body (the robot's own space).
    // The first of each kind is the original spot, which stays where it is.
    static readonly (string source, string name, Vector3 pos, RepairRegion region)[] ExtraSpots =
    {
        ("HeadDent", "HeadDent #2", new Vector3(0.15f, 1.72f, 0.0f), RepairRegion.Head),
        ("HeadDent", "HeadDent #3", new Vector3(-0.12f, 1.50f, 0.005f), RepairRegion.Head),
        ("HeadDent", "HeadDent #4", new Vector3(0.10f, 1.83f, -0.10f), RepairRegion.Head),
        ("ArmDent", "ArmDent #2", new Vector3(0.58f, 0.447f, -0.198f), RepairRegion.Arm),
        ("ArmDent", "ArmDent #3", new Vector3(0.54f, 1.05f, -0.035f), RepairRegion.Arm),
        ("ArmDent", "ArmDent #4", new Vector3(-0.54f, 1.05f, -0.055f), RepairRegion.Arm),
        ("ArmDent", "LegDent #1", new Vector3(0.18f, 0.30f, -0.145f), RepairRegion.Leg),
        ("ArmDent", "LegDent #2", new Vector3(-0.17f, 0.30f, -0.145f), RepairRegion.Leg),
        ("ArmDent", "LegDent #3", new Vector3(-0.17f, 0.05f, -0.145f), RepairRegion.Leg),
        ("Rust", "Rust #2", new Vector3(0.22f, 0.95f, 0.0f), RepairRegion.Torso),
        ("Rust", "Rust #3", new Vector3(0.12f, 0.62f, -0.085f), RepairRegion.Torso),
        ("LooseScrews", "LooseScrews #2", new Vector3(0.18f, 0.47f, -0.145f), RepairRegion.Leg),
        ("LooseScrews", "LooseScrews #3", new Vector3(0.54f, 0.66f, -0.09f), RepairRegion.Arm),
        ("LooseScrews", "LooseScrews #4", new Vector3(-0.25f, 1.55f, 0.0f), RepairRegion.Head),
        ("JammedJoint", "JammedJoint #2", new Vector3(0.58f, 0.82f, -0.17f), RepairRegion.Arm),
        ("JammedJoint", "JammedJoint #3", new Vector3(0.20f, 0.53f, -0.06f), RepairRegion.Torso),
        ("JammedJoint", "JammedJoint #4", new Vector3(0.0f, 1.30f, -0.11f), RepairRegion.Head),
    };

    [MenuItem("Tools/RoboDoc/Apply Nov 8 Fixes")]
    public static void RunNov8()
    {
        // Repair entries: dented legs, and screws / joints on any part of the body
        var db = AssetDatabase.LoadAssetAtPath<RepairDatabase>("Assets/Data/RepairDatabase.asset");
        void Ensure(string symptom, RepairRegion region, ToolType tool, float work, int minLevel, int pay)
        {
            if (!db.repairs.Any(r => r.symptom == symptom && r.region == region && r.tool == tool))
                db.repairs.Add(new RepairEntry { symptom = symptom, region = region, tool = tool, workNeeded = work, minLevel = minLevel, pay = pay });
        }
        Ensure("Dented leg", RepairRegion.Leg, ToolType.Hammer, 3f, 0, 100);
        foreach (RepairRegion r in System.Enum.GetValues(typeof(RepairRegion)))
        {
            Ensure("Loose screws", r, ToolType.Screwdriver, 3f, 2, 120);
            Ensure("Jammed joint", r, ToolType.Drill, 3f, 3, 140);
        }
        EditorUtility.SetDirty(db);

        // Spots on the robot, each kind in its own shade of red, orange or yellow
        string path = Prefabs + "RobotRoot.prefab";
        var robot = PrefabUtility.LoadPrefabContents(path);
        var faults = robot.GetComponent<RobotFaults>();
        foreach (var f in robot.GetComponentsInChildren<Fault>(true).ToArray())
            if (f && f.name.Contains(" #")) { faults.possibleFaults.Remove(f); Object.DestroyImmediate(f.gameObject); }
        foreach (var l in new[] { "LegDent #1", "LegDent #2", "LegDent #3" })
        {
            var stale = robot.transform.Find(l);
            if (stale) { faults.possibleFaults.Remove(stale.GetComponent<Fault>()); Object.DestroyImmediate(stale.gameObject); }
        }
        faults.possibleFaults.RemoveAll(f => !f);

        foreach (var s in ExtraSpots)
        {
            var src = robot.transform.Find(s.source);
            var copy = Object.Instantiate(src.gameObject, src.parent);
            copy.name = s.name;
            copy.transform.localPosition = s.pos;
            copy.transform.localRotation = src.localRotation;
            var f = copy.GetComponent<Fault>();
            f.region = s.region;
            if (s.name.StartsWith("LegDent")) f.symptom = "Dented leg";
            faults.possibleFaults.Add(f);
        }

        var tints = new Dictionary<string, Color>
        {
            { "HeadDent", Hex("E5383B") },      // red
            { "ArmDent", Hex("FF7A1A") },       // orange
            { "LegDent", Hex("FFB020") },       // amber
            { "Rust", Hex("B8460F") },          // rust
            { "LooseScrews", Hex("FFE14D") },   // yellow
            { "JammedJoint", Hex("E8A317") },   // gold
        };
        foreach (var f in robot.GetComponentsInChildren<Fault>(true))
        {
            string kind = f.name.Contains(" #") ? f.name.Substring(0, f.name.IndexOf(" #")) : f.name;
            if (!tints.TryGetValue(kind, out var c) || !f.brokenVisual) continue;
            var r = f.brokenVisual.GetComponent<Renderer>();
            if (r) r.sharedMaterial = Mat("Fault " + kind, c, true);
        }
        PrefabUtility.SaveAsPrefabAsset(robot, path);
        PrefabUtility.UnloadPrefabContents(robot);

        // BUY PART next to TAKE PART, in the shop as it is now
        var game = EditorSceneManager.OpenScene(ScenePath);
        var shop = Object.FindAnyObjectByType<ShopScreen>(FindObjectsInactive.Include);
        var take = shop.takePart.transform;
        var row = take.parent;
        var oldBuy = row.Find("BuyPart");
        if (oldBuy) Object.DestroyImmediate(oldBuy.gameObject);
        var buy = Object.Instantiate(take.gameObject, row);
        buy.name = "BuyPart";
        var btn = buy.GetComponent<Button>();
        for (int i = btn.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(btn.onClick, i);
        var stock = shop.stock;
        UnityEventTools.AddPersistentListener(btn.onClick, stock.BuyPart);
        var label = buy.GetComponentInChildren<TMP_Text>(true);
        label.text = "BUY " + stock.partPrice + "G";
        if (shop.rows.Length > 0 && shop.rows[0].buyLabel) label.color = shop.rows[0].buyLabel.color;   // the shop's gold prices
        var spacer = row.Find("Spacer");
        buy.transform.SetSiblingIndex(spacer ? spacer.GetSiblingIndex() : take.GetSiblingIndex() + 1);
        if (spacer) Object.DestroyImmediate(spacer.gameObject);
        shop.buyPart = btn;
        shop.buyPartLabel = label;
        EditorUtility.SetDirty(shop);

        // A big RACK sign high on the wall behind the rack, readable from the middle of the room
        foreach (var tr in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (tr.name == "Rack Sign") { Object.DestroyImmediate(tr.gameObject); break; }
        var rack = Object.FindAnyObjectByType<Rack>(FindObjectsInactive.Include);
        var room = GameObject.Find("Room Walls");
        float wallX = room ? RenderBounds(room).min.x + 0.2f : -9.15f;   // inner face of the west wall
        var sign = new GameObject("Rack Sign");
        sign.transform.position = new Vector3(wallX + 0.05f, 4.3f, rack.slots.Average(s => s.position.z));
        sign.transform.rotation = Quaternion.LookRotation(Vector3.right * -1f);   // faces into the room
        var signText = sign.AddComponent<TextMeshPro>();
        signText.font = Font("ShareTechMono-Regular SDF");
        signText.text = "RACK";
        signText.fontSize = 24f;
        signText.alignment = TextAlignmentOptions.Center;
        signText.color = Cyan;
        signText.textWrappingMode = TextWrappingModes.NoWrap;
        signText.rectTransform.sizeDelta = new Vector2(8f, 2f);
        EditorSceneManager.MarkSceneDirty(game);
        EditorSceneManager.SaveScene(game);
        AssetDatabase.SaveAssets();
    }

    // ---------- Nov 7: pink computer, bigger antenna socket, NEXT button, comfort option ----------

    [MenuItem("Tools/RoboDoc/Apply Nov 7 Fixes")]
    public static void RunNov7()
    {
        // The antenna socket is high up: a trigger 0.3 m wide instead of 0.1 m
        string path = Prefabs + "RobotRoot.prefab";
        var robot = PrefabUtility.LoadPrefabContents(path);
        foreach (var f in robot.GetComponentsInChildren<Fault>(true))
            if (f.requiredTool == ToolType.Part)
                foreach (var c in f.GetComponentsInChildren<BoxCollider>(true))
                    if (c.isTrigger) c.size = Vector3.one * 6f;   // the socket is scaled 0.05
        PrefabUtility.SaveAsPrefabAsset(robot, path);
        PrefabUtility.UnloadPrefabContents(robot);

        var game = EditorSceneManager.OpenScene(ScenePath);

        // Computer body and printer: empty material slots draw magenta
        var body = Mat("Computer Body", Hex("2A2F3A"));
        foreach (var root in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (root.name != "Computer" && root.name != "Printer") continue;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++) if (!mats[i]) { mats[i] = body; changed = true; }
                if (changed) r.sharedMaterials = mats;
            }
        }

        // NEXT button on the tutorial panel
        var tm = Object.FindAnyObjectByType<TutorialManager>(FindObjectsInactive.Include);
        var canvas = tm.panel.GetComponent<Canvas>();
        if (!tm.panel.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>())
            tm.panel.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
        var panelBg = tm.text.transform.parent;
        var oldBtn = panelBg.Find("Next");
        if (oldBtn) Object.DestroyImmediate(oldBtn.gameObject);
        var next = SmallBtn(panelBg, "Next", "NEXT >", Font("ShareTechMono-Regular SDF"),
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-190, 14), new Vector2(-20, 64));
        next.gameObject.layer = tm.panel.layer;
        next.gameObject.SetActive(false);
        tm.nextButton = next;
        // keep the text clear of the button
        var trt = tm.text.rectTransform;
        trt.offsetMin = new Vector2(trt.offsetMin.x, 66f);
        EditorUtility.SetDirty(tm);
        EditorSceneManager.MarkSceneDirty(game);
        EditorSceneManager.SaveScene(game);

        // Options page: three rows (Grab, Teleport, Comfort), BACK moved down
        var start = EditorSceneManager.OpenScene(MenuScenePath);
        var menu = Object.FindAnyObjectByType<GameStartMenu>(FindObjectsInactive.Include);
        AddComfortOption(menu);
        EditorSceneManager.MarkSceneDirty(start);
        EditorSceneManager.SaveScene(start);
        AssetDatabase.SaveAssets();
    }

    static void AddComfortOption(GameStartMenu menu)
    {
        var opts = menu.options.GetComponent<SetOptionFromUI>();
        var grabRow = opts.grabDropdown.transform.parent;
        var teleRow = opts.teleportDropdown.transform.parent;
        var old = grabRow.parent.Find("Comfort Options");
        if (old) Object.DestroyImmediate(old.gameObject);
        var copy = Object.Instantiate(teleRow.gameObject, grabRow.parent);
        copy.name = "Comfort Options";
        copy.GetComponentInChildren<TMP_Text>(true).text = "Comfort";
        opts.comfortDropdown = copy.GetComponentInChildren<TMP_Dropdown>(true);

        // absolute positions: the Grab row was at dropdown y 28, label y -500.28 (top anchored)
        PlaceRow(grabRow, 100f);
        PlaceRow(teleRow, -30f);
        PlaceRow(copy.transform, -160f);
        foreach (var b in menu.options.GetComponentsInChildren<Button>(true))
            if (b.name == "Validate") ((RectTransform)b.transform).anchoredPosition = new Vector2(0f, -330f);
        EditorUtility.SetDirty(opts);
    }

    static void PlaceRow(Transform row, float dropdownY)
    {
        var label = (RectTransform)row.GetComponentInChildren<TMP_Text>(true).transform;
        var drop = (RectTransform)row.GetComponentInChildren<TMP_Dropdown>(true).transform;
        drop.anchoredPosition = new Vector2(drop.anchoredPosition.x, dropdownY);
        label.anchoredPosition = new Vector2(label.anchoredPosition.x, -500.28f + (dropdownY - 28f));
    }

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
        return TexSprite("Assets/Art/Textures/ScannerFrame.png", (x, y, n) =>
        {
            bool onH = y < 2 || y >= n - 2, onV = x < 2 || x >= n - 2;
            bool nearX = x < 16 || x >= n - 16, nearY = y < 16 || y >= n - 16;
            return (onH && nearX) || (onV && nearY);
        }, new Vector4(16, 16, 16, 16));
    }

    static Sprite WhiteSprite() { return TexSprite("Assets/Art/Textures/White.png", (x, y, n) => true); }

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

    static GameObject BuildSparePartPlaceholder()
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
        var db = AssetDatabase.LoadAssetAtPath<RepairDatabase>("Assets/Data/RepairDatabase.asset");
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
        v.spacing = 3;
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
        rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;
        var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(10, 4, 2, 2);
        h.spacing = 6;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlHeight = h.childControlWidth = true;
        h.childForceExpandHeight = true;
        h.childForceExpandWidth = false;
        var t = Label("Label", rt, label, font, 18, Text, TextAlignmentOptions.Left);
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
        label = Label("Text", go.transform, text, font, 16, textColor, TextAlignmentOptions.Center);
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
