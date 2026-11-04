using UnityEngine;

public class PanelFollow : MonoBehaviour
{
    public Transform head;
    public float distance = 1.3f;
    public float height = -0.25f;   // negative = below the eye line
    public float side = 0f;         // negative = left, positive = right
    public float speed = 3f;
    public float deadZone = 0.12f;        // metres the head can move before the panel follows
    public float deadAngle = 15f;         // degrees the head can turn before the panel follows
    public bool hideFromScanner = true;   // moved to the UI layer, which the scanner camera does not draw. Turn off for panels with buttons.

    void Awake()
    {
        if (hideFromScanner) SetLayer(transform, LayerMask.NameToLayer("UI"));
    }

    static void SetLayer(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        foreach (Transform c in t) SetLayer(c, layer);
    }

    Vector3 anchorPos;
    float anchorYaw;
    bool anchored;

    // The panel holds still while the head only sways a little, so it doesn't swim in view
    void LateUpdate()
    {
        Vector3 fwd = head.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) return;
        fwd.Normalize();
        float yaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
        if (!anchored || (head.position - anchorPos).sqrMagnitude > deadZone * deadZone || Mathf.Abs(Mathf.DeltaAngle(yaw, anchorYaw)) > deadAngle)
        {
            anchorPos = head.position; anchorYaw = yaw; anchored = true;
        }
        fwd = Quaternion.Euler(0f, anchorYaw, 0f) * Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        Vector3 target = anchorPos + fwd * distance + Vector3.up * height + right * side;
        transform.position = Vector3.Lerp(transform.position, target, speed * Time.deltaTime);
        Quaternion look = Quaternion.LookRotation(transform.position - anchorPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, speed * Time.deltaTime);
    }
}