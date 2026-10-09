using UnityEngine;

public class IntakeScreen : MonoBehaviour
{
    public GameObject screenRoot;     // the world-space Canvas
    public Transform workshopPoint;
    public Spawner spawner;

    Robot current;

    void Awake() { screenRoot.SetActive(false); }

    public void Show(Robot robot)
    {
        current = robot;
        robot.state = Robot.State.WaitingForDecision;
        screenRoot.SetActive(true);
    }

    public void Accept()
    {
        if (current == null) return;
        Robot r = current;
        current = null;
        screenRoot.SetActive(false);
        r.TeleportTo(workshopPoint);
        r.state = Robot.State.Repair;
        spawner.ReleaseCurrent();
        Debug.Log("Accepted. Robot is in repair mode.");
    }

    public void Decline()
    {
        if (current == null) return;
        Robot r = current;
        current = null;
        screenRoot.SetActive(false);
        r.state = Robot.State.Leaving;
        spawner.ReleaseCurrent();
        r.walker.WalkTo(spawner.ExitPoint, () =>
        {
            Destroy(r.gameObject);
            spawner.SpawnNext();      // test loop: next robot comes in
        });
    }
}