using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public GameObject panel;           // the pause screen
    public GameObject confirmPanel;    // 'Leave the run?' Yes / No
    public InputAction menu = new InputAction("Pause", InputActionType.Button, "<XRController>{LeftHand}/{MenuButton}");
    bool paused;
    public Transform head;            // the XR camera
    public GameOverScreen gameOver;
    public float distance = 1.2f;
    public Vector2 confirmSize = new Vector2(760f, 400f);

    void OnEnable()  { menu.Enable(); menu.performed += OnMenu; }
    void OnDisable() { menu.performed -= OnMenu; menu.Disable(); if (paused) Resume(); }

    void Start()
    {
        if (panel) panel.SetActive(false);
        if (confirmPanel) { LayoutConfirm(); confirmPanel.SetActive(false); }
    }

    // The leave question: its own dark panel, a headline and the two answers side by side
    void LayoutConfirm()
    {
        var rt = (RectTransform)confirmPanel.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = confirmSize;
        PanelSkin.Background(rt, confirmSize);
        PanelSkin.Rule(rt, confirmSize, PanelSkin.Amber);

        var title = PanelSkin.Label(rt, "Confirm");
        if (title)
        {
            PanelSkin.Headline(title, "LEAVE THE RUN?", 62f, PanelSkin.Amber);
            PanelSkin.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -124f), new Vector2(-24f, -24f));
        }

        var body = PanelSkin.Body(rt, title, "Your shift ends here. Nothing you earned is kept.", 34f, PanelSkin.Light);
        PanelSkin.Place(body.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(48f, 140f), new Vector2(-48f, -132f));

        Answer(rt, "Yes", "YES, LEAVE", PanelSkin.Danger, new Vector2(-330f, 48f), new Vector2(-24f, 140f));
        Answer(rt, "No", "KEEP WORKING", PanelSkin.Cyan, new Vector2(24f, 48f), new Vector2(330f, 140f));
    }

    static void Answer(RectTransform panel, string name, string label, Color color, Vector2 oMin, Vector2 oMax)
    {
        var t = panel.Find(name);
        if (!t) return;
        PanelSkin.Place((RectTransform)t, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), oMin, oMax);
        var text = t.GetComponentInChildren<TMP_Text>(true);
        if (text) text.text = label;
        var button = t.GetComponent<Button>();
        if (button) PanelSkin.StyleButton(button, PanelSkin.Raised, color);
    }

    void OnMenu(InputAction.CallbackContext c)
    {
        var gp = GameProgress.Instance;
        if (paused) Resume();
        else if (CanPause) Pause();
    }

    public void Pause()
    {
        paused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        panel.SetActive(true);
        PanelSkin.Background((RectTransform)panel.transform, new Vector2(900f, 520f));   // a backing so the list reads
        Place();
    }

    public void Resume()
    {
        paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        panel.SetActive(false);
        if (confirmPanel) confirmPanel.SetActive(false);
    }

    public void AskLeave() { confirmPanel.SetActive(true); }
    public void CancelLeave() { confirmPanel.SetActive(false); }

    public void LeaveToMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        GameProgress.Instance.ToMenu();
    }

    void OnApplicationFocus(bool focus)
    {
        if (!focus && CanPause && !paused) Pause();
    }
    bool CanPause
    {
        get
        {
            var gp = GameProgress.Instance;
            return gp && !gp.cleared && (!gameOver || !gameOver.root.activeSelf);
        }
    }

    void Place()
    {
        Vector3 f = head.forward; f.y = 0f; f.Normalize();
        panel.transform.position = head.position + f * distance;
        panel.transform.rotation = Quaternion.LookRotation(f);
    }
}
