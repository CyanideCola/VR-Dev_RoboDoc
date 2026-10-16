using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GrabDebug : MonoBehaviour
{
    XRGrabInteractable grab;

    void OnEnable()
    {
        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnEnter);
        grab.selectExited.AddListener(OnExit);
    }

    void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnEnter);
        grab.selectExited.RemoveListener(OnExit);
    }

    static string Path(Transform t)
    {
        string s = t.name;
        for (int i = 0; i < 3 && t.parent != null; i++) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }

    void OnEnter(SelectEnterEventArgs a)
    {
        Debug.Log("GRAB    " + Path(a.interactorObject.transform) + " t=" + Time.time.ToString("F2"));
    }

    void OnExit(SelectExitEventArgs a)
    {
        Debug.Log("RELEASE " + Path(a.interactorObject.transform) + " canceled=" + a.isCanceled + " t=" + Time.time.ToString("F2"));
    }
}