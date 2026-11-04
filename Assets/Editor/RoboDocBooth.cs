using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Meta;

// Booth build (mixed-reality branch only). Turns the game into mixed reality for a fair booth:
// passthrough instead of the room's walls and floor, no teleporting, and the stations pulled in around one spot.
// The player starts at the spot the headset was recentered on, facing the counter.
// Safe to run again after merging main into this branch: every step sets values rather than adding to them.
public static class RoboDocBooth
{
    const string GameScene = "Assets/Scenes/2 Main Game.unity";
    const string MenuScene = "Assets/Scenes/1 Start Scene.unity";

    // The layout, in metres from the player's spot (+X is straight ahead, -Z is to the right).
    // Shrink or grow these to fit the booth.
    const float CounterShift = -0.3f;   // counter, computer and robot door move this far toward the player
    const float DoorBehindCounter = 1.0f;   // the robot door stands this far behind where the robot waits
    const float ShopShift = 0.2f;       // shop table, screen and printer move this far in from the right
    const float RackNearX = -1.6f, RackFarX = -2.3f;      // the rack's inner and outer slots, behind the player
    const float RackNearZ = 0.65f, RackFarZ = 1.95f;      // and how far each pair is to the sides

    [MenuItem("Tools/RoboDoc/Apply Booth (Mixed Reality)")]
    public static void Apply()
    {
        EnableOpenXRFeatures(BuildTargetGroup.Android);
        EnableOpenXRFeatures(BuildTargetGroup.Standalone);   // for play mode over Link

        // ---------- game ----------
        var game = EditorSceneManager.OpenScene(GameScene);
        Passthrough();

        var origin = Object.FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsInactive.Include);
        var p = origin.transform.position;
        origin.transform.position = new Vector3(p.x, 0f, p.z);   // the game floor is the real floor
        var assist = origin.GetComponent<PlayerHeightAssist>();
        if (assist) assist.enabled = false;

        // the room goes; the floor keeps its collider so dropped tools still land
        SetActive("Room Walls", false);
        SetActive("Room/Table/Tools", false);
        var floor = Find("Room/Floor");
        if (floor) foreach (var r in floor.GetComponentsInChildren<Renderer>(true)) r.enabled = false;

        // counter, computer and robots come closer
        var spawner = Object.FindAnyObjectByType<Spawner>(FindObjectsInactive.Include);
        // positions are set from the game layout's own numbers, so running again never moves anything twice
        SetX("Room/Table/Registration", 2.08f + CounterShift);
        SetX("Computer", 2.36f + CounterShift);
        var counter = spawner.counterPoint;
        var door = spawner.transform;
        counter.SetParent(null, true);
        spawner.spawnPoint.SetParent(null, true);
        counter.position = new Vector3(3.45f + CounterShift, 0f, counter.position.z);
        door.position = new Vector3(counter.position.x + DoorBehindCounter, door.position.y, door.position.z);
        spawner.spawnPoint.position = new Vector3(door.position.x + 0.3f, 0f, spawner.spawnPoint.position.z);
        counter.SetParent(door, true);
        spawner.spawnPoint.SetParent(door, true);

        // shop comes in from the right
        SetZ("Room/Table/Shop", -1.90f + ShopShift);
        SetZ("StoreCanvas", -1.48f + ShopShift);
        SetZ("Printer", -1.72f + ShopShift);
        SetZ("DropPoint", -2.01f + ShopShift);

        // the rack comes in from 6 m behind to just behind the player
        var rack = Object.FindAnyObjectByType<Rack>(FindObjectsInactive.Include);
        Vector3[] slots =
        {
            new Vector3(RackFarX, 0f, -RackFarZ), new Vector3(RackNearX, 0f, -RackNearZ),
            new Vector3(RackNearX, 0f, RackNearZ), new Vector3(RackFarX, 0f, RackFarZ),
        };
        for (int i = 0; i < rack.slots.Length && i < slots.Length; i++) rack.slots[i].position = slots[i];

        // teleport pads: only Home stays, as the "stand here" mark. The rack pads become plain points the tutorial checks.
        foreach (var pad in Object.FindObjectsByType<TeleportationAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            pad.enabled = false;
            if (pad.name.StartsWith("Rack TP Anchor"))
            {
                int i = int.Parse(pad.name.Substring("Rack TP Anchor ".Length)) - 1;
                if (i >= 0 && i < slots.Length) pad.transform.position = slots[i] + Vector3.right * 1.0f;
                var visuals = pad.transform.Find("Visuals");
                if (visuals) visuals.gameObject.SetActive(false);
            }
        }

        var sign = Find("Rack Sign");
        if (sign) sign.position = new Vector3(RackFarX - 0.3f, 3.5f, 0f);

        var booth = GameObject.Find("Booth Mode") ?? new GameObject("Booth Mode");
        if (!booth.GetComponent<BoothMode>()) booth.AddComponent<BoothMode>();

        EditorSceneManager.MarkSceneDirty(game);
        EditorSceneManager.SaveScene(game);

        // ---------- menu ----------
        var menu = EditorSceneManager.OpenScene(MenuScene);
        Passthrough();
        EditorSceneManager.MarkSceneDirty(menu);
        EditorSceneManager.SaveScene(menu);
        AssetDatabase.SaveAssets();
        Debug.Log("RoboDocBooth: done");
    }

    // An AR session, and a camera that clears to transparent so the passthrough shows behind the game
    static void Passthrough()
    {
        if (!Object.FindAnyObjectByType<ARSession>(FindObjectsInactive.Include))
            new GameObject("AR Session").AddComponent<ARSession>();
        var cam = Object.FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsInactive.Include).Camera;
        if (!cam.GetComponent<ARCameraManager>()) cam.gameObject.AddComponent<ARCameraManager>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        EditorUtility.SetDirty(cam);
    }

    static void EnableOpenXRFeatures(BuildTargetGroup group)
    {
        FeatureHelpers.RefreshFeatures(group);
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
        if (!settings) return;
        foreach (var f in new UnityEngine.XR.OpenXR.Features.OpenXRFeature[] { settings.GetFeature<ARSessionFeature>(), settings.GetFeature<ARCameraFeature>() })
        {
            if (!f) continue;
            f.enabled = true;
            EditorUtility.SetDirty(f);
        }
        EditorUtility.SetDirty(settings);
    }

    static Transform Find(string path)
    {
        var parts = path.Split('/');
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == parts[0])
                return parts.Length == 1 ? root.transform : root.transform.Find(string.Join("/", parts, 1, parts.Length - 1));
        return null;
    }

    static void SetActive(string path, bool on)
    {
        var t = Find(path);
        if (t) t.gameObject.SetActive(on);
    }

    static void SetX(string path, float x)
    {
        var t = Find(path);
        if (t) t.position = new Vector3(x, t.position.y, t.position.z);
    }

    static void SetZ(string path, float z)
    {
        var t = Find(path);
        if (t) t.position = new Vector3(t.position.x, t.position.y, z);
    }
}
