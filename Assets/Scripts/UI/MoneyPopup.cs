using TMPro;
using UnityEngine;

public class MoneyPopup : MonoBehaviour
{
    public TMP_Text label;           // the 3D TextMeshPro on this object
    public float rise = 0.6f;
    public float duration = 1.8f;

    float t;
    Vector3 start;
    Color c;

    public void Show(int amount)
    {
        label.text = "+$" + amount.ToString("N0");
        c = label.color;
        start = transform.position;
        t = 0f;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        transform.position = start + Vector3.up * (rise * k);
        c.a = 1f - k * k;
        label.color = c;
        var cam = Camera.main;
        if (cam) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        if (t >= duration) Destroy(gameObject);
    }
}