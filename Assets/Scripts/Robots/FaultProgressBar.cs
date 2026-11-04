using UnityEngine;
using UnityEngine.UI;

public class FaultProgressBar : MonoBehaviour
{
    public Fault fault;
    public GameObject visual;   // child holding the bar. This script must NOT sit on it.
    public Image fill;
    public float forwardOffset = 0.14f;   // metres toward the player, so no part of the robot sits in front of the bar
    public float lift = 0.05f;            // and a little above the fault

    void LateUpdate()
    {
        bool show = fault && !fault.isFixed && fault.Progress01 > 0f;
        visual.SetActive(show);
        if (!show) return;
        fill.fillAmount = fault.Progress01;
        var cam = Camera.main;
        if (!cam) return;
        Vector3 from = fault.transform.position;
        Vector3 toPlayer = (cam.transform.position - from).normalized;
        visual.transform.position = from + toPlayer * forwardOffset + Vector3.up * lift;
        visual.transform.rotation = Quaternion.LookRotation(visual.transform.position - cam.transform.position);
    }
}
