using System.Text;
using TMPro;
using UnityEngine;

public class Rack : MonoBehaviour
{
    public Transform[] slots;          // left to right, as the player faces the rack
    public GameObject[] doneButtons;   // one per slot, shown only while that slot is occupied
    public int OccupiedCount { get { int n = 0; foreach (var o in occupants) if (o != null) n++; return n; } }

    Robot[] occupants;

    void Awake()
    {
        occupants = new Robot[slots.Length];
        Refresh();
    }

    public bool HasFreeSlot { get { return FirstFree() >= 0; } }

    int FirstFree()
    {
        for (int i = 0; i < occupants.Length; i++) if (occupants[i] == null) return i;
        return -1;
    }

    public bool Place(Robot r)
    {
        int i = FirstFree();
        if (i < 0) return false;
        occupants[i] = r;
        r.TeleportTo(slots[i]);
        Refresh();
        return true;
    }

    public void Done(int index)
    {
        if (index < 0 || index >= occupants.Length) return;
        Robot r = occupants[index];
        if (r == null) return;
        occupants[index] = null;
        if (r.AnyFixed) r.Handoff();
        else r.Dismantle();
        Refresh();
        TutorialEvents.Raise("Done");
    }

    public void DoneFirst()
    {
        for (int i = 0; i < occupants.Length; i++)
            if (occupants[i] != null) { Done(i); return; }
    }

    void Update() { Refresh(); }   // labels follow repairs (and over-hits) as they happen

    void Refresh()
    {
        if (doneButtons == null) return;
        for (int i = 0; i < doneButtons.Length && i < occupants.Length; i++)
        {
            if (!doneButtons[i]) continue;
            Robot r = occupants[i];
            doneButtons[i].SetActive(r != null);
            if (r == null) continue;
            var label = doneButtons[i].GetComponentInChildren<TMP_Text>(true);
            if (label) label.text = r.AnyFixed ? "DONE" : "DISMANTLE";
        }
    }

    public string Summary()
    {
        var sb = new StringBuilder("RACK\n");
        if (occupants == null) return sb.ToString();
        for (int i = 0; i < occupants.Length; i++)
        {
            sb.Append(i + 1).Append(": ");
            Robot r = occupants[i];
            if (r == null) { sb.Append("empty\n"); continue; }
            var rf = r.GetComponent<RobotFaults>();
            if (rf)
            {
                foreach (var f in rf.reported)
                    sb.Append(f.symptom).Append(f.isFixed ? " [fixed]" : "").Append("; ");
                foreach (var f in rf.Hidden)
                    if (f.revealed) sb.Append(f.symptom).Append(f.isFixed ? " [fixed]" : " [scanned]").Append("; ");
            }
            sb.Append('\n');
        }
        return sb.ToString();
    }
}