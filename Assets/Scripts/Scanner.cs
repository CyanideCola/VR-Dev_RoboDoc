using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Hold the trigger and point at a robot to reveal its hidden faults.
// Works on a robot waiting at the counter too, so the player can scan before accepting.
[RequireComponent(typeof(XRGrabInteractable))]
public class Scanner : MonoBehaviour
{
    public Transform tip;               // blue arrow points out of the emitter
    public float range = 1.5f;          // metres
    public float aimRadius = 0.45f;     // how close the beam must pass to the robot's middle
    public float scanTime = 1f;         // seconds on target
    public LineRenderer beam;           // optional
    public string scanSoundName = "";   // add a Sound with this name to the AudioManager

    XRGrabInteractable grab;
    bool scanning;
    RobotFaults target;
    float onTarget;
    readonly HashSet<RobotFaults> done = new HashSet<RobotFaults>();

    Transform T { get { return tip ? tip : transform; } }

    void Awake() { grab = GetComponent<XRGrabInteractable>(); }

    void OnEnable()
    {
        grab.activated.AddListener(OnActivated);
        grab.deactivated.AddListener(OnDeactivated);
        grab.selectExited.AddListener(OnReleased);
    }

    void OnDisable()
    {
        grab.activated.RemoveListener(OnActivated);
        grab.deactivated.RemoveListener(OnDeactivated);
        grab.selectExited.RemoveListener(OnReleased);
        Stop();
    }

    void OnActivated(ActivateEventArgs a) { scanning = true; }
    void OnDeactivated(DeactivateEventArgs a) { Stop(); }
    void OnReleased(SelectExitEventArgs a) { Stop(); }

    void Stop()
    {
        scanning = false;
        target = null;
        onTarget = 0f;
        if (beam) beam.enabled = false;
    }

    void Update()
    {
        if (!scanning) return;
        Vector3 a = T.position, b = a + T.forward * range;
        if (beam)
        {
            beam.enabled = true;
            beam.SetPosition(0, a);
            beam.SetPosition(1, b);
        }

        RobotFaults best = Aimed(a, b);
        if (best != target) { target = best; onTarget = 0f; }
        if (!target || done.Contains(target)) return;

        onTarget += Time.deltaTime;
        if (onTarget < scanTime) return;

        done.Add(target);
        int n = target.RevealHidden();
        Notifications.Push(n == 0 ? "Scan: nothing hidden" : "Scan: " + n + " hidden fault" + (n > 1 ? "s" : "") + " found");
        if (AudioManager.instance && !string.IsNullOrEmpty(scanSoundName)) AudioManager.instance.Play(scanSoundName);
        TutorialEvents.Raise("Scanned");
    }

    // The robot whose middle is closest to the beam, within aimRadius
    RobotFaults Aimed(Vector3 a, Vector3 b)
    {
        RobotFaults best = null;
        float bestD = aimRadius;
        foreach (var rf in FindObjectsByType<RobotFaults>(FindObjectsSortMode.None))
        {
            Vector3 mid = rf.transform.position + Vector3.up * 1.1f;
            Vector3 ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(mid - a, ab) / ab.sqrMagnitude);
            float d = Vector3.Distance(mid, a + ab * t);
            if (d < bestD) { bestD = d; best = rf; }
        }
        return best;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(T.position, T.position + T.forward * range);
    }
}
