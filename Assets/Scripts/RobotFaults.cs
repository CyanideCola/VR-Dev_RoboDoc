using System.Collections.Generic;
using UnityEngine;

public class RobotFaults : MonoBehaviour
{
    public RepairDatabase database;        // optional: drives each spot's symptom, tool and work
    public List<Fault> possibleFaults;     // the fault spots on this robot
    public List<Fault> reported = new List<Fault>();
    public float comboBonus = 0.25f;        // +25% per extra repair completed on the same robot
    public float hiddenPayFactor = 0.5f;    // hidden repairs pay half

    readonly List<Fault> hidden = new List<Fault>();
    readonly List<RepairEntry> used = new List<RepairEntry>();

    void Awake()
    {
        var pool = new List<Fault>();
        foreach (var f in possibleFaults)
            if (f) { f.gameObject.SetActive(false); pool.Add(f); }

        int level = 0, rMin = 1, rMax = 1, hMin = 0, hMax = 0;
        float mult = 1f;
        var gp = GameProgress.Instance;
        if (gp)
        {
            var L = gp.Current;
            int extra = gp.Overflow;
            level = gp.level;
            rMin = L.minReported; rMax = L.maxReported;
            hMin = L.minHidden;   hMax = L.maxHidden + extra;
            mult = L.workMultiplier + 0.1f * extra;
        }

        int reportedCount = Mathf.Min(Random.Range(rMin, rMax + 1), pool.Count);
        int hiddenCount = Mathf.Min(Random.Range(hMin, hMax + 1), pool.Count - reportedCount);

        for (int i = 0; i < reportedCount; i++)
        {
            var f = Pick(pool, mult, level);
            f.reported = true;
            reported.Add(f);
        }
        for (int i = 0; i < hiddenCount; i++) hidden.Add(Pick(pool, mult, level));
    }

    Fault Pick(List<Fault> pool, float mult, int level)
    {
        int i = Random.Range(0, pool.Count);
        var f = pool[i];
        pool.RemoveAt(i);
        if (database)
        {
            var e = database.Roll(f.region, f.requiredTool, level, used);
            if (e != null) { f.Apply(e); used.Add(e); }
        }
        f.gameObject.SetActive(true);
        f.workNeeded *= mult;
        return f;
    }

    public int Evaluate()
    {
        int fixedCount = 0;
        float total = 0f;
        foreach (var f in reported) if (f.isFixed) { total += f.pay; fixedCount++; }
        foreach (var f in hidden) if (f.isFixed) { total += f.pay * hiddenPayFactor; fixedCount++; }
        if (fixedCount > 1) total *= 1f + comboBonus * (fixedCount - 1);
        Debug.Log("Repairs fixed: " + fixedCount + " | payout " + Mathf.RoundToInt(total));
        return Mathf.RoundToInt(total);
    }
}