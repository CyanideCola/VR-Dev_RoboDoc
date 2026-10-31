using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// The scanner's display: a live camera picture with a label pinned to every fault it can see.
// Hidden faults stay off the screen until the trigger is down and they are in view; the Scanner then
// reveals them and the label turns amber.
// Drop the ScannerScreen prefab on the scanner model's display (canvas faces the player). It finds the
// Scanner in its parents; the Scanner's scanCamera is the picture. Built by Tools > RoboDoc > Build Scanner Screen.
public class ScannerScreen : MonoBehaviour
{
    public Scanner scanner;
    public RawImage feed;                  // the camera picture
    public RectTransform overlay;          // same rect as the feed; the labels are drawn here
    public RectTransform markerTemplate;   // inactive; copied once for each fault in view
    public RectTransform reticle;          // the scan area
    public Image scanFill;                 // green tint over the scan area while scanning
    public RectTransform sweepLine;        // moves up and down the scan area while scanning
    public TMP_Text statusText, hintText, countText, standbyText;
    public Vector2Int feedSize = new Vector2Int(384, 176);
    public int renderEveryNFrames = 4;     // 72 fps / 4 = 18 pictures a second, cheap enough for the Quest

    static readonly Color Cyan = new Color32(0x19, 0xD3, 0xF5, 0xFF);
    static readonly Color Amber = new Color32(0xFF, 0xC5, 0x3D, 0xFF);
    static readonly Color Green = new Color32(0x3D, 0xDC, 0x84, 0xFF);
    static readonly Color Light = new Color32(0xD9, 0xEE, 0xFF, 0xFF);
    static readonly Color Dim = new Color32(0x8F, 0xA6, 0xC7, 0xFF);

    enum Kind { Scanning, Known, HiddenFound, Fixed }

    class Marker
    {
        public RectTransform root;
        public Image frame, barFill;
        public TMP_Text label, sub;
        public GameObject bar;
        public Fault fault;
        public Kind kind = (Kind)(-1);
    }

    readonly List<Marker> markers = new List<Marker>();
    RenderTexture rt;
    bool wasHeld;
    int lastDone = -1, lastTotal = -1;

    void Awake()
    {
        if (!scanner) scanner = GetComponentInParent<Scanner>();
        if (!scanner || !scanner.scanCamera) { enabled = false; return; }

        rt = new RenderTexture(feedSize.x, feedSize.y, 16) { name = "ScannerFeed", antiAliasing = 1 };
        rt.Create();
        var cam = scanner.scanCamera;
        cam.targetTexture = rt;
        cam.enabled = false;                                  // drawn by hand, a few times a second
        cam.stereoTargetEye = StereoTargetEyeMask.None;       // not part of the headset's view
        cam.farClipPlane = scanner.range + 0.5f;
        feed.texture = rt;
        markerTemplate.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (rt) { rt.Release(); Destroy(rt); }
    }

