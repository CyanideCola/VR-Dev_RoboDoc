using UnityEngine;

public class Robot : MonoBehaviour
{
    public enum State { Spawning, WalkingToCounter, WaitingForDecision, Repair, Leaving }
    public State state = State.Spawning;
    public RobotWalker walker;
    public GameObject poofPrefab;       // optional smoke effect
    public MoneyPopup popupPrefab;      // floating +$ text
    public float popupHeight = 1.9f;    // above the robot's feet
    public string moneySoundName = "Money";

    void Awake()
    {
        if (!walker) walker = GetComponent<RobotWalker>();
    }

    public void TeleportTo(Transform t)
    {
        walker.Stop();
        transform.SetPositionAndRotation(t.position, t.rotation);
        Physics.SyncTransforms();
        foreach (var rb in GetComponentsInChildren<Rigidbody>())
        {
            if (rb.isKinematic) continue;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void Handoff()
    {
        if (state != State.Repair) return;
        state = State.Leaving;
        var rf = GetComponent<RobotFaults>();
        int pay = rf ? rf.Evaluate() : 0;
        var gp = GameProgress.Instance;
        if (gp) { gp.money += pay; gp.RobotFinished(); }
        if (poofPrefab) Instantiate(poofPrefab, transform.position + Vector3.up, Quaternion.identity);
        Notifications.Push("+" + pay + " G");
        int missed = rf ? rf.HiddenNotFound : 0;
        if (missed > 0)   // nothing is said when the scanner found them all, or there were none
            Notifications.Push("<color=#FFC53D>" + missed + (missed == 1 ? " hidden fault" : " hidden faults") + " not found</color>");
        if (pay > 0 && AudioManager.instance) AudioManager.instance.Play(moneySoundName);
        Destroy(gameObject);
    }

    public bool AnyFixed { get { var rf = GetComponent<RobotFaults>(); return rf && rf.AnyFixed; } }

    // Scrap for parts instead of paying. Costs energy so it is never cheaper than declining.
    public void Dismantle()
    {
        if (state != State.Repair) return;
        state = State.Leaving;
        var gp = GameProgress.Instance;
        if (gp) gp.parts++;
        var es = EnergySystem.Instance;
        if (gp && gp.running && es)
        {
            es.Spend(es.dismantlePenalty);
            Notifications.Push("Dismantled  +1 part  -" + es.dismantlePenalty + " energy");
        }
        else Notifications.Push("Dismantled  +1 part");
        if (poofPrefab) Instantiate(poofPrefab, transform.position + Vector3.up, Quaternion.identity);
        TutorialEvents.Raise("Dismantled");
        Destroy(gameObject);
    }
}