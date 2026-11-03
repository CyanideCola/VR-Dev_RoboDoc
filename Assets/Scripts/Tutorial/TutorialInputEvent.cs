using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialInputEvent : MonoBehaviour
{
    public InputActionReference teleportActivate;   // XRI RightHand Locomotion / Teleport Mode Activate

    void OnEnable() { if (teleportActivate) teleportActivate.action.performed += OnTeleport; }
    void OnDisable() { if (teleportActivate) teleportActivate.action.performed -= OnTeleport; }
    void OnTeleport(InputAction.CallbackContext c) { TutorialEvents.Raise("TeleportRay"); }
}