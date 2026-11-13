using TMPro;
using UnityEngine;

public class GameOverScreen : MonoBehaviour
{
    public GameObject root;
    public TMP_Text reasonText;

    void Awake() { if (root) root.SetActive(false); }

    public void Show(string reason) { reasonText.text = reason; root.SetActive(true); }
    public void Retry() { SceneTransitionManager.singleton.GoToSceneAsync(GameMode.GameScene); }
    public void Menu()  { GameProgress.Instance.ToMenu(); }
}