using System.Collections.Generic;
using UnityEngine;

public class RobotFaults : MonoBehaviour
{
    public List<Fault> possibleFaults;
    public List<Fault> reported = new List<Fault>();
    readonly List<Fault> hidden = new List<Fault>();

    void Awake()
    {
        foreach (var f in possibleFaults) f.gameObject.SetActive(false);
        var pool = new List<Fault>(possibleFaults);

        int rMin = 1, rMax = 1, hMin = 0, hMax = 0;
        float mult = 1f;
        if (GameProgress.Instance)
        {
            var L = GameProgress.Instance.Current;
            int extra = GameProgress.Instance.Overflow;
            rMin = L.minReported; rMax = L.maxReported;
            hMin = L.minHidden;   hMax = L.maxHidden + extra;
            mult = L.workMultiplier + 0.1f * extra;
        }

        // Counts are capped by how many fault spots the prefab actually has
        int reportedCount = Mathf.Min(Random.Range(rMin, rMax + 1), pool.Count);
        int hiddenCount = Mathf.Min(Random.Range(hMin, hMax + 1), pool.Count - reportedCount);

        for (int i = 0; i < reportedCount; i++)
        {
            var f = Pick(pool, mult);
            f.reported = true;
            reported.Add(f);
        }
        for (int i = 0; i < hiddenCount; i++) hidden.Add(Pick(pool, mult));
    }

    Fault Pick(List<Fault> pool, float mult)
    {
        int i = Random.Range(0, pool.Count);
        var f = pool[i];
        pool.RemoveAt(i);
        f.gameObject.SetActive(true);
        f.workNeeded *= mult;
        return f;
    }

    // Called at handoff. Keep the result hidden from the player until the owner reacts.
    public int payPerReported = 100;
    public int payPerHidden = 50;

    public int Evaluate()
    {
        int rFixed = 0, hFixed = 0;
        foreach (var f in reported) if (f.isFixed) rFixed++;
        foreach (var f in hidden) if (f.isFixed) hFixed++;
        Debug.Log("Reported fixed " + rFixed + "/" + reported.Count +
                  " | hidden fixed " + hFixed + "/" + hidden.Count);
        return rFixed * payPerReported + hFixed * payPerHidden;
    }
}