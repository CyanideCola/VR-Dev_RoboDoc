using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeScreen : MonoBehaviour
{
    public bool fadeOnStart = true;
    public float fadeDuration = 1;
    public Color fadeColor;
    public AnimationCurve fadeCurve;
    public string colorPropertyName = "_Color";
    private Renderer rend;
    private Coroutine running;
    private float alpha;           // where the fade is right now, so a new fade can start from it

    // Start is called before the first frame update
    void Start()
    {
        rend = GetComponent<Renderer>();
        rend.enabled = false;

        if (fadeOnStart)
            FadeIn();
    }

    // From black (as the scene appears) to clear
    public void FadeIn()
    {
        Fade(1, 0);
    }

    // From wherever it is now to black
    public void FadeOut()
    {
        Fade(alpha, 1);
    }

    // The screen goes black at once, hiding a teleport (comfort option); BlinkIn brings it back
    public void BlinkOut()
    {
        if (!rend) rend = GetComponent<Renderer>();
        if (running != null) { StopCoroutine(running); running = null; }
        rend.enabled = true;
        SetAlpha(1f);
    }

    public void BlinkIn(float seconds) { Fade(1, 0, seconds); }

    public void Fade(float alphaIn, float alphaOut, float seconds = -1f)
    {
        // Only one fade at a time: a fade-in still running when the player leaves must not cancel the fade-out
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(FadeRoutine(alphaIn, alphaOut, seconds));
    }

    public IEnumerator FadeRoutine(float alphaIn, float alphaOut, float seconds = -1f)
    {
        if (!rend) rend = GetComponent<Renderer>();
        if (fadeCurve == null || fadeCurve.length == 0) fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        rend.enabled = true;

        // A fade that starts partway takes proportionally less time
        float duration = (seconds >= 0f ? seconds : fadeDuration) * Mathf.Abs(alphaOut - alphaIn);
        float timer = 0;
        while(timer < duration)
        {
            SetAlpha(Mathf.Lerp(alphaIn, alphaOut, fadeCurve.Evaluate(timer / duration)));
            timer += Time.deltaTime;
            yield return null;
        }

        SetAlpha(alphaOut);

        if(alphaOut == 0)
            rend.enabled = false;
        running = null;
    }

    void SetAlpha(float a)
    {
        alpha = a;
        Color newColor = fadeColor;
        newColor.a = a;
        rend.material.SetColor(colorPropertyName, newColor);
    }
}
