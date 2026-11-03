using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class TwoHandedGate : MonoBehaviour
{
    XRGrabInteractable grab;
    void Awake() { grab = GetComponent<XRGrabInteractable>(); }
    public bool BothHands { get { return grab.interactorsSelecting.Count >= 2; } }
}