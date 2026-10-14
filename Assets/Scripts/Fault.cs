using UnityEngine;

public class Fault : MonoBehaviour
{
    public RepairRegion region = RepairRegion.Head;
    // These three are the fallback. When a RepairDatabase is assigned they are overwritten at spawn.
    public string symptom = "Dented head";
    public ToolType requiredTool = ToolType.Hammer;
    public float workNeeded = 3f;
    public GameObject brokenVisual;
    public GameObject fixedVisual;
    [HideInInspector] public bool reported;

    public bool isFixed { get; private set; }
    float progress;

    void OnEnable() { Refresh(); }

    public void Apply(RepairEntry e)
    {
        symptom = e.symptom;
        requiredTool = e.tool;
        workNeeded = e.workNeeded;
    }

    public void Work(ToolType tool, float amount)
    {
        if (isFixed || tool != requiredTool) return;   // wrong tool: nothing happens
        progress += amount;
        if (progress >= workNeeded) { isFixed = true; Refresh(); }
    }

    void Refresh()
    {
        if (brokenVisual) brokenVisual.SetActive(!isFixed);
        if (fixedVisual) fixedVisual.SetActive(isFixed);
    }
}