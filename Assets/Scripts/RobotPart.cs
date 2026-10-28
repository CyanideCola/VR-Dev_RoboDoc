using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// A spare part from scrapped robots. Touch it to an empty socket (a Part fault) to install it.
[RequireComponent(typeof(XRGrabInteractable))]
public class RobotPart : MonoBehaviour
{
    public string installSoundName = "";   // add a Sound with this name to the AudioManager

    XRGrabInteractable grab;
    bool used;

    void Awake() { grab = GetComponent<XRGrabInteractable>(); }

    void OnTriggerEnter(Collider other)
    {
        if (used) return;
        var f = other.GetComponentInParent<Fault>();
        if (!f || f.requiredTool != ToolType.Part || f.isFixed) return;
        f.Work(ToolType.Part, f.workNeeded);   // one part always completes the socket
        if (!f.isFixed) return;                // e.g. robot not accepted yet

        used = true;
        if (AudioManager.instance && !string.IsNullOrEmpty(installSoundName)) AudioManager.instance.Play(installSoundName);
        if (grab.isSelected && grab.interactionManager) grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
        Destroy(gameObject);
    }
}
