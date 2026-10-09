using System;

[Serializable]
public class LevelSettings
{
    public int robots = 3;            // robots to finish to clear this level
    public int minReported = 1;       // symptoms shown on the intake screen
    public int maxReported = 1;
    public int minHidden = 0;         // extra faults NOT shown on the screen
    public int maxHidden = 0;
    public float workMultiplier = 1f; // scales hits/seconds needed per fault
}