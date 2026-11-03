using UnityEngine;

public class DebugKeys : MonoBehaviour
{
    public IntakeScreen intake;
    public Spawner spawner;
    public Rack rack;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A)) intake.Accept();
        if (Input.GetKeyDown(KeyCode.D)) intake.Decline();
        if (Input.GetKeyDown(KeyCode.S)) spawner.SpawnNext();
        if (Input.GetKeyDown(KeyCode.F)) rack.DoneFirst();
    }
}