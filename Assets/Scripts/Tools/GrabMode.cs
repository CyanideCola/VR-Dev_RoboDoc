using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Hold to grab (default) or toggle grab: press grip once to pick up, again to drop.
// Saved as a PlayerPref and applied to every hand interactor whenever a scene loads.
public static class GrabMode
{
    const string Key = "grabToggle";

    public static bool Toggle
    {
        get { return PlayerPrefs.GetInt(Key, 0) == 1; }
        set { PlayerPrefs.SetInt(Key, value ? 1 : 0); PlayerPrefs.Save(); Apply(); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += (s, m) => Apply();
        Apply();
    }

    public static void Apply()
    {
        var trigger = Toggle ? XRBaseInputInteractor.InputTriggerType.Toggle : XRBaseInputInteractor.InputTriggerType.StateChange;
        int grabLayers = InteractionLayerMask.GetMask("Default");
        foreach (var i in Object.FindObjectsByType<XRBaseInputInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if ((i.interactionLayers.value & grabLayers) != 0)   // skips the teleport ray, which only uses the Teleport layer
                i.selectActionTrigger = trigger;
    }
}
