using UnityEngine;

// Rolls the credits up through a masked window, then starts over, so any length fits above the BACK button.
// content is the credits text (top-anchored, sized to its text); its parent is the window.
public class CreditsScroll : MonoBehaviour
{
    public RectTransform content;
    public float speed = 70f;          // canvas units per second
    public float pause = 2f;           // held at the start and at the end

    float y, wait;
    bool atEnd;

    void OnEnable() { y = 0f; wait = pause; atEnd = false; Place(); }

    void Update()
    {
        var window = content ? content.parent as RectTransform : null;
        if (!window) return;
        float overflow = content.rect.height - window.rect.height;
        if (overflow <= 0f) { y = 0f; Place(); return; }      // short enough to stand still

        if (wait > 0f)
        {
            wait -= Time.unscaledDeltaTime;
            if (wait <= 0f && atEnd) { y = 0f; atEnd = false; wait = pause; Place(); }
            return;
        }
        y += speed * Time.unscaledDeltaTime;
        if (y >= overflow) { y = overflow; atEnd = true; wait = pause; }
        Place();
    }

    void Place() { content.anchoredPosition = new Vector2(0f, y); }
}
