using TMPro;
using UnityEngine;

// Fills a text with the top times whenever the page is shown
public class LeaderboardView : MonoBehaviour
{
    public TMP_Text list;
    public int rows = 10;

    void OnEnable() { if (list) list.text = Leaderboard.Table(rows); }
}
