using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Fastest Challenge clears, saved as JSON on the headset (Application.persistentDataPath).
// Delete leaderboard.json there to clear the board.
public static class Leaderboard
{
    [Serializable]
    public class Entry
    {
        public string initials;
        public float seconds;
        public string date;
    }

    [Serializable]
    class Data { public List<Entry> entries = new List<Entry>(); }

    const int MaxKept = 100;
    static Data data;

    public static string FilePath { get { return Path.Combine(Application.persistentDataPath, "leaderboard.json"); } }

    static Data D
    {
        get
        {
            if (data != null) return data;
            data = new Data();
            try
            {
                if (File.Exists(FilePath)) data = JsonUtility.FromJson<Data>(File.ReadAllText(FilePath)) ?? new Data();
            }
            catch (Exception e) { Debug.LogWarning("Leaderboard: could not read " + FilePath + ": " + e.Message); }
            return data;
        }
    }

    // Adds a run and returns its rank, 1 being the fastest
    public static int Add(string initials, float seconds)
    {
        var e = new Entry { initials = initials, seconds = seconds, date = DateTime.Now.ToString("yyyy-MM-dd HH:mm") };
        var list = D.entries;
        int i = 0;
        while (i < list.Count && list[i].seconds <= seconds) i++;
        list.Insert(i, e);
        if (list.Count > MaxKept) list.RemoveRange(MaxKept, list.Count - MaxKept);
        try { File.WriteAllText(FilePath, JsonUtility.ToJson(D, true)); }
        catch (Exception ex) { Debug.LogWarning("Leaderboard: could not save " + FilePath + ": " + ex.Message); }
        return i + 1;
    }

    public static IReadOnlyList<Entry> Entries { get { return D.entries; } }

    // m:ss.cc
    public static string Format(float seconds)
    {
        int cs = Mathf.FloorToInt(seconds * 100f);
        return (cs / 6000) + ":" + (cs / 100 % 60).ToString("00") + "." + (cs % 100).ToString("00");
    }

    // One line per rank, for a TextMeshPro text
    public static string Table(int rows, int highlightRank = 0)
    {
        var sb = new System.Text.StringBuilder();
        var list = D.entries;
        if (list.Count == 0) return "No times yet. Be the first!";
        for (int i = 0; i < rows && i < list.Count; i++)
        {
            string line = (i + 1).ToString().PadLeft(2) + ".  " + list[i].initials + "   " + Format(list[i].seconds).PadLeft(8);
            sb.Append(i + 1 == highlightRank ? "<color=#FFC53D>" + line + "</color>" : line).Append('\n');
        }
        return sb.ToString();
    }
}
