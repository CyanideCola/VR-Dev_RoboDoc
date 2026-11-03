using UnityEngine;

[ExecuteAlways]
public class FaceTarget : MonoBehaviour
{
    public Transform target;

    void Update()
    {
        if (!target) return;
        Vector3 d = target.position - transform.position; d.y = 0f;
        if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(d);
    }
}