using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// Hands have no thumbstick: the player points the hand ray at a teleport pad and pinches.
// The pad is let go of on the next frame, which is what makes it teleport, so this works with Hold and Sticky grab alike.
// The hands' interaction layers include Teleport (set by Tools > RoboDoc > Apply Hand Support).
// Put it on the XR Origin.
public class HandTeleport : MonoBehaviour
{
    readonly List<BaseTeleportationInteractable> pads = new List<BaseTeleportationInteractable>();
    readonly List<(IXRSelectInteractor who, BaseTeleportationInteractable pad)> pending = new List<(IXRSelectInteractor, BaseTeleportationInteractable)>();

    void OnEnable()
    {
        pads.AddRange(FindObjectsByType<BaseTeleportationInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        foreach (var p in pads) p.selectEntered.AddListener(OnSelected);
    }

    void OnDisable()
    {
        foreach (var p in pads) if (p) p.selectEntered.RemoveListener(OnSelected);
        pads.Clear();
        pending.Clear();
    }

    void OnSelected(SelectEnterEventArgs args)
    {
        if (!(args.interactorObject is NearFarInteractor) || !HandTracking.IsHand(args.interactorObject)) return;
        pending.Add((args.interactorObject, (BaseTeleportationInteractable)args.interactableObject));
        TutorialEvents.Raise("TeleportRay");
    }

    void Update()
    {
        foreach (var (who, pad) in pending)
            if (pad && who != null && who.IsSelecting(pad))
                pad.interactionManager.SelectExit(who, (IXRSelectInteractable)pad);
        pending.Clear();
    }
}
