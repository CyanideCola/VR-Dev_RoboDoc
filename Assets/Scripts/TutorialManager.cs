using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public Spawner spawner;
    public GameObject panel;          // the tutorial canvas
    public TMP_Text text;
    public Transform head;            // XR camera
    public Transform counterStand;    // teleport station by the computer
    public Transform rackStand;       // teleport station by the rack
    public float reachDistance = 1.5f;
    public bool alwaysRun;   
    public RectTransform panelRect;    // the TutorialPanel's own RectTransform
    public float padding = 20f;
    public float minHeight = 70f;         // tick while testing to replay every time

    readonly HashSet<string> seen = new HashSet<string>();
    readonly HashSet<ToolType> taught = new HashSet<ToolType>();
    bool running;

    void OnEnable() { TutorialEvents.Raised += OnEvent; }
    void OnDisable() { TutorialEvents.Raised -= OnEvent; }

    void Start()
    {
        if (!alwaysRun && PlayerPrefs.GetInt("tutorialDone", 0) == 1)
        {
            panel.SetActive(false);
            spawner.SpawnNext();
            return;
        }
        StartCoroutine(Run());
    }

    void OnEvent(string id)
    {
        if (id == "Accepted") seen.Clear();
        seen.Add(id);
        if (id == "Accepted" && !running) StartCoroutine(Hint());
    }

    IEnumerator Run()
    {
        running = true;
        Say("Welcome to RoboDoc!\nSqueeze the grip to pick up the mallet. Let go to put it down.");
        yield return WaitForAny("Grab:Mallet");

        Say("A patient is on its way.");
        spawner.SpawnNext();
        yield return WaitForAny("RobotAtCounter");

        Say("Read the complaint on the screen. Point at ACCEPT and pull the trigger.");
        yield return WaitForAny("Accepted");

        Say("The robot went into the first free rack slot. The screen lists all four slots. When they are full you cannot accept more.");
        yield return new WaitForSeconds(7f);

        Say("Teleport to the station by the rack. Push the right thumbstick up to show the teleport ray. Aim at the station by the robot and let go.");
        yield return new WaitUntil(() => Near(rackStand));

        Fault f = FirstOpenFault(false);
        if (f) { taught.Add(f.requiredTool); Say("Find the damaged spot on the robot.\n" + RepairText(f)); }
        else Say("Repair the damage on the robot.");
        yield return WaitForAny("Fixed", "Done");

        Say("Fixed! Press DONE above the robot to hand it back. You are paid for every repair you finish.");
        yield return WaitForAny("Done");

        Say("Your money and level are on the computer. The next patient arrives about 8 seconds after you accept or decline, so keep up.");
        yield return new WaitForSeconds(8f);

        PlayerPrefs.SetInt("tutorialDone", 1);
        PlayerPrefs.Save();
        running = false;
        panel.SetActive(false);
    }

    // After the tutorial: the first time a robot needs a tool you have not been shown, show a hint.
    IEnumerator Hint()
    {
        yield return null;
        Fault f = FirstOpenFault(true);
        if (!f) yield break;
        taught.Add(f.requiredTool);
        Say(RepairText(f));
        yield return new WaitForSeconds(10f);
        if (!running) panel.SetActive(false);
    }

    Fault FirstOpenFault(bool onlyUntaught)
    {
        foreach (var rf in FindObjectsByType<RobotFaults>(FindObjectsSortMode.None))
            foreach (var f in rf.reported)
                if (f && !f.isFixed && (!onlyUntaught || !taught.Contains(f.requiredTool))) return f;
        return null;
    }

    string RepairText(Fault f)
    {
        switch (f.requiredTool)
        {
            case ToolType.Hammer:
                return "Dents need the MALLET. Swing it hard at the damaged spot, " + Mathf.CeilToInt(f.workNeeded) + " hits. Soft taps do not count.";
            case ToolType.SprayCan:
                return "Rust needs the SPRAY CAN. Aim it at the rust and hold the trigger for " + Mathf.CeilToInt(f.workNeeded) + " seconds.";
            default:
                return "Use the matching tool on the damaged spot.";
        }
    }

    bool Near(Transform t)
    {
        Vector3 a = head.position, b = t.position;
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z)) < reachDistance;
    }

    IEnumerator WaitForAny(params string[] ids)
    {
        while (true)
        {
            foreach (var id in ids) if (seen.Contains(id)) yield break;
            yield return null;
        }
    }

    void Say(string s)
    {
        panel.SetActive(true);
        text.text = s;
        if (!panelRect) return;
        float w = panelRect.sizeDelta.x;
        float h = text.GetPreferredValues(s, w - padding * 2f, 0f).y + padding * 2f;
        panelRect.sizeDelta = new Vector2(w, Mathf.Max(h, minHeight));
    }
}