using System.Text;
using TMPro;
using UnityEngine;

public class IntakeScreen : MonoBehaviour
{
    public GameObject intakePanel;    // panel with complaint text and Accept/Decline
    public TMP_Text complaintText;
    public TMP_Text statusText;       // always visible: money, level, rack
    public Spawner spawner;
    public Rack rack;

    Robot current;

    void Awake() { if (intakePanel) intakePanel.SetActive(false); }

    void Start() { InvokeRepeating(nameof(RefreshStatus), 0f, 0.5f); }

    public void RefreshStatus()
    {
        if (!statusText) return;
        var sb = new StringBuilder();
        var gp = GameProgress.Instance;
        if (gp) sb.Append("MONEY: $").Append(gp.money).Append("   LEVEL ").Append(gp.level + 1).Append("\n\n");
        if (rack) sb.Append(rack.Summary());
        statusText.text = sb.ToString();
    }

    public void Show(Robot robot)
    {
        current = robot;
        robot.state = Robot.State.WaitingForDecision;
        var rf = robot.GetComponent<RobotFaults>();

        if (!complaintText) Debug.LogWarning("IntakeScreen: Complaint Text is not assigned.");
        else if (!rf) Debug.LogWarning("IntakeScreen: robot has no RobotFaults component.");
        else if (rf.reported.Count == 0) Debug.LogWarning("IntakeScreen: no reported faults. Is Possible Faults filled in on RobotFaults?");
        else
        {
            var sb = new StringBuilder("INCOMING PATIENT\n");
            foreach (var f in rf.reported) sb.Append("- ").Append(f.symptom).Append('\n');
            complaintText.text = sb.ToString();
        }
        intakePanel.SetActive(true);
    }

    public void Accept()
    {
        if (current == null) return;
        if (!rack.HasFreeSlot)
        {
            if (complaintText) complaintText.text = "RACK FULL\nFinish a robot or decline.";
            return;
        }
        Robot r = current;
        current = null;
        intakePanel.SetActive(false);
        rack.Place(r);
        r.state = Robot.State.Repair;
        spawner.ReleaseCurrent();
        spawner.QueueNext();
        RefreshStatus();
    }

    public void Decline()
    {
        if (current == null) return;
        Robot r = current;
        current = null;
        intakePanel.SetActive(false);
        r.state = Robot.State.Leaving;
        spawner.ReleaseCurrent();
        spawner.QueueNext();
        r.walker.WalkTo(spawner.ExitPoint, () => Destroy(r.gameObject));
    }
}