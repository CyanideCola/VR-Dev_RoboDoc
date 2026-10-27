using UnityEngine;

public class ObjectiveArrow : MonoBehaviour
{
    public Transform head;
    public RectTransform panelArrow;   // UI arrow on the panel, pointing up
    public Transform marker;           // small cone pointing down
    public float inViewAngle = 40f;
    Transform target;

    public void SetTarget(Transform t) { target = t; }

    void LateUpdate()
    {
        marker.gameObject.SetActive(false);
        panelArrow.gameObject.SetActive(false);
        if (!target || !target.gameObject.activeInHierarchy) return;

        Vector3 flat = target.position - head.position; flat.y = 0f;
        Vector3 fwd = head.forward; fwd.y = 0f;
        float ang = Vector3.SignedAngle(fwd, flat, Vector3.up);

        if (Mathf.Abs(ang) < inViewAngle)
        {
            marker.gameObject.SetActive(true);
            marker.position = target.position + Vector3.up * (0.25f + 0.05f * Mathf.Sin(Time.time * 4f));
        }
        else
        {
            panelArrow.gameObject.SetActive(true);
            panelArrow.localRotation = Quaternion.Euler(0f, 0f, -ang);
        }
    }
}