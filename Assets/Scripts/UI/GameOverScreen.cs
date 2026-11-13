using TMPro;
using UnityEngine;

public class GameOverScreen : MonoBehaviour
{
    public GameObject root;
    public TMP_Text reasonText;
    public Transform head;
    public float distance = 1.2f;

    public void Show(string reason)
    {
        reasonText.text = reason;
        root.SetActive(true);
        if (head)
        {
            Vector3 f = head.forward; f.y = 0f; f.Normalize();
            root.transform.position = head.position + f * distance;
            root.transform.rotation = Quaternion.LookRotation(f);
        }
    }

    void Awake() { if (root) root.SetActive(false); }

    public void Retry() { SceneTransitionManager.singleton.GoToSceneAsync(GameMode.GameScene); }
    public void Menu()  { GameProgress.Instance.ToMenu(); }
}