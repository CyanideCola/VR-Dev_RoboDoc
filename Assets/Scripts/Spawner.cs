using System.Collections;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject robotPrefab;
    public Transform counterPoint;
    public Transform spawnPoint;
    public IntakeScreen intake;
    public bool spawnOnStart = true;
    public float spawnDelay = 8f;     // seconds until the next robot after you accept or decline

    Robot current;
    bool queued;

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

    public void QueueNext()
    {
        if (queued) return;
        StartCoroutine(SpawnAfterDelay());
    }

    IEnumerator SpawnAfterDelay()
    {
        queued = true;
        yield return new WaitForSeconds(spawnDelay);
        queued = false;
        SpawnNext();
    }
}