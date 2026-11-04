using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class TutorialInputEvent : MonoBehaviour
{
    public InputActionReference teleportActivate;   // XRI RightHand Locomotion / Teleport Mode Activate

    InputAction left;   // the left thumbstick's teleport action, used when the player turns that option on

    void OnEnable()
    {
        if (teleportActivate) teleportActivate.action.performed += OnTeleport;
        // the sample script keeps its action private, so read it by name
        var field = typeof(ControllerInputActionManager).GetField("m_TeleportMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var c in FindObjectsByType<ControllerInputActionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var reference = field != null && c.name.StartsWith("Left") ? field.GetValue(c) as InputActionReference : null;
            if (reference) { left = reference.action; left.performed += OnTeleport; }
        }
    }

    void OnDisable()
    {
        if (teleportActivate) teleportActivate.action.performed -= OnTeleport;
        if (left != null) { left.performed -= OnTeleport; left = null; }
    }

    void OnTeleport(InputAction.CallbackContext c) { TutorialEvents.Raise("TeleportRay"); }
}
