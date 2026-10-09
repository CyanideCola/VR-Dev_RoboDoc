using UnityEngine;

public class DebugKeys : MonoBehaviour
{
    public IntakeScreen intake;
    public Spawner spawner;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A)) intake.Accept();    // accept patient
        if (Input.GetKeyDown(KeyCode.D)) intake.Decline();   // decline patient
        if (Input.GetKeyDown(KeyCode.S)) spawner.SpawnNext(); // spawn next robot
    }
}