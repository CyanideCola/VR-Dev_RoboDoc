using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    public GameObject panel;           // the pause screen
    public GameObject confirmPanel;    // 'Leave the run?' Yes / No
    public InputAction menu = new InputAction("Pause", InputActionType.Button, "<XRController>{LeftHand}/{MenuButton}");

    bool paused;

    void OnEnable()  { menu.Enable(); menu.performed += OnMenu; }
    void OnDisable() { menu.performed -= OnMenu; menu.Disable(); if (paused) Resume(); }

    void Start() { panel.SetActive(false); if (confirmPanel) confirmPanel.SetActive(false); }

    void OnMenu(InputAction.CallbackContext c)
    {
        var gp = GameProgress.Instance;
        if (paused) Resume();
        else if (gp && gp.running) Pause();
    }

    public void Pause()
    {
        paused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        panel.SetActive(true);
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
        var gp = GameProgress.Instance;
        if (!focus && gp && gp.running && !paused) Pause();
    }
}