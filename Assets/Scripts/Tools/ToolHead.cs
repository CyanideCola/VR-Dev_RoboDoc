using UnityEngine;

public class ToolHead : MonoBehaviour
{
    public enum Mode { Hit, Rub }
    public ToolType type = ToolType.Hammer;
    public Mode mode = Mode.Hit;
    public Transform headPoint;            // the working end of the tool
    public float minSpeed = 1.5f;
    public float hitCooldown = 0.3f;
    public string hitSoundName = "hammer_hit";
    public int hitVariants = 5;

    Vector3 lastPos;
    float speed;
    float lastHit;

    Transform P { get { return headPoint ? headPoint : transform; } }

    void Start() { lastPos = P.position; }

    void FixedUpdate()
    {
        speed = (P.position - lastPos).magnitude / Time.fixedDeltaTime;
        lastPos = P.position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (mode != Mode.Hit || speed < minSpeed || Time.time - lastHit < hitCooldown) return;
        var f = other.GetComponentInParent<Fault>();
        if (f)
        {
            f.Work(type, 1f);
            lastHit = Time.time;
            if (AudioManager.instance && !string.IsNullOrEmpty(hitSoundName))
            {
                string n = hitVariants > 1 ? hitSoundName + "_" + Random.Range(1, hitVariants + 1) : hitSoundName;
                AudioManager.instance.Play(n);
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (mode != Mode.Rub || speed < minSpeed) return;
        var f = other.GetComponentInParent<Fault>();
        if (f) f.Work(type, Time.fixedDeltaTime);
    }
}