using UnityEngine;

public class PanelFollow : MonoBehaviour
{
    public Transform head;
    public float distance = 1.3f;
    public float height = -0.25f;   // negative = below the eye line
    public float side = 0f;         // negative = left, positive = right
    public float speed = 3f;

    void LateUpdate()
    {
        Vector3 fwd = head.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) return;
        fwd.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        Vector3 target = head.position + fwd * distance + Vector3.up * height + right * side;
        transform.position = Vector3.Lerp(transform.position, target, speed * Time.deltaTime);
        Quaternion look = Quaternion.LookRotation(transform.position - head.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, speed * Time.deltaTime);
    }
}