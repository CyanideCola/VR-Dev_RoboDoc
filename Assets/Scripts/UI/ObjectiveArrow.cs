using UnityEngine;

public class ObjectiveArrow : MonoBehaviour
{
    public Transform head;
    public RectTransform panelArrow;   // UI arrow on the panel, pointing up
    public Transform marker;           // small cone pointing down
    public float inViewAngle = 40f;
    Transform target;

    public void SetTarget(Transform t)
    {
        target = t;
        if (!t) Hide();
    }

    // The marker lives outside the panel, so hide it when the panel goes away
    void OnDisable() { Hide(); }

    void Hide()
    {
        if (marker) marker.gameObject.SetActive(false);
        if (panelArrow) panelArrow.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        Hide();
        if (!head || !target || !target.gameObject.activeInHierarchy) return;

        Vector3 flat = target.position - head.position; flat.y = 0f;
        Vector3 fwd = head.forward; fwd.y = 0f;
        float ang = Vector3.SignedAngle(fwd, flat, Vector3.up);

        if (Mathf.Abs(ang) < inViewAngle)
        {
            if (!marker) return;
            marker.gameObject.SetActive(true);
            marker.position = target.position + Vector3.up * (0.25f + 0.05f * Mathf.Sin(Time.time * 4f));
        }
        else if (panelArrow)
        {
            panelArrow.gameObject.SetActive(true);
            panelArrow.localRotation = Quaternion.Euler(0f, 0f, -ang);
        }
    }
}
