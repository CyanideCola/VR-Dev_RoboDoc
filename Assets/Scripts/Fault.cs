using UnityEngine;

public class Fault : MonoBehaviour
{
    public string symptom = "Dented head";
    public ToolType requiredTool = ToolType.Hammer;
    public float workNeeded = 3f;          // hits (Hit mode) or seconds (Rub mode)
    public GameObject brokenVisual;
    public GameObject fixedVisual;
    [HideInInspector] public bool reported;

    public bool isFixed { get; private set; }
    float progress;

    void OnEnable() { Refresh(); }

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