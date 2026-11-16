using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The run-over screen: a big panel on its own dark backing that stays in front of the player,
// with the reason they are done and the two ways out. The rig cannot move while it is up.
public class GameOverScreen : MonoBehaviour
{
    public GameObject root;
    public TMP_Text reasonText;
    public Transform head;
    public float distance = 1.05f;
    public Vector2 panelSize = new Vector2(900f, 520f);
    public float followSpeed = 6f;

    void Awake()
    {
        if (!root) return;
        Layout();
        root.SetActive(false);
    }

    public void Show(string reason)
    {
        if (!root) return;
        Layout();
        if (reasonText) reasonText.text = reason;
        root.SetActive(true);
        PlayerLock.Set(true);   // the run is over: nothing moves under the screen
        Place();
    }

    // Stays in front of the player: they can look around, and the panel comes with them
    void LateUpdate()
    {
        if (root && root.activeSelf) Place(true);
    }

    public void Retry()
    {
        PlayerLock.Set(false);
        if (SceneTransitionManager.singleton) SceneTransitionManager.singleton.GoToSceneAsync(GameMode.GameScene);
        else UnityEngine.SceneManagement.SceneManager.LoadScene(GameMode.GameScene);
    }

    public void Menu()
    {
        PlayerLock.Set(false);
        if (GameProgress.Instance) GameProgress.Instance.ToMenu();
    }

    void Place(bool smooth = false)
    {
        if (!head || !root) return;
        Vector3 f = head.forward; f.y = 0f;
        if (f.sqrMagnitude < 0.01f) return;
        f.Normalize();

        Vector3 want = head.position + f * distance;
        var look = Quaternion.LookRotation(root.transform.position - head.position);
        if (!smooth)
        {
            root.transform.position = want;
            root.transform.rotation = look;
            return;
        }
        float k = followSpeed * Time.unscaledDeltaTime;
        root.transform.position = Vector3.Lerp(root.transform.position, want, k);
        root.transform.rotation = Quaternion.Slerp(root.transform.rotation, look, k);
    }

    // Bigger than it was, on a dark panel, with the two buttons in a row along the bottom
    void Layout()
    {
        var panel = (RectTransform)root.transform;
        panel.sizeDelta = panelSize;
        PanelSkin.Background(panel, panelSize);
        PanelSkin.Rule(panel, panelSize, PanelSkin.Danger);

        var title = PanelSkin.Label(panel, "GameOver");
        if (title)
        {
            PanelSkin.Headline(title, "RUN OVER", 76f, PanelSkin.Danger);
            PanelSkin.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -132f), new Vector2(-28f, -28f));
        }

        var reason = reasonText ? reasonText : PanelSkin.Label(panel, "ReasonText");
        if (reason)
        {
            reason.color = PanelSkin.Light;
            reason.enableAutoSizing = true;
            reason.fontSizeMin = 26f;
            reason.fontSizeMax = 44f;
            reason.alignment = TextAlignmentOptions.Center;
            reason.textWrappingMode = TextWrappingModes.Normal;
            reason.overflowMode = TextOverflowModes.Overflow;
            PanelSkin.Place(reason.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(48f, 160f), new Vector2(-48f, -140f));
        }

        RowButton(panel, "Retry", "TRY AGAIN", PanelSkin.Cyan, new Vector2(-330f, 44f), new Vector2(-22f, 140f));
        RowButton(panel, "Menu", "MAIN MENU", PanelSkin.Amber, new Vector2(22f, 44f), new Vector2(330f, 140f));
    }

    static void RowButton(RectTransform panel, string name, string label, Color color, Vector2 oMin, Vector2 oMax)
    {
        var t = panel.Find(name);
        if (!t) return;
        PanelSkin.Place((RectTransform)t, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), oMin, oMax);
        var text = t.GetComponentInChildren<TMP_Text>(true);
        if (text) text.text = label;
        var button = t.GetComponent<Button>();
        if (button) PanelSkin.StyleButton(button, PanelSkin.Raised, color);
    }
}
