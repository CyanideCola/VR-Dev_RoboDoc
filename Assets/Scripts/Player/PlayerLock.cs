using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

// Stops the player moving while a screen owns the game (the run is over, or they are leaving it).
// Every locomotion provider in the rig is switched off, so neither the thumbsticks nor a teleport
// can carry them away from the screen.
public static class PlayerLock
{
    static readonly List<Behaviour> switchedOff = new List<Behaviour>();

    public static bool Locked { get { return switchedOff.Count > 0; } }

    public static void Set(bool locked)
    {
        if (locked)
        {
            if (switchedOff.Count > 0) return;
            foreach (var p in Object.FindObjectsByType<LocomotionProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!p.enabled) continue;
                p.enabled = false;
                switchedOff.Add(p);
            }
            return;
        }

        foreach (var p in switchedOff) if (p) p.enabled = true;
        switchedOff.Clear();
    }
}
