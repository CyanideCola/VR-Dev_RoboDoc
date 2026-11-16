using UnityEngine;

// While the Demo lesson is running, the store only sells what the current objective asks for.
// Buying the wrong thing would spend the gold the lesson needs and leave the player stuck with
// an objective they cannot afford to finish.
public static class DemoRules
{
    public static bool Complete;         // the lesson is over: the store is open again
    public static bool StoreOpen;        // part two of the lesson: the shop's extras (refills) unlock
    public static ToolType? Wanted;      // the tool the objective on screen is asking for

    public static bool Active { get { return !Complete && GameMode.Current == GameMode.Mode.Demo; } }

    public static void Reset() { Complete = false; StoreOpen = false; Wanted = null; }

    // The next objective asks for this tool. Null means "the lesson is not shopping right now".
    public static void Ask(ToolType? tool) { Wanted = tool; }

    public static bool CanBuy(ToolType tool)
    {
        return !Active || (Wanted.HasValue && Wanted.Value == tool);
    }

    // Says no to the player, so a locked button that still gets pressed explains itself
    public static bool Refuse()
    {
        if (!Active) return false;
        Notifications.Push("<color=#FFC53D>Finish the objective first</color>");
        return true;
    }
}
