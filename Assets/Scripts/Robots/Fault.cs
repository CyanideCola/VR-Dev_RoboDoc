using UnityEngine;

public class Fault : MonoBehaviour
{
    public RepairRegion region = RepairRegion.Head;
    // Fallback values. A RepairDatabase overwrites them at spawn.
    public string symptom = "Dented head";
    public ToolType requiredTool = ToolType.Hammer;
    public float workNeeded = 3f;
    public int pay = 100;
    public int overHitGrace = 2;         // extra hammer hits allowed after it is fixed
    public GameObject brokenVisual;
    public GameObject fixedVisual;
    [HideInInspector] public bool reported;
    [HideInInspector] public bool hidden;    // not on the screen list, and invisible until scanned

    public bool isFixed { get; private set; }
    public bool revealed { get; private set; }
    public bool Visible { get { return !hidden || revealed; } }
    float progress;
    int extraHits;
    float nextHint;
    Robot owner;

    public float Progress01 { get { return workNeeded <= 0f ? 1f : Mathf.Clamp01(progress / workNeeded); } }

    void Awake() { owner = GetComponentInParent<Robot>(); }
    void OnEnable() { Refresh(); }

    public void Apply(RepairEntry e)
    {
        symptom = e.symptom;
        requiredTool = e.tool;
        workNeeded = e.workNeeded;
        pay = e.pay;
    }

    public void Work(ToolType tool, float amount)
    {
        if (tool != requiredTool) return;                       // wrong tool: nothing happens
        if (!Visible) return;                                   // hidden fault: scan it first

        if (owner && owner.state != Robot.State.Repair)         // robot not accepted yet
        {
            if (Time.time > nextHint) { Notifications.Push("Accept the patient first"); nextHint = Time.time + 2f; }
            return;
        }

        if (isFixed)
        {
            if (tool == ToolType.Hammer && ++extraHits > overHitGrace)   // over-hit: it breaks again
            {
                isFixed = false; progress = 0f; extraHits = 0; Refresh();
                Notifications.Push("Over-hit! The damage is back");
            }
            return;
        }

        progress += amount;
        if (progress >= workNeeded) { isFixed = true; extraHits = 0; Refresh(); TutorialEvents.Raise("Fixed"); }
    }

    public void Reveal() { revealed = true; Refresh(); }

    // An unscanned hidden fault looks like a healthy part
    void Refresh()
    {
        bool showBroken = !isFixed && Visible;
        if (brokenVisual) brokenVisual.SetActive(showBroken);
        if (fixedVisual) fixedVisual.SetActive(!showBroken);
    }
}