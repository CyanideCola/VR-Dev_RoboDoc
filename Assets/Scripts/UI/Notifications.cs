using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Notifications : MonoBehaviour
{
    public static Notifications Instance;
    public TMP_Text label;           // alignment: bottom left
    public int maxLines = 4;
    public float life = 3.5f;

    struct Item { public string text; public float until; }
    readonly List<Item> items = new List<Item>();

    void Awake() { Instance = this; }
    public static void Push(string s) { if (Instance) Instance.Add(s); }

    void Add(string s)
    {
        items.Add(new Item { text = s, until = Time.time + life });
        if (items.Count > maxLines) items.RemoveAt(0);
        Redraw();
    }

    void Update() { if (items.RemoveAll(i => i.until < Time.time) > 0) Redraw(); }
    void Redraw() { label.text = string.Join("\n", items.ConvertAll(i => i.text)); }
}