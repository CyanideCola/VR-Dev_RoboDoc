using TMPro;
using UnityEngine;

// The run-over screen, in the store's style: a header, why the run ended, the shift's numbers,
// and TRY AGAIN / MAIN MENU. It stays in front of the player, and the rig cannot move while it is up.
// The layout is built by Tools > RoboDoc > Build Pause and Run Over Screens.
public class GameOverScreen : MonoBehaviour
{
    public GameObject root;
    public TMP_Text reasonText;
    public TMP_Text moneyText;
    public TMP_Text robotsText, timeText;
    public Transform head;
    public float distance = 1.05f;
    public float followSpeed = 6f;

    void Awake()
    {
        if (root) root.SetActive(false);
    }

    public void Show(string reason)
    {
        if (!root) return;
        if (reasonText) reasonText.text = reason;
        var gp = GameProgress.Instance;
        if (gp && moneyText) moneyText.text = gp.money.ToString("N0") + " G";
        if (gp && robotsText) robotsText.text = GameMode.IsChallenge ? gp.totalRobotsDone + " / " + gp.challengeRobots : gp.totalRobotsDone.ToString();
        if (timeText) timeText.text = gp && GameMode.IsChallenge ? Leaderboard.Format(gp.elapsed) : "NO CLOCK";
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
        var look = Quaternion.LookRotation(f);
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
}
