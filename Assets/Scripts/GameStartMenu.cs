using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameStartMenu : MonoBehaviour
{
    [Header("UI Pages")]
    public GameObject mainMenu;
    public GameObject options;
    public GameObject about;
    public GameObject leaderboard;

    [Header("Main Menu Buttons")]
    public Button startButton;         // DEMO
    public Button challengeButton;
    public Button leaderboardButton;
    public Button optionButton;
    public Button aboutButton;
    public Button quitButton;

    public List<Button> returnButtons;

    // Start is called before the first frame update
    void Start()
    {
        EnableMainMenu();

        //Hook events
        startButton.onClick.AddListener(StartDemo);
        if (challengeButton) challengeButton.onClick.AddListener(StartChallenge);
        if (leaderboardButton) leaderboardButton.onClick.AddListener(EnableLeaderboard);
        optionButton.onClick.AddListener(EnableOption);
        aboutButton.onClick.AddListener(EnableAbout);
        quitButton.onClick.AddListener(QuitGame);

        foreach (var item in returnButtons)
        {
            item.onClick.AddListener(EnableMainMenu);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void StartDemo() { Play(GameMode.Mode.Demo); }
    public void StartChallenge() { Play(GameMode.Mode.Challenge); }

    void Play(GameMode.Mode mode)
    {
        GameMode.Current = mode;
        HideAll();
        SceneTransitionManager.singleton.GoToSceneAsync(GameMode.GameScene);
    }

    public void HideAll()
    {
        mainMenu.SetActive(false);
        options.SetActive(false);
        about.SetActive(false);
        if (leaderboard) leaderboard.SetActive(false);
    }

    public void EnableMainMenu()
    {
        HideAll();
        mainMenu.SetActive(true);
    }
    public void EnableOption()
    {
        HideAll();
        options.SetActive(true);
    }
    public void EnableAbout()
    {
        HideAll();
        about.SetActive(true);
    }
    public void EnableLeaderboard()
    {
        HideAll();
        leaderboard.SetActive(true);
    }
}
