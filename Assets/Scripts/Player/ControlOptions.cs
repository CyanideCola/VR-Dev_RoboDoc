using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

// Player-chosen controls. Saved as PlayerPrefs and applied to the rig whenever a scene loads.
// The right thumbstick always teleports; "left stick teleport" lets the left one do it too.
public static class ControlOptions
{
    const string LeftKey = "leftStickTeleport";

    public static bool LeftStickTeleport
    {
        get { return PlayerPrefs.GetInt(LeftKey, 0) == 1; }
        set { PlayerPrefs.SetInt(LeftKey, value ? 1 : 0); PlayerPrefs.Save(); Apply(); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += (s, m) => Apply();
        Apply();
    }

    public static void Apply()
    {
        foreach (var c in Object.FindObjectsByType<ControllerInputActionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.name.StartsWith("Left"))
                c.smoothMotionEnabled = !LeftStickTeleport;   // smooth motion off means the stick teleports instead
    }
}
