using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Keeps the store screen's labels in step with what the player owns and can afford.
// The layout itself is built by Tools > RoboDoc > Build Store Screen.
public class ShopScreen : MonoBehaviour
{
    [System.Serializable]
    public class Row
    {
        public Button buy;
        public TMP_Text buyLabel;
        public Button respawn;
    }

    public ToolStock stock;
    public Row[] rows;                 // one per ToolStock entry, same order
    public Button refill;
    public TMP_Text refillLabel;
    public Button takePart;
    public TMP_Text takePartLabel;
    public TMP_Text moneyText;

    void Update()
    {
        var gp = GameProgress.Instance;
        if (moneyText && gp) moneyText.text = gp.money.ToString("N0") + " G";

        if (stock)
            for (int i = 0; i < rows.Length && i < stock.entries.Length; i++)
            {
                var e = stock.entries[i];
                rows[i].buy.interactable = !e.owned;
                rows[i].buyLabel.text = e.owned ? "OWNED" : e.price + "G";
                if (rows[i].respawn) rows[i].respawn.interactable = e.owned;
            }

        var es = EnergySystem.Instance;
        if (refillLabel && es) refillLabel.text = es.RefillUnlocked ? es.refillPrice + "G" : "LOCKED";

        if (takePart && gp)
        {
            takePart.interactable = gp.parts > 0;
            takePartLabel.text = "TAKE (" + gp.parts + ")";
        }
    }
}
