using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public FadeScreen fadeScreen;
    public static SceneTransitionManager singleton;

    private bool changing;   // a second press (or a double click) while a change is under way is ignored

    private void Awake()
    {
        if (singleton && singleton != this)
            Destroy(singleton);

        singleton = this;
    }

    public void GoToScene(int sceneIndex)
    {
        if (changing) return;
        changing = true;
        StartCoroutine(GoToSceneRoutine(sceneIndex));
    }

    IEnumerator GoToSceneRoutine(int sceneIndex)
    {
        fadeScreen.FadeOut();
        yield return new WaitForSeconds(fadeScreen.fadeDuration);

        //Launch the new scene
        SceneManager.LoadScene(sceneIndex);
    }

    public void GoToSceneAsync(int sceneIndex)
    {
        if (changing) return;
        changing = true;
        StartCoroutine(GoToSceneAsyncRoutine(sceneIndex));
    }

    IEnumerator GoToSceneAsyncRoutine(int sceneIndex)
    {
        fadeScreen.FadeOut();
        //Launch the new scene
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
        operation.allowSceneActivation = false;

        // Stay black until the fade has finished, and the scene has loaded (it stops at 0.9 until activated)
        float timer = 0;
        while(timer <= fadeScreen.fadeDuration || operation.progress < 0.9f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        operation.allowSceneActivation = true;
    }
}
