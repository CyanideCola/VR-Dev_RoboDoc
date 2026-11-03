using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class TutorialGrabEvent : MonoBehaviour
{
    public string id = "Mallet";

    void OnEnable() { GetComponent<XRGrabInteractable>().selectEntered.AddListener(OnGrab); }
    void OnDisable() { GetComponent<XRGrabInteractable>().selectEntered.RemoveListener(OnGrab); }
    void OnGrab(SelectEnterEventArgs a) { TutorialEvents.Raise("Grab:" + id); }
}