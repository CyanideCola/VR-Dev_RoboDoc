using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnergySystem : MonoBehaviour
{
    public static EnergySystem Instance;
    public Rack rack;
    public ToolStock tools;             // refill stays locked until the starter tools are owned
    public float max = 100f, energy = 100f;
    public float drainPerRobotPerSecond = 0.5f;
    public float declinePenalty = 10f, refillAmount = 50f;
    public float dismantlePenalty = 10f;   // keep >= declinePenalty, so accept then dismantle is never cheaper
    public int refillPrice = 30;

    readonly List<Button> refillButtons = new List<Button>();

    void Awake() { Instance = this; }

    void Start()
    {
        if (!tools) tools = FindAnyObjectByType<ToolStock>();
        // Find the shop buttons wired to BuyRefill, so they can show the locked state
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                if (b.onClick.GetPersistentTarget(i) == this && b.onClick.GetPersistentMethodName(i) == nameof(BuyRefill))
                    refillButtons.Add(b);
    }

    public bool RefillUnlocked { get { return !tools || tools.StartersOwned; } }

    void Update()
    {
        foreach (var b in refillButtons) b.interactable = RefillUnlocked;

        var gp = GameProgress.Instance;
        if (!gp || !gp.running || !rack) return;
        energy -= rack.OccupiedCount * drainPerRobotPerSecond * Time.deltaTime;
        Check();
    }

    public void Spend(float a) { energy -= a; Check(); }
    void Check()
    {
        if (energy > 0f) return;
        energy = 0f;
        GameProgress.Instance.Fail("POWER OUT");
    }

    public void BuyRefill()
    {
        if (!RefillUnlocked) { Notifications.Push("Buy both tools first"); return; }
        if (energy >= max) { Notifications.Push("Energy is full"); return; }
        if (!GameProgress.Instance.TrySpend(refillPrice)) { Notifications.Push("Need " + refillPrice + " gold"); return; }
        energy = Mathf.Min(max, energy + refillAmount);
        Notifications.Push("+" + refillAmount + " energy");
    }
}
