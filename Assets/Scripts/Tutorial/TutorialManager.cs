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
    public Transform shopStand, counterStand, rackStand, nearStand;      // teleport anchors
    public Transform shopTarget, computerTarget, rackTarget;  // what the arrow points at
    public float reachDistance = 1.5f;
    public float demoEndDelay = 8f;           // seconds on the last Demo message before the menu

    readonly HashSet<string> seen = new HashSet<string>();
    readonly HashSet<ToolType> taught = new HashSet<ToolType>();
    bool running;
    int arrivals;

    void OnEnable() { TutorialEvents.Raised += OnEvent; }
    void OnDisable() { TutorialEvents.Raised -= OnEvent; }

    // The Demo always runs the full lesson, so every visitor gets it
    void Start()
    {
        if (GameMode.IsChallenge) StartChallenge();
        else StartCoroutine(Run());
    }

    // No lesson: starter tools are waiting at the store and the clock starts now
    void StartChallenge()
    {
        var stock = FindAnyObjectByType<ToolStock>();
        if (stock) stock.GrantStarters();
        var gp = GameProgress.Instance;
        if (gp) gp.running = true;
        spawner.SpawnNext();
        Say("CHALLENGE: hand back " + (gp ? gp.challengeRobots : 8) + " robots as fast as you can. Your mallet and spray can are at the store. The clock is running!", shopTarget);
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
        Say("Welcome to RoboDoc! To start, find your right thumbstick and push it up.", null);
        yield return WaitForAny("TeleportRay");

        Say("In this game, you can only move using the teleporters on the floor. Look behind you and try it out.", nearStand);
        yield return new WaitUntil(() => Near(nearStand));

        Say("Let's buy your first tools. Aim the ray at the station by the shop and let go.", shopTarget);
        yield return new WaitUntil(() => Near(shopStand));

        Say("To your right is the shop. Buy the MALLET for 50 gold: point at the button and pull the trigger (index finger).", shopTarget);
        yield return WaitForAny("Bought:Hammer");
        Say("Now buy the SPRAY CAN, also 50 gold.", shopTarget);
        yield return WaitForAny("Bought:SprayCan");

        Say(GrabMode.Toggle
            ? "Press the middle finger trigger to pick up the mallet. Press it again to put it down."
            : "Hold the middle finger trigger to pick up the mallet. Let go to put it down.", shopTarget);
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

        Say("Your money and the rack list are on the computer. The next patient is already coming.", computerTarget);
        yield return new WaitUntil(() => arrivals >= 2);

        // Part two: energy, declining, the scanner and dismantling. Energy drains from here; the Demo has no clock.
        var gp = GameProgress.Instance;
        if (gp) gp.running = true;
        Say("Your energy is the bar at the bottom right. Every robot in the rack drains it, and at zero the shift ends. Refills are sold in the store. Not every patient is worth it: press DECLINE on this one. Declining costs 10 energy.", computerTarget);
        yield return WaitForAny("Declined");

        var stock = FindAnyObjectByType<ToolStock>();
        var scanner = stock ? System.Array.Find(stock.entries, e => e.tool == ToolType.Scanner) : null;
        if (gp && scanner != null && gp.money < scanner.price) gp.money = scanner.price;   // never stuck for gold here
        Say("Some faults are hidden and only show up on a scanner. Teleport to the store and buy the SCANNER.", shopTarget);
        yield return WaitForAny("Bought:Scanner");

        Say("Pick up the scanner: its screen shows every fault it can see. Go to the computer, aim it at the next patient and hold the trigger until it finds the hidden fault.", computerTarget);
        float scannedFor = 0f;   // if the fault is on the far side, move on after 15 seconds of scanning
        yield return new WaitUntil(() => seen.Contains("Found") || (seen.Contains("Scanned") && (scannedFor += Time.deltaTime) > 15f));

        Say("A fault the scanner finds can be fixed like any other, and it pays extra. Faults you never find are reported when you hand a robot back. Now ACCEPT this patient.", computerTarget);
        yield return WaitForAny("Accepted");

        Say("Teleport to the rack. If a robot isn't worth fixing, press DISMANTLE before you fix anything: it costs 10 energy but gives you a spare part. Robots with a red socket on the head need one: press TAKE PART in the store and touch it to the socket. Press DISMANTLE now.", rackTarget);
        yield return WaitForAny("Dismantled", "Done");

        if (gp) gp.running = false;
        Say("That's everything you need. Try the CHALLENGE from the main menu: " + (gp ? gp.challengeRobots : 8) + " robots, fastest time wins. Back to the menu...", null);
        yield return new WaitForSeconds(demoEndDelay);
        running = false;
        if (gp) gp.ToMenu();
    }

    // After the tutorial: the first time a robot needs a tool you haven't been shown.
    IEnumerator Hint()
    {
        yield return null;
        Fault f = FirstOpenFault(true);
        if (!f)
        {
            if (!running) { panel.SetActive(false); if (arrow) arrow.SetTarget(null); }   // e.g. the Challenge's opening message
            yield break;
        }
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
            case ToolType.Part:
                return f.symptom + ": press TAKE PART in the store, then touch the part to the empty socket. Dismantle robots to get parts.";
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