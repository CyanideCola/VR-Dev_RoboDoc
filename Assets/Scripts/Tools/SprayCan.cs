using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class SprayCan : MonoBehaviour
{
    public Transform nozzle;            // blue arrow points out of the spout
    public float range = 0.4f;          // metres
    public float radius = 0.06f;        // spray width
    public ParticleSystem sprayEffect;  // optional
    public AudioSource sprayAudio;      // optional

    XRGrabInteractable grab;
    bool spraying;
    readonly HashSet<Fault> hit = new HashSet<Fault>();

    void Awake() { grab = GetComponent<XRGrabInteractable>(); }

    void OnEnable()
    {
        grab.activated.AddListener(OnActivated);
        grab.deactivated.AddListener(OnDeactivated);
        grab.selectExited.AddListener(OnReleased);
    }

    void OnDisable()
    {
        grab.activated.RemoveListener(OnActivated);
        grab.deactivated.RemoveListener(OnDeactivated);
        grab.selectExited.RemoveListener(OnReleased);
        Stop();
    }

    void OnActivated(ActivateEventArgs a) { Begin(); }
    void OnDeactivated(DeactivateEventArgs a) { Stop(); }
    void OnReleased(SelectExitEventArgs a) { Stop(); }

    void Begin()
    {
        spraying = true;
        if (sprayEffect) sprayEffect.Play();
        if (sprayAudio) sprayAudio.Play();
    }

    void Stop()
    {
        spraying = false;
        if (sprayEffect) sprayEffect.Stop();
        if (sprayAudio) sprayAudio.Stop();
    }

    void Update()
    {
        if (!spraying || !nozzle) return;
        hit.Clear();
        Vector3 end = nozzle.position + nozzle.forward * range;
        foreach (var c in Physics.OverlapCapsule(nozzle.position, end, radius, ~0, QueryTriggerInteraction.Collide))
        {
            var f = c.GetComponentInParent<Fault>();
            if (f && hit.Add(f)) f.Work(ToolType.SprayCan, Time.deltaTime);   // seconds of spraying
        }
    }
    void OnDrawGizmosSelected()
    {
        if (!nozzle) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(nozzle.position, nozzle.position + nozzle.forward * range);
        Gizmos.DrawWireSphere(nozzle.position + nozzle.forward * range, radius);
    }
}