using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// After every teleport the player keeps looking the way they were looking, whatever the anchor says.
// Put it on the XR Origin and give it the TeleportationProvider.
public class KeepFacingAfterTeleport : MonoBehaviour
{
    public TeleportationProvider provider;
    public XROrigin origin;

    float yawBefore;
    int fixAtFrame = -1;     // the teleport is applied a frame or two after it ends, so the fix waits for it

    void OnEnable()
    {
        if (!provider) return;
        provider.locomotionStarted += OnStarted;
        provider.locomotionEnded += OnEnded;
    }

    void OnDisable()
    {
        if (!provider) return;
        provider.locomotionStarted -= OnStarted;
        provider.locomotionEnded -= OnEnded;
    }

    float Yaw() { return origin.Camera.transform.eulerAngles.y; }

    void OnStarted(LocomotionProvider p) { yawBefore = Yaw(); }
    void OnEnded(LocomotionProvider p) { fixAtFrame = Time.frameCount + 2; }

    void LateUpdate()
    {
        if (fixAtFrame < 0 || Time.frameCount < fixAtFrame) return;
        fixAtFrame = -1;
        float delta = Mathf.DeltaAngle(Yaw(), yawBefore);
        if (Mathf.Abs(delta) > 0.5f) origin.RotateAroundCameraUsingOriginUp(delta);
    }
}
