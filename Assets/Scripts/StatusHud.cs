using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusHud : MonoBehaviour
{
    public Image energyFill;      // Image Type: Filled, Horizontal (give it any sprite)
    public TMP_Text energyText;
    public TMP_Text timerText;
    public TMP_Text timerLabel;   // TIME; in the Challenge it shows robots cleared
    public Color okColor = new Color32(0x3D, 0xDC, 0x84, 0xFF);     // green
    public Color lowColor = new Color32(0xFF, 0xC5, 0x3D, 0xFF);    // amber, at or below half
    public Color criticalColor = new Color32(0xFF, 0x5A, 0x4D, 0xFF); // red, at or below a quarter

    void Update()
    {
        var e = EnergySystem.Instance;
        if (e)
        {
            float f = e.energy / e.max;
            if (energyFill)
            {
                energyFill.fillAmount = f;
                energyFill.color = f <= 0.25f ? criticalColor : f <= 0.5f ? lowColor : okColor;
            }
            if (energyText) energyText.text = Mathf.CeilToInt(e.energy).ToString();
        }
        var gp = GameProgress.Instance;
        if (!gp || !timerText) return;
        if (GameMode.IsChallenge)
        {
            // Stopwatch counting up, and how many robots are handed back
            int s = Mathf.FloorToInt(gp.elapsed);
            timerText.text = (s / 60) + ":" + (s % 60).ToString("00");
            if (timerLabel) timerLabel.text = gp.totalRobotsDone + "/" + gp.challengeRobots + " DONE";
        }
        else
        {
            timerText.text = "DEMO";
            if (timerLabel) timerLabel.text = "MODE";
        }
    }
}
