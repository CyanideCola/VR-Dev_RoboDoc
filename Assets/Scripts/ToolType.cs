public enum ToolType { Hammer, Screwdriver, FeatherDuster, Grinder, SprayCan, Scanner, Part }

public static class ToolTypeNames
{
    // What the player sees on screens and labels
    public static string Label(this ToolType t)
    {
        switch (t)
        {
            case ToolType.Hammer: return "MALLET";
            case ToolType.SprayCan: return "SPRAY CAN";
            case ToolType.FeatherDuster: return "DUSTER";
            case ToolType.Part: return "SPARE PART";
            default: return t.ToString().ToUpperInvariant();
        }
    }
}
