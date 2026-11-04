using UnityEngine;

// Asks the headset for a 90 Hz display refresh rate (if it supports it) so the picture is as steady as it can be.
public static class HeadsetSettings
{
    const float Wanted = 90f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (OVRPlugin.systemDisplayFrequency >= Wanted) return;
            foreach (float f in OVRPlugin.systemDisplayFrequenciesAvailable)
                if (Mathf.Approximately(f, Wanted)) { OVRPlugin.systemDisplayFrequency = Wanted; return; }
        }
        catch (System.Exception e) { Debug.LogWarning("Could not set the refresh rate: " + e.Message); }
#endif
    }
}
