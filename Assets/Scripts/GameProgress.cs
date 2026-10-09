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
    public int money;

    void Awake() { Instance = this; }

    public LevelSettings Current { get { return levels[Mathf.Min(level, levels.Length - 1)]; } }

    // Levels past the table keep the last entry but add hidden faults and slower work
    public int Overflow { get { return Mathf.Max(0, level - (levels.Length - 1)); } }

    public void RobotFinished()
    {
        totalRobotsDone++;
        robotsDoneThisLevel++;
        if (robotsDoneThisLevel >= Current.robots)
        {
            level++;
            robotsDoneThisLevel = 0;
            Debug.Log("Level " + (level + 1) + " reached");
        }
    }
}