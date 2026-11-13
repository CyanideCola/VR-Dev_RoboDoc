using UnityEngine;

public class Robot : MonoBehaviour
{
    public enum State { Spawning, WalkingToCounter, WaitingForDecision, Repair, Leaving }
    public State state = State.Spawning;
    public RobotWalker walker;
    public GameObject poofPrefab;       // optional smoke effect
    public string moneySoundName = "Money";
    RobotFaults faults;
    public bool AnyFixed { get { if (!faults) faults = GetComponent<RobotFaults>(); return faults && faults.AnyFixed; } }

    void Awake()
    {
        if (!walker) walker = GetComponent<RobotWalker>();
    }

    public void TeleportTo(Transform t)
    {
        walker.Stop();
        transform.SetPositionAndRotation(t.position, t.rotation);
        StandOn(t.position.y);
        Physics.SyncTransforms();
        foreach (var rb in GetComponentsInChildren<Rigidbody>())
        {
            if (rb.isKinematic) continue;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // The model's feet hang below its root, so lift the robot until its lowest point is on the ground
    public void StandOn(float groundY)
    {
        float feet = LowestPoint();
        if (feet != float.MaxValue) transform.position += Vector3.up * (groundY - feet);
    }

    // World height of the lowest visible part of the model (its feet), or float.MaxValue if nothing is drawn
    float LowestPoint()
    {
        float lowest = float.MaxValue;
        foreach (var r in GetComponentsInChildren<Renderer>(false))
            if (r.enabled && r.gameObject.activeInHierarchy && r.bounds.min.y < lowest) lowest = r.bounds.min.y;
        return lowest;
    }

    public void Handoff()
    {
        if (state != State.Repair) return;
        state = State.Leaving;
        var rf = GetComponent<RobotFaults>();
        int pay = rf ? rf.Evaluate() : 0;
        int missed = rf ? rf.HiddenNotFound : 0;
        var gp = GameProgress.Instance;
        if (gp) { gp.money += pay; gp.hiddenMissed += missed; gp.RobotFinished(); }   // missed faults count before the last robot ends the run
        if (poofPrefab) Instantiate(poofPrefab, transform.position + Vector3.up, Quaternion.identity);
        Notifications.Push("+" + pay + " G");
        if (missed > 0)   // nothing is said when the scanner found them all, or there were none
        {
            string penalty = gp && GameMode.IsChallenge ? "  +" + Mathf.RoundToInt(missed * gp.missedPenaltySeconds) + "s" : "";
            Notifications.Push("<color=#FFC53D>" + missed + (missed == 1 ? " hidden fault" : " hidden faults") + " not found" + penalty + "</color>");
        }
        if (pay > 0 && AudioManager.instance) AudioManager.instance.Play(moneySoundName);
        Destroy(gameObject);
    }

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