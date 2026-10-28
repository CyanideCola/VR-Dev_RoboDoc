using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusHud : MonoBehaviour
{
    public Image energyFill;      // Image Type: Filled, Horizontal (give it any sprite)
    public TMP_Text energyText;
    public TMP_Text timerText;

    void Update()
    {
        var e = EnergySystem.Instance;
        if (e)
        {
            if (energyFill) energyFill.fillAmount = e.energy / e.max;
            if (energyText) energyText.text = Mathf.CeilToInt(e.energy).ToString();
        }
        var gp = GameProgress.Instance;
        if (gp && timerText)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(gp.timeLeft));
            timerText.text = (s / 60) + ":" + (s % 60).ToString("00");
        }
    }
}