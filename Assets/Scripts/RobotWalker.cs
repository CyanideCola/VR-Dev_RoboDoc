using System;
using UnityEngine;

public class RobotWalker : MonoBehaviour
{
    public float speed = 1.2f;
    public float turnSpeed = 360f;
    public float stopDistance = 0.05f;
    public bool arrived { get; private set; }

    Transform target;
    Action onArrive;

    public void WalkTo(Transform t, Action callback = null)
    {
        target = t;
        arrived = false;
        onArrive = callback;
    }

    public void Stop() { target = null; }

    void Update()
    {
        if (target == null || arrived) return;

        Vector3 flatTarget = new Vector3(target.position.x, transform.position.y, target.position.z);
        Vector3 to = flatTarget - transform.position;

        if (to.magnitude <= stopDistance)
        {
            arrived = true;
            transform.rotation = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
            Action cb = onArrive;
            onArrive = null;              // fire once
            cb?.Invoke();
            return;
        }

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, Quaternion.LookRotation(to.normalized), turnSpeed * Time.deltaTime);
        transform.position = Vector3.MoveTowards(transform.position, flatTarget, speed * Time.deltaTime);
    }
}