using UnityEngine;

public class PanelFollow : MonoBehaviour
{
    public Transform head;
    public float distance = 1.3f;
    public float height = -0.25f;   // below the eye line
    public float speed = 3f;
    public float side = 0f;

    void LateUpdate()
    {
        Vector3 fwd = head.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) return;
        Vector3 target = head.position + fwd.normalized * distance + Vector3.up * height;
        transform.position = Vector3.Lerp(transform.position, target, speed * Time.deltaTime);
        Quaternion look = Quaternion.LookRotation(transform.position - head.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, speed * Time.deltaTime);
    }
}