using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    public GameObject panel;           // the pause screen
    public GameObject confirmPanel;    // 'Leave the run?' Yes / No
    public InputAction menu = new InputAction("Pause", InputActionType.Button, "<XRController>{LeftHand}/{MenuButton}");
    bool paused;
    public Transform head;            // the XR camera
    public GameOverScreen gameOver;
    public float distance = 1.2f;

    void OnEnable()  { menu.Enable(); menu.performed += OnMenu; }
    void OnDisable() { menu.performed -= OnMenu; menu.Disable(); if (paused) Resume(); }

    void Start() { panel.SetActive(false); if (confirmPanel) confirmPanel.SetActive(false); }

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