    void LateUpdate()
    {
        var cam = scanner.scanCamera;
        bool held = scanner.Held, scanning = scanner.Scanning;

        if (held && (!wasHeld || Time.frameCount % Mathf.Max(1, renderEveryNFrames) == 0)) cam.Render();
        wasHeld = held;

        feed.enabled = held;
        standbyText.gameObject.SetActive(!held);
        reticle.gameObject.SetActive(held);
        scanFill.enabled = scanning;
        sweepLine.gameObject.SetActive(scanning);
        if (scanning)
        {
            scanFill.color = new Color(Green.r, Green.g, Green.b, 0.14f + 0.05f * Mathf.Sin(Time.time * 6f));
            float h = reticle.rect.height;
            sweepLine.anchoredPosition = new Vector2(0f, Mathf.PingPong(Time.time * 140f, h) - h * 0.5f);
        }

        // Labels: every fault in view that is known, and hidden ones only while they are being scanned
        int used = 0;
        RobotFaults target = null;
        if (held)
        {
            float best = float.MaxValue;
            foreach (var rf in scanner.Robots)
            {
                if (!rf) continue;
                Vector3 c = cam.WorldToViewportPoint(rf.transform.position + Vector3.up * 1.1f);
                if (!scanner.InViewport(c)) continue;
                float d = new Vector2(c.x - 0.5f, c.y - 0.5f).sqrMagnitude;
                if (d < best) { best = d; target = rf; }
            }
            foreach (var f in scanner.Faults)
            {
                if (!f) continue;
                Vector3 v = cam.WorldToViewportPoint(f.transform.position);
                if (!scanner.InViewport(v)) continue;
                bool unknown = f.hidden && !f.revealed;
                if (unknown && !scanning) continue;   // hidden faults stay hidden until the scan finds them
                Show(GetMarker(used++), f, v, unknown);
                if (target == null) target = scanner.OwnerOf(f);   // a fault in view means its robot is the patient
            }
        }
        for (int i = used; i < markers.Count; i++) markers[i].root.gameObject.SetActive(false);

        // Text
        Set(statusText, !held ? "STANDBY" : scanning ? "SCANNING" : "READY");
        statusText.color = !held ? Dim : scanning ? Amber : Light;
        if (!held) { Set(hintText, ""); Set(countText, ""); lastDone = lastTotal = -1; return; }
        if (target == null) { Set(hintText, "AIM AT A PATIENT"); Set(countText, ""); lastDone = lastTotal = -1; return; }
        Set(hintText, scanning ? "SCANNING..." : "HOLD TRIGGER TO SCAN");

        // Only faults the player can see count, so the total never gives hidden ones away
        int total = 0, done = 0;
        foreach (var f in target.reported) if (f) { total++; if (f.isFixed) done++; }
        foreach (var f in target.Hidden) if (f && f.revealed) { total++; if (f.isFixed) done++; }
        if (done != lastDone || total != lastTotal)
        {
            lastDone = done; lastTotal = total;
            countText.text = "FIXED " + done + "/" + total;
        }
    }

    void Show(Marker m, Fault f, Vector3 viewport, bool unknown)
    {
        m.root.gameObject.SetActive(true);
        m.root.anchoredPosition = new Vector2((viewport.x - 0.5f) * feedSize.x, (viewport.y - 0.5f) * feedSize.y);

        Kind kind = unknown ? Kind.Scanning : f.isFixed ? Kind.Fixed : f.hidden ? Kind.HiddenFound : Kind.Known;
        if (m.fault != f || m.kind != kind)    // text only changes with the fault or its state
        {
            m.fault = f; m.kind = kind;
            string name = f.symptom.ToUpperInvariant();
            switch (kind)
            {
                case Kind.Scanning: m.label.text = "SCANNING"; m.sub.text = ""; break;
                case Kind.Fixed: m.label.text = name; m.sub.text = "FIXED"; break;
                case Kind.HiddenFound: m.label.text = name; m.sub.text = "HIDDEN  " + f.requiredTool.Label(); break;
                default: m.label.text = name; m.sub.text = f.requiredTool.Label(); break;
            }
        }

        Color c = kind == Kind.Scanning ? Light : kind == Kind.Fixed ? Green : kind == Kind.HiddenFound ? Amber : Cyan;
        float age = kind == Kind.HiddenFound ? scanner.SecondsSinceFound(f) : float.MaxValue;
        if (age < 2f && Mathf.Sin(age * 14f) < 0f) c.a = 0.35f;   // flashes for two seconds after it is found
        m.frame.color = c;
        m.label.color = c;
        m.sub.color = c;
        m.bar.SetActive(kind == Kind.Scanning);
        if (kind == Kind.Scanning) m.barFill.fillAmount = scanner.Progress(f);
    }

    Marker GetMarker(int i)
    {
        while (markers.Count <= i)
        {
            var rtf = Instantiate(markerTemplate, overlay);
            markers.Add(new Marker
            {
                root = rtf,
                frame = rtf.Find("Frame").GetComponent<Image>(),
                label = rtf.Find("Label").GetComponent<TMP_Text>(),
                sub = rtf.Find("Sub").GetComponent<TMP_Text>(),
                bar = rtf.Find("Bar").gameObject,
                barFill = rtf.Find("Bar/Fill").GetComponent<Image>(),
            });
        }
        return markers[i];
    }

    static void Set(TMP_Text t, string s) { if (t.text != s) t.text = s; }
}
