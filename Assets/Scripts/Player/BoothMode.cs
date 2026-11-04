using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

// Booth build only (mixed-reality branch): the real room shows through passthrough and the game is laid out
// around one fixed spot, so the player walks between stations instead of teleporting.
// Teleporting would slide the game away from the real room, so the sticks never teleport here,
// and the height assist is off so the game floor stays on the real floor.
// Built by Tools > RoboDoc > Apply Booth (Mixed Reality).
public class BoothMode : MonoBehaviour
{
    public static bool Active { get; private set; }

    void Awake() { Active = true; }
    void OnDestroy() { Active = false; }

    // Start runs after ControlOptions has applied the player's teleport choice on scene load, so this wins
    void Start()
    {
        foreach (var c in FindObjectsByType<ControllerInputActionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            c.smoothMotionEnabled = true;    // the stick's teleport mode is off; the move it switches to has no provider
        var assist = FindAnyObjectByType<PlayerHeightAssist>(FindObjectsInactive.Include);
        if (assist) assist.enabled = false;
    }

    // Teleport wording for the normal game, walking wording for the booth
    public static string Say(string teleport, string walk) { return Active ? walk : teleport; }
}
