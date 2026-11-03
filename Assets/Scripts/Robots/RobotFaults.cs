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
    System.Random rng;                      // Challenge only: the same robots for every player

    // Random.Range, or the Challenge's seeded generator
    int Range(int min, int maxExclusive) { return rng != null ? rng.Next(min, maxExclusive) : Random.Range(min, maxExclusive); }

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
            int index = gp.NextRobotIndex();
            var L = gp.Current;
            int extra = gp.Overflow;
            level = gp.level;
            if (GameMode.IsChallenge)
            {
                // Difficulty by spawn order, not by how fast the player is going
                level = gp.ChallengeLevel(index);
                L = gp.levels[Mathf.Min(level, gp.levels.Length - 1)];
                extra = Mathf.Max(0, level - (gp.levels.Length - 1));
                rng = new System.Random(gp.challengeSeed * 1000 + index);
            }
            rMin = L.minReported; rMax = L.maxReported;
            hMin = L.minHidden;   hMax = L.maxHidden + extra;
            // The Demo's scanner lesson needs something to find: every robot after the first carries one hidden fault
            if (GameMode.Current == GameMode.Mode.Demo && index >= 1) { hMin = Mathf.Max(hMin, 1); hMax = Mathf.Max(hMax, 1); }
            mult = L.workMultiplier + 0.1f * extra;
        }

        // A spot stays empty until the database unlocks it at this level (the missing antenna starts at level 2)
        if (database) pool.RemoveAll(f => database.Locked(f.region, f.requiredTool, level));

        int reportedCount = Mathf.Min(Range(rMin, rMax + 1), pool.Count);
        int hiddenCount = Mathf.Min(Range(hMin, hMax + 1), pool.Count - reportedCount);
        if (pool.Count < rMax + hMax) Debug.LogWarning("RobotFaults: only " + pool.Count + " fault spots. Add more to the prefab.");

        for (int i = 0; i < reportedCount; i++)
        {
            var f = Pick(pool, mult, level, false);
            f.reported = true;
            reported.Add(f);
        }
        for (int i = 0; i < hiddenCount; i++) hidden.Add(Pick(pool, mult, level, true));
    }

    public IReadOnlyList<Fault> Hidden { get { return hidden; } }

    // Hidden faults the scanner never found. Reported when the robot is handed back.
    public int HiddenNotFound
    {
        get { int n = 0; foreach (var f in hidden) if (f && !f.revealed) n++; return n; }
    }

    // Scanner: shows every hidden fault on this robot. Returns how many were new.
    public int RevealHidden()
    {
        int n = 0;
        foreach (var f in hidden) if (!f.revealed) { f.Reveal(); n++; }
        return n;
    }

    Fault Pick(List<Fault> pool, float mult, int level, bool isHidden)
    {
        int i = Range(0, pool.Count);
        var f = pool[i];
        pool.RemoveAt(i);
        f.hidden = isHidden;   // set before SetActive, so OnEnable draws it right
        if (database)
        {
            var e = database.Roll(f.region, f.requiredTool, level, used, Range);
            if (e != null) { f.Apply(e); used.Add(e); }
        }
        f.gameObject.SetActive(true);
        f.workNeeded *= mult;
        return f;
    }

    public bool AnyFixed
    {
        get
        {
            foreach (var f in reported) if (f.isFixed) return true;
            foreach (var f in hidden) if (f.isFixed) return true;
            return false;
        }
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