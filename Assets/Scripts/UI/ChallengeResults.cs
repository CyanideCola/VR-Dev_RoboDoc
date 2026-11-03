using TMPro;
using UnityEngine;

// The Challenge clear screen: arcade initials, save to the leaderboard, back to the menu.
// The layout is built by Tools > RoboDoc > Build Challenge and Menu.
public class ChallengeResults : MonoBehaviour
{
    public GameObject root;            // the panel, hidden until the challenge is cleared
    public TMP_Text timeText;
    public TMP_Text[] letters;         // three initials
    public GameObject entryGroup;      // initials and SAVE, hidden after saving
    public TMP_Text boardText;         // top times, shown after saving
    public GameObject menuButton;      // shown after saving
    public int boardRows = 5;

    const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    readonly int[] pick = new int[3];
    float seconds;
    bool saved;

    void Awake() { if (root) root.SetActive(false); }

    public void Show(float time)
    {
        seconds = time;
        saved = false;
        for (int i = 0; i < 3; i++) pick[i] = 0;   // AAA: each run at the fair is usually a new person
        root.SetActive(true);
        entryGroup.SetActive(true);
        menuButton.SetActive(false);
        timeText.text = Leaderboard.Format(seconds);
        boardText.text = "";
        Redraw();
    }

    // Wired to the up and down buttons under each letter
    public void Up(int slot) { Step(slot, 1); }
    public void Down(int slot) { Step(slot, -1); }

    void Step(int slot, int d)
    {
        if (saved) return;
        pick[slot] = (pick[slot] + d + Alphabet.Length) % Alphabet.Length;
        Redraw();
    }

    void Redraw() { for (int i = 0; i < 3; i++) letters[i].text = Alphabet[pick[i]].ToString(); }

    public void Save()
    {
        if (saved) return;
        saved = true;
        string initials = "" + Alphabet[pick[0]] + Alphabet[pick[1]] + Alphabet[pick[2]];
        int rank = Leaderboard.Add(initials, seconds);
        entryGroup.SetActive(false);
        boardText.text = "RANK #" + rank + "\n\n" + Leaderboard.Table(boardRows, rank);
        menuButton.SetActive(true);
    }

    public void BackToMenu()
    {
        if (GameProgress.Instance) GameProgress.Instance.ToMenu();
    }
}
