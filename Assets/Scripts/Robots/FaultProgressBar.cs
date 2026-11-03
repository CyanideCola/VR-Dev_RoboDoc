using UnityEngine;
using UnityEngine.UI;

public class FaultProgressBar : MonoBehaviour
{
    public Fault fault;
    public GameObject visual;   // child holding the bar. This script must NOT sit on it.
    public Image fill;

    void LateUpdate()
    {
        bool show = fault && !fault.isFixed && fault.Progress01 > 0f;
        visual.SetActive(show);
        if (!show) return;
        fill.fillAmount = fault.Progress01;
        var cam = Camera.main;
        if (cam) visual.transform.rotation = Quaternion.LookRotation(visual.transform.position - cam.transform.position);
    }
}