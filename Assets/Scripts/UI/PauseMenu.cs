using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// The pause screen, in the store's style: a header, the shift so far, and RESUME / MAIN MENU.
// MAIN MENU first asks to confirm on the same panel. Time stops and the player cannot move while it is up.
// The layout is built by Tools > RoboDoc > Build Pause and Run Over Screens.
public class PauseMenu : MonoBehaviour
{
    public GameObject panel;           // the pause screen
    public GameObject mainGroup;       // the stats and RESUME / MAIN MENU rows
    public GameObject confirmPanel;    // 'Leave the run?' YES / NO, shown in place of mainGroup
    public TMP_Text headerText;
    public TMP_Text moneyText;
    public TMP_Text robotsText, energyText, timeText;
    public InputAction menu = new InputAction("Pause", InputActionType.Button, "<XRController>{LeftHand}/{MenuButton}");
    public Transform head;             // the XR camera
    public GameOverScreen gameOver;
    public float distance = 1.2f;

    bool paused;

    void OnEnable()  { menu.Enable(); menu.performed += OnMenu; }
    void OnDisable() { menu.performed -= OnMenu; menu.Disable(); if (paused) Resume(); }

    void Start()
    {
        if (panel) panel.SetActive(false);
        if (confirmPanel) confirmPanel.SetActive(false);
    }

    void OnMenu(InputAction.CallbackContext c)
    {
        if (paused) Resume();
        else if (CanPause) Pause();
    }

    public void Pause()
    {
        paused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        PlayerLock.Set(true);          // no teleporting away from the screen
        ShowMain();
        Refresh();
        panel.SetActive(true);
        Place();
    }

    public void Resume()
    {
        paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        PlayerLock.Set(false);
        panel.SetActive(false);
        ShowMain();
    }

    public void AskLeave()
    {
        if (mainGroup) mainGroup.SetActive(false);
        if (confirmPanel) confirmPanel.SetActive(true);
        if (headerText) headerText.text = "ROBODOC//LEAVE?";
    }

    public void CancelLeave() { ShowMain(); }

    public void LeaveToMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        PlayerLock.Set(false);
        GameProgress.Instance.ToMenu();
    }

    void ShowMain()
    {
        if (mainGroup) mainGroup.SetActive(true);
        if (confirmPanel) confirmPanel.SetActive(false);
        if (headerText) headerText.text = "ROBODOC//PAUSED";
    }

    // The shift so far
    void Refresh()
    {
        var gp = GameProgress.Instance;
        if (gp && moneyText) moneyText.text = gp.money.ToString("N0") + " G";
        if (gp && robotsText) robotsText.text = GameMode.IsChallenge ? gp.totalRobotsDone + " / " + gp.challengeRobots : gp.totalRobotsDone.ToString();
        var es = EnergySystem.Instance;
        if (energyText) energyText.text = es ? Mathf.CeilToInt(es.energy) + " / " + Mathf.RoundToInt(es.max) : "-";
        if (timeText) timeText.text = gp && GameMode.IsChallenge ? Leaderboard.Format(gp.elapsed) : "NO CLOCK";
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
            return gp && !gp.cleared && (!gameOver || !gameOver.root || !gameOver.root.activeSelf);
        }
    }

    void Place()
    {
        if (!head) return;
        Vector3 f = head.forward; f.y = 0f;
        if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
        f.Normalize();
        panel.transform.position = head.position + f * distance;
        panel.transform.rotation = Quaternion.LookRotation(f);
    }
}
