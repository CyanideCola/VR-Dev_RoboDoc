using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Whether the player is using tracked hands instead of controllers, and whether an interactor belongs to a hand.
public static class HandTracking
{
    public static bool Active
    {
        get { return XRInputModalityManager.currentInputMode.Value == XRInputModalityManager.InputMode.TrackedHand; }
    }

    // The hand interactors live under the rig's Left Hand and Right Hand objects
    public static bool IsHand(IXRInteractor interactor)
    {
        if (interactor == null) return false;
        for (var t = interactor.transform; t; t = t.parent)
            if (t.name == "Left Hand" || t.name == "Right Hand") return true;
        return false;
    }

    // Controller wording or hand wording, for text the player reads
    public static string Say(string controllers, string hands) { return Active ? hands : controllers; }
}
