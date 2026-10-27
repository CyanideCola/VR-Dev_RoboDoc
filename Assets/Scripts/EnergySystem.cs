using UnityEngine;

public class EnergySystem : MonoBehaviour
{
    public static EnergySystem Instance;
    public Rack rack;
    public float max = 100f, energy = 100f;
    public float drainPerRobotPerSecond = 0.5f;
    public float declinePenalty = 10f, refillAmount = 50f;
    public int refillPrice = 30;

    void Awake() { Instance = this; }

    void Update()
    {
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
        if (energy >= max) { Notifications.Push("Energy is full"); return; }
        if (!GameProgress.Instance.TrySpend(refillPrice)) { Notifications.Push("Need " + refillPrice + " gold"); return; }
        energy = Mathf.Min(max, energy + refillAmount);
        Notifications.Push("+" + refillAmount + " energy");
    }
}