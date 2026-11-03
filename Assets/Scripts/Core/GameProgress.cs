using UnityEngine;

public class GameProgress : MonoBehaviour
{
    public static GameProgress Instance { get; private set; }

    public LevelSettings[] levels = new LevelSettings[]
    {
        new LevelSettings { robots = 3, minReported = 1, maxReported = 1, minHidden = 0, maxHidden = 0, workMultiplier = 1.0f },
        new LevelSettings { robots = 3, minReported = 1, maxReported = 1, minHidden = 0, maxHidden = 1, workMultiplier = 1.0f },
        new LevelSettings { robots = 4, minReported = 1, maxReported = 2, minHidden = 0, maxHidden = 1, workMultiplier = 1.2f },
        new LevelSettings { robots = 4, minReported = 2, maxReported = 2, minHidden = 1, maxHidden = 1, workMultiplier = 1.4f },
        new LevelSettings { robots = 5, minReported = 2, maxReported = 3, minHidden = 1, maxHidden = 2, workMultiplier = 1.6f },
    };

    public int level;                 // 0-based
    public int robotsDoneThisLevel;
    public int totalRobotsDone;
    public int money = 100;           // gold. Also set this in the Inspector, since the scene's saved value wins.
    public int parts;                 // from dismantled robots. What they buy is not decided yet.

    public bool running;              // false during the tutorial, set true when it ends
    public float timeLeft;

    [Header("Challenge")]
    public int challengeRobots = 8;           // hand back this many to clear
    public float challengeTimeCap = 900f;     // seconds; the run ends after this so the queue keeps moving
    public int challengeSeed = 2026;          // same robots for everyone; change it to make a new course
    // Which level row each challenge robot uses, by spawn order (past the end, the last value repeats)
    public int[] challengeLevels = { 0, 0, 1, 1, 2, 2, 3, 4 };
    public float elapsed;                     // challenge stopwatch
    public bool cleared;
    public ChallengeResults results;          // shown when the challenge is cleared

    int robotsSpawned;

    void Awake() { Instance = this; }

    void Start() { timeLeft = Current.timeLimit; }

    void Update()
    {
        // The Demo has no clock. The per-level time limits are kept for an endless mode later.
        if (!running || !GameMode.IsChallenge) return;
        elapsed += Time.deltaTime;
        if (elapsed >= challengeTimeCap) Fail("TIME'S UP");
    }

    public LevelSettings Current { get { return levels[Mathf.Min(level, levels.Length - 1)]; } }

    // Levels past the table keep the last entry but add hidden faults and slower work
    public int Overflow { get { return Mathf.Max(0, level - (levels.Length - 1)); } }

    // Called once per robot as it is built. In the Challenge it fixes that robot's level row.
    public int NextRobotIndex() { return robotsSpawned++; }

    public int ChallengeLevel(int robotIndex)
    {
        return challengeLevels[Mathf.Min(robotIndex, challengeLevels.Length - 1)];
    }

    public bool TrySpend(int amount)
    {
        if (money < amount) return false;
        money -= amount;
        return true;
    }

    public void RobotFinished()
    {
        totalRobotsDone++;
        if (GameMode.IsChallenge)
        {
            Notifications.Push(totalRobotsDone + " / " + challengeRobots + " robots");
            if (totalRobotsDone >= challengeRobots) Clear();
            return;
        }

        robotsDoneThisLevel++;
        if (robotsDoneThisLevel >= Current.robots)
        {
            level++;
            robotsDoneThisLevel = 0;
            timeLeft = Current.timeLimit;
            Notifications.Push("LEVEL " + (level + 1));
            Debug.Log("Level " + (level + 1) + " reached");
        }
    }

    void Clear()
    {
        running = false;
        cleared = true;
        Notifications.Push("CHALLENGE CLEAR  " + Leaderboard.Format(elapsed));
        if (results) results.Show(elapsed);
        else Invoke(nameof(ToMenu), 6f);
    }

    public void Fail(string why)
    {
        if (!running) return;
        running = false;
        Notifications.Push(why);
        Invoke(nameof(ToMenu), 4f);
    }

    public void ToMenu()
    {
        if (SceneTransitionManager.singleton) SceneTransitionManager.singleton.GoToSceneAsync(GameMode.MenuScene);
        else UnityEngine.SceneManagement.SceneManager.LoadScene(GameMode.MenuScene);
    }
}
