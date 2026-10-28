using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public Spawner spawner;
    public GameObject panel;                  // fixed-size panel
    public TMP_Text text;                     // Auto Size on
    public ObjectiveArrow arrow;              // optional
    public Transform head;                    // XR camera
    public Transform shopStand, counterStand, rackStand;      // teleport anchors
    public Transform shopTarget, computerTarget, rackTarget;  // what the arrow points at
    public float reachDistance = 1.5f;
    public bool alwaysRun;                    // tick while testing

    readonly HashSet<string> seen = new HashSet<string>();
    readonly HashSet<ToolType> taught = new HashSet<ToolType>();
    bool running;
    int arrivals;

    void OnEnable() { TutorialEvents.Raised += OnEvent; }
    void OnDisable() { TutorialEvents.Raised -= OnEvent; }

    void Start()
    {
        if (!alwaysRun && PlayerPrefs.GetInt("tutorialDone", 0) == 1) { Finish(true); return; }
        StartCoroutine(Run());
    }

    void Finish(bool spawnFirst)      // starts the real game: timer and energy begin
    {
        running = false;
        panel.SetActive(false);
        if (arrow) arrow.SetTarget(null);
        if (GameProgress.Instance) GameProgress.Instance.running = true;
        if (spawnFirst) spawner.SpawnNext();
    }

    void OnEvent(string id)
    {
        if (id == "Accepted") seen.Clear();
        if (id == "RobotAtCounter") arrivals++;
        seen.Add(id);
        if (id == "Accepted" && !running) StartCoroutine(Hint());
    }

    IEnumerator Run()
    {
        running = true;
        Say("Welcome to RoboDoc! Push the right thumbstick up to show the teleport ray.", null);
        yield return WaitForAny("TeleportRay");

        Say("Aim the ray at the glowing station by the shop and let go.", shopTarget);
        yield return new WaitUntil(() => Near(shopStand));

        Say("Buy the MALLET for 50 gold: point at the button and pull the trigger.", shopTarget);
        yield return WaitForAny("Bought:Hammer");
        Say("Now buy the SPRAY CAN, also 50 gold.", shopTarget);
        yield return WaitForAny("Bought:SprayCan");

        Say("Squeeze the grip to pick up the mallet. Let go to put it down.", shopTarget);
        yield return WaitForAny("Grab:Mallet");

        Say("Teleport to the station by the computer.", computerTarget);
        yield return new WaitUntil(() => Near(counterStand));

        Say("A patient is on its way.", computerTarget);
        spawner.SpawnNext();
        yield return WaitForAny("RobotAtCounter");

        Say("Read the complaint, then point at ACCEPT and pull the trigger.", computerTarget);
        yield return WaitForAny("Accepted");

        Say("The robot is in the rack. It costs energy until you press DONE. Teleport to the rack station.", rackTarget);
        yield return new WaitUntil(() => Near(rackStand));

        Fault f = FirstOpenFault(false);
        if (f) { taught.Add(f.requiredTool); Say(RepairText(f), rackTarget); }
        else Say("Repair the damage on the robot.", rackTarget);
        yield return WaitForAny("Fixed", "Done");

        Say("Fixed! Press DONE above the robot. It stops the energy cost and pays you.", rackTarget);
        yield return WaitForAny("Done");

        Say("Your money, level and timer are on the computer. The next patient is already coming.", computerTarget);
        yield return new WaitUntil(() => arrivals >= 2);

        PlayerPrefs.SetInt("tutorialDone", 1);
        PlayerPrefs.Save();
        Finish(false);
    }

    // After the tutorial: the first time a robot needs a tool you haven't been shown.
    IEnumerator Hint()
    {
        yield return null;
        Fault f = FirstOpenFault(true);
        if (!f) yield break;
        taught.Add(f.requiredTool);
        Say(RepairText(f), null);
        yield return WaitForAny("Fixed", "Done");
        if (!running) panel.SetActive(false);
    }

    // Only robots that are accepted (in the rack), never the one waiting at the counter.
    Fault FirstOpenFault(bool onlyUntaught)
    {
        foreach (var rf in FindObjectsByType<RobotFaults>(FindObjectsSortMode.None))
        {
            var robot = rf.GetComponent<Robot>();
            if (!robot || robot.state != Robot.State.Repair) continue;
            foreach (var f in rf.reported)
                if (f && !f.isFixed && (!onlyUntaught || !taught.Contains(f.requiredTool))) return f;
        }
        return null;
    }

    string RepairText(Fault f)
    {
        switch (f.requiredTool)
        {
            case ToolType.Hammer:
                return f.symptom + ": use the MALLET. Swing it hard at the damaged spot, " + Mathf.CeilToInt(f.workNeeded) + " hits. Soft taps don't count.";
            case ToolType.SprayCan:
                return f.symptom + ": use the SPRAY CAN. Aim at the rust and hold the trigger for " + Mathf.CeilToInt(f.workNeeded) + " seconds.";
            default:
                return f.symptom + ": use the matching tool on the damaged spot.";
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

    void Say(string s, Transform target)
    {
        panel.SetActive(true);
        text.text = s;
        if (arrow) arrow.SetTarget(target);
    }
}