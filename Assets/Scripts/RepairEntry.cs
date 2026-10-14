using System;

[Serializable]
public class RepairEntry
{
    public string symptom = "New repair";
    public RepairRegion region = RepairRegion.Head;
    public ToolType tool = ToolType.Hammer;
    public float workNeeded = 3f;   // hits for a Hit tool, seconds for a Rub tool
    public int minLevel = 0;        // first level (0-based) where it can appear
}