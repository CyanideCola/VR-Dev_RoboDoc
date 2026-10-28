using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RepairDatabase", menuName = "RoboDoc/Repair Database")]
public class RepairDatabase : ScriptableObject
{
    // Starter entries (placeholders: edit the text and numbers in the Inspector)
    public List<RepairEntry> repairs = new List<RepairEntry>
    {
        new RepairEntry { symptom = "Dented head",        region = RepairRegion.Head,  tool = ToolType.Hammer,        workNeeded = 3f, minLevel = 0 },
        new RepairEntry { symptom = "Hearing is muffled", region = RepairRegion.Head,  tool = ToolType.FeatherDuster, workNeeded = 2f, minLevel = 0 },
        new RepairEntry { symptom = "Dusty eye sensors",  region = RepairRegion.Head,  tool = ToolType.FeatherDuster, workNeeded = 2f, minLevel = 1 },
        new RepairEntry { symptom = "Dented arm",         region = RepairRegion.Arm,   tool = ToolType.Hammer,        workNeeded = 3f, minLevel = 0 },
        new RepairEntry { symptom = "Dusty arm joint",    region = RepairRegion.Arm,   tool = ToolType.FeatherDuster, workNeeded = 2f, minLevel = 1 },
        new RepairEntry { symptom = "Dented torso",       region = RepairRegion.Torso, tool = ToolType.Hammer,        workNeeded = 4f, minLevel = 1 },
        new RepairEntry { symptom = "Clogged torso vents",region = RepairRegion.Torso, tool = ToolType.FeatherDuster, workNeeded = 3f, minLevel = 1 },
    };

    // Picks a random entry for this region and level. Avoids repeats on the same robot when possible.
    public RepairEntry Roll(RepairRegion region, ToolType tool, int level, List<RepairEntry> alreadyUsed, System.Func<int, int, int> range = null)
    {
        var options = new List<RepairEntry>();
        foreach (var r in repairs)
            if (r.region == region && r.tool == tool && r.minLevel <= level && !alreadyUsed.Contains(r)) options.Add(r);

        if (options.Count == 0)   // everything already used: allow a repeat
            foreach (var r in repairs)
                if (r.region == region && r.tool == tool && r.minLevel <= level) options.Add(r);

        if (options.Count == 0) return null;
        return options[range != null ? range(0, options.Count) : Random.Range(0, options.Count)];
    }
}