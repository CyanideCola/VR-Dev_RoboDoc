using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

// Player-chosen controls. Saved as PlayerPrefs and applied to the rig whenever a scene loads.
// The right thumbstick always teleports. Advanced turns the left stick's camera movement back on;
// without it the left stick teleports too, so nothing ever slides the player by accident.
public static class ControlOptions
{
    const string AdvancedKey = "advancedControls";
    const string ComfortKey = "comfortBlink";

    public static bool Advanced
    {
        get { return PlayerPrefs.GetInt(AdvancedKey, 0) == 1; }
        set { PlayerPrefs.SetInt(AdvancedKey, value ? 1 : 0); PlayerPrefs.Save(); Apply(); }
    }

    // Comfort: the screen blinks to black for a moment on every teleport
    public static bool ComfortBlink
    {
        get { return PlayerPrefs.GetInt(ComfortKey, 0) == 1; }
        set { PlayerPrefs.SetInt(ComfortKey, value ? 1 : 0); PlayerPrefs.Save(); }
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
                c.smoothMotionEnabled = Advanced;   // off: the left stick teleports instead of moving the camera

        // Pushing a stick down used to spin the player a half turn: snap turns stay small now
        foreach (var s in Object.FindObjectsByType<SnapTurnProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            s.enableTurnAround = false;
        foreach (var t in Object.FindObjectsByType<ContinuousTurnProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            t.enableTurnAround = false;
    }
}
