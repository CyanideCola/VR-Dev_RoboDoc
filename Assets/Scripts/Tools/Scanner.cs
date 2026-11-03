using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Handheld scanner. While it is held, its screen (ScannerScreen) shows every fault it can see.
// Holding the trigger scans: a hidden fault that stays in view for scanTime is revealed,
// and from then on it is an ordinary fault that can be fixed.
// Works on a robot waiting at the counter too, so the player can scan before accepting.
[RequireComponent(typeof(XRGrabInteractable))]
public class Scanner : MonoBehaviour
{
    public Transform tip;               // blue arrow points out of the lens
    public Camera scanCamera;           // the picture on the screen; a fault is in view when this camera sees it
    public float range = 2f;            // metres
    public float scanTime = 0.8f;       // seconds a hidden fault must stay in view while scanning
    public float viewMargin = 0.08f;    // the outer edge of the picture does not count
    public LineRenderer beam;           // optional
    public string scanSoundName = "";   // add a Sound with this name to the AudioManager

    [Header("Editor preview (play mode, no headset)")]
    public bool previewHeld;            // shows the screen as if the scanner were held
    public bool previewScanning;        // and as if the trigger were down

    XRGrabInteractable grab;
    bool triggerDown;
    float nextRefresh;
    readonly List<RobotFaults> robots = new List<RobotFaults>();
    readonly List<Fault> faults = new List<Fault>();
    readonly Dictionary<Fault, float> dwell = new Dictionary<Fault, float>();
    readonly Dictionary<Fault, float> foundAt = new Dictionary<Fault, float>();
    readonly Dictionary<Fault, RobotFaults> owners = new Dictionary<Fault, RobotFaults>();
    readonly Dictionary<RobotFaults, float> robotDwell = new Dictionary<RobotFaults, float>();
    readonly HashSet<RobotFaults> scannedRobots = new HashSet<RobotFaults>();

    public bool Held { get { return previewHeld || (grab && grab.isSelected); } }
    public bool Scanning { get { return Held && (previewScanning || triggerDown); } }
    public IReadOnlyList<RobotFaults> Robots { get { return robots; } }
    public IReadOnlyList<Fault> Faults { get { return faults; } }

    Transform T { get { return tip ? tip : transform; } }

    // The robot a fault is on
    public RobotFaults OwnerOf(Fault f)
    {
        RobotFaults rf;
        return owners.TryGetValue(f, out rf) ? rf : null;
    }

    // 0 to 1: how far a hidden fault is from being revealed
    public float Progress(Fault f)
    {
        float d;
        return dwell.TryGetValue(f, out d) ? Mathf.Clamp01(d / scanTime) : 0f;
    }

    // Seconds since this scanner revealed the fault, for a flash on the screen
    public float SecondsSinceFound(Fault f)
    {
        float t;
        return foundAt.TryGetValue(f, out t) ? Time.time - t : float.MaxValue;
    }

    // viewport: x and y from 0 to 1 across the picture, z the distance in metres
    public bool InViewport(Vector3 v)
    {
        return v.z > 0.05f && v.z <= range
            && v.x > viewMargin && v.x < 1f - viewMargin
            && v.y > viewMargin && v.y < 1f - viewMargin;
    }

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        if (!scanCamera) scanCamera = GetComponentInChildren<Camera>(true);
    }

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
        triggerDown = false;
        if (beam) beam.enabled = false;
    }

    void OnActivated(ActivateEventArgs a) { triggerDown = true; }
    void OnDeactivated(DeactivateEventArgs a) { triggerDown = false; }
    void OnReleased(SelectExitEventArgs a) { triggerDown = false; }

    void Update()
    {
        if (!Held || !scanCamera)
        {
            if (dwell.Count > 0 || robotDwell.Count > 0) { dwell.Clear(); robotDwell.Clear(); }
            if (beam) beam.enabled = false;
            return;
        }

        if (Time.time >= nextRefresh) { Refresh(); nextRefresh = Time.time + 0.4f; }

        bool scanning = Scanning;
        if (beam)
        {
            beam.enabled = scanning;
            if (scanning)
            {
                beam.SetPosition(0, T.position);
                beam.SetPosition(1, T.position + T.forward * range);
            }
        }

        float dt = Time.deltaTime;
        foreach (var f in faults)
        {
            if (!f || !f.hidden || f.revealed) continue;
            float d;
            dwell.TryGetValue(f, out d);
            if (scanning && InViewport(scanCamera.WorldToViewportPoint(f.transform.position))) d += dt;
            else d = Mathf.Max(0f, d - dt * 2f);   // the bar drains when the fault leaves the picture
            dwell[f] = d;
            if (d >= scanTime) Found(f);
        }

        // The Demo waits for this: the robot has been in view long enough to count as scanned
        if (!scanning) { robotDwell.Clear(); return; }
        foreach (var rf in robots)
        {
            if (!rf || scannedRobots.Contains(rf)) continue;
            float d;
            robotDwell.TryGetValue(rf, out d);
            if (InViewport(scanCamera.WorldToViewportPoint(rf.transform.position + Vector3.up * 1.1f))) d += dt;
            else d = 0f;
            robotDwell[rf] = d;
            if (d >= scanTime) { scannedRobots.Add(rf); TutorialEvents.Raise("Scanned"); }
        }
    }

    void Found(Fault f)
    {
        f.Reveal();
        dwell.Remove(f);
        foundAt[f] = Time.time;
        Notifications.Push("<color=#FFC53D>Hidden fault found: " + f.symptom + "</color>");
        if (AudioManager.instance && !string.IsNullOrEmpty(scanSoundName)) AudioManager.instance.Play(scanSoundName);
        TutorialEvents.Raise("Found");
    }

    void Refresh()
    {
        robots.Clear();
        faults.Clear();
        owners.Clear();
        foreach (var rf in FindObjectsByType<RobotFaults>(FindObjectsSortMode.None))
        {
            robots.Add(rf);
            foreach (var f in rf.reported) { faults.Add(f); owners[f] = rf; }
            foreach (var f in rf.Hidden) { faults.Add(f); owners[f] = rf; }
        }
        // Forget robots and faults that have gone
        var gone = new List<Fault>();
        foreach (var k in dwell.Keys) if (!k) gone.Add(k);
        foreach (var k in gone) dwell.Remove(k);
        gone.Clear();
        foreach (var k in foundAt.Keys) if (!k) gone.Add(k);
        foreach (var k in gone) foundAt.Remove(k);
        scannedRobots.RemoveWhere(r => !r);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(T.position, T.position + T.forward * range);
    }
}
