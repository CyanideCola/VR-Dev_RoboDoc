using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ToolStock : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public string label = "Mallet";
        public ToolType tool = ToolType.Hammer;
        public GameObject prefab;
        public int price = 50;
        public bool starter = true;     // starter tools must be owned before the energy refill unlocks
        [HideInInspector] public bool owned;
        [HideInInspector] public GameObject live;
    }

    public Entry[] entries;
    public Transform dropPoint;         // inside the printer's tray
    public float floorY = -0.5f, lostDistance = 3f;
    public GameObject partPrefab;       // spare part, taken from GameProgress.parts

    [Header("Printer")]
    public Transform printer;           // shakes while it prints
    public float printTime = 2f;        // seconds from nothing to a finished item
    public float itemSpacing = 0.14f;   // items bought together sit side by side in the tray

    int printing;                       // items being printed right now
    Vector3 printerHome;

    public bool StartersOwned { get { foreach (var e in entries) if (e.starter && !e.owned) return false; return true; } }

    void Awake() { if (printer) printerHome = printer.position; }

    public void BuyByIndex(int i)
    {
        var e = entries[i];
        if (e.owned) { Notifications.Push("Already owned"); return; }
        if (!GameProgress.Instance.TrySpend(e.price)) { Notifications.Push("Need " + e.price + " gold"); return; }
        e.owned = true;
        Spawn(e);
        TutorialEvents.Raise("Bought:" + e.tool);
    }

    // Challenge: the starter tools are already bought, so the clock measures repairs, not shopping
    public void GrantStarters()
    {
        foreach (var e in entries)
            if (e.starter && !e.owned) { e.owned = true; Spawn(e); }
    }

    public void RespawnByIndex(int i)
    {
        var e = entries[i];
        if (!e.owned) return;
        if (e.live)
        {
            Vector3 p = e.live.transform.position;
            bool lost = p.y < floorY || Vector3.Distance(p, dropPoint.position) > lostDistance;
            if (!lost) { Notifications.Push(e.label + " is still around"); return; }
            Destroy(e.live);
        }
        Spawn(e);
    }

    public void TakePart()
    {
        var gp = GameProgress.Instance;
        if (!partPrefab || !gp) return;
        if (gp.parts <= 0) { Notifications.Push("No parts. Dismantle a robot to get some"); return; }
        gp.parts--;
        StartCoroutine(Print(partPrefab, null));
    }

    void Spawn(Entry e) { StartCoroutine(Print(e.prefab, e)); }

    // The item appears inside the printer and is built up while the printer shakes and the item turns.
    // It cannot be picked up until it is finished.
    IEnumerator Print(GameObject prefab, Entry e)
    {
        int lane = printing++;
        float side = (lane % 2 == 0 ? 1 : -1) * ((lane + 1) / 2) * itemSpacing;
        var go = Instantiate(prefab, dropPoint.position + dropPoint.right * side, dropPoint.rotation);
        if (e != null) e.live = go;

        var rb = go.GetComponent<Rigidbody>();
        bool wasKinematic = rb && rb.isKinematic;
        if (rb) rb.isKinematic = true;
        var grab = go.GetComponent<XRGrabInteractable>();
        if (grab) grab.enabled = false;

        Vector3 full = go.transform.localScale;
        float t = 0f;
        while (t < printTime && go)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / printTime));
            go.transform.localScale = full * Mathf.Max(0.02f, k);
            go.transform.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
            if (printer) printer.position = printerHome + Random.insideUnitSphere * 0.002f;
            yield return null;
        }

        printing--;
        if (printer && printing == 0) printer.position = printerHome;
        if (!go) yield break;
        go.transform.localScale = full;
        go.transform.rotation = dropPoint.rotation;
        if (rb) rb.isKinematic = wasKinematic;
        if (grab) grab.enabled = true;
    }
}
