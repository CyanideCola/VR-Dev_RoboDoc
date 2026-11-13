using UnityEngine;

public class ItemImpactSound : MonoBehaviour
{
    public string soundName = "item_hit_surface";
    public float minImpactSpeed = 0.8f;
    public float cooldown = 0.15f;
    float last;

    void OnCollisionEnter(Collision c)
    {
        if (Time.time - last < cooldown || c.relativeVelocity.magnitude < minImpactSpeed) return;
        last = Time.time;
        if (AudioManager.instance) AudioManager.instance.Play(soundName);
    }
}