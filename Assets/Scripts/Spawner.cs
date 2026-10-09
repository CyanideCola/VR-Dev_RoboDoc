using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject robotPrefab;
    public Transform counterPoint;
    public Transform spawnPoint;
    public IntakeScreen intake;
    public bool spawnOnStart = true;

    Robot current;

    public Transform ExitPoint { get { return spawnPoint ? spawnPoint : transform; } }

    void Start() { if (spawnOnStart) SpawnNext(); }

    [ContextMenu("Spawn Next")]
    public void SpawnNext()
    {
        if (current != null) return;
        Transform origin = ExitPoint;
        GameObject go = Instantiate(robotPrefab, origin.position, origin.rotation);
        Robot robot = go.GetComponent<Robot>();
        current = robot;
        robot.state = Robot.State.WalkingToCounter;
        robot.walker.WalkTo(counterPoint, () => intake.Show(robot));
    }

    public void ReleaseCurrent() { current = null; }
}