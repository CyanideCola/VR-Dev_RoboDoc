using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The look shared by the screens that appear in front of the player (run over, pause, leaving).
// Each screen keeps its own layout code, but the dark panel, the rule along the top and the button
// styling come from here, so they all read as one interface.
public static class PanelSkin
{
    public static readonly Color Ink = new Color(0.043f, 0.071f, 0.125f, 0.96f);
    public static readonly Color Raised = new Color(0.106f, 0.153f, 0.251f, 1f);
    public static readonly Color Cyan = new Color(0.098f, 0.827f, 0.961f, 1f);
    public static readonly Color Amber = new Color(1f, 0.773f, 0.239f, 1f);
    public static readonly Color Light = new Color(0.851f, 0.933f, 1f, 1f);
    public static readonly Color Danger = new Color(1f, 0.353f, 0.302f, 1f);

    // A dark panel behind everything else in the holder
    public static Image Background(RectTransform holder, Vector2 size)
    {
        Image bg = null;
        for (int i = 0; i < holder.childCount && !bg; i++)
        {
            var c = holder.GetChild(i);
            if (c.name != "Background" && c.name != "Image") continue;
            bg = c.GetComponent<Image>();
        }
        if (!bg)
        {
            var go = new GameObject("Background", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(holder, false);
            bg = go.GetComponent<Image>();
            bg.raycastTarget = true;
        }
        bg.transform.SetAsFirstSibling();   // behind every label and button
        bg.color = Ink;
        Centre((RectTransform)bg.transform, size);
        return bg;
    }

    // A thin rule along the top edge, so the panel has an edge in a dark room
    public static void Rule(RectTransform holder, Vector2 size, Color color)
    {
        var child = holder.Find("Rule");
        Image bar = child ? child.GetComponent<Image>() : null;
        if (!bar)
        {
            var go = new GameObject("Rule", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(holder, false);
            bar = go.GetComponent<Image>();
            bar.raycastTarget = false;
            bar.transform.SetSiblingIndex(1);   // just in front of the background
        }
        bar.color = color;
        var rt = (RectTransform)bar.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size.x, 6f);
        rt.anchoredPosition = new Vector2(0f, size.y * 0.5f - 3f);
    }

    // Anchors and offsets in one call: aMin/aMax are the anchors, oMin/oMax the offsets
    public static RectTransform Place(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = oMin;
        rt.offsetMax = oMax;
        return rt;
    }

    // Dead centre of the holder at a fixed size
    public static void Centre(RectTransform rt, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
    }

    // A headline in the interface's type style
    public static void Headline(TMP_Text text, string label, float size, Color color)
    {
        if (!text) return;
        text.text = label;
        text.color = color;
        text.enableAutoSizing = false;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
    }

    // A wrapped body line, made the first time it is asked for and reusing the font of the given label
    public static TMP_Text Body(Transform parent, TMP_Text like, string label, float size, Color color)
    {
        var child = parent.Find("Body");
        var text = child ? child.GetComponent<TMP_Text>() : null;
        if (!text)
        {
            var go = new GameObject("Body", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            text = go.AddComponent<TextMeshProUGUI>();
            if (like) text.font = like.font;
        }
        text.text = label;
        text.color = color;
        text.enableAutoSizing = true;
        text.fontSizeMin = size * 0.7f;
        text.fontSizeMax = size;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    // The one button style: dark fill, coloured label, and a brighter fill while it is pointed at
    public static void StyleButton(Button button, Color fill, Color label)
    {
        if (!button) return;
        var img = button.targetGraphic as Image ?? button.GetComponent<Image>();
        if (img) img.color = fill;

        var text = button.GetComponentInChildren<TMP_Text>(true);
        if (text)
        {
            text.color = label;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16f;
            text.fontSizeMax = 36f;
        }

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.35f, 1.35f, 1.35f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.5f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }

    public static TMP_Text Label(Transform parent, string name)
    {
        var t = parent.Find(name);
        return t ? t.GetComponent<TMP_Text>() : null;
    }
}
