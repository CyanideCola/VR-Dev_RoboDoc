using Unity.XR.CoreUtils;
using UnityEngine;

// Short players can't reach the top of the robots. Their eyes are lifted, floor-relative, up to minEyeHeight
// (a step stool, in effect) so every player reaches the same places. Taller players are never lowered.
// Uses the highest eye height seen, so crouching or leaning doesn't change it.
// Put it on the XR Origin.
public class PlayerHeightAssist : MonoBehaviour
{
    public XROrigin origin;
    public float minEyeHeight = 1.6f;
    public float maxLift = 0.45f;

    float standingEye;
    Vector3 baseOffset;
    bool haveBase;

    void Start()
    {
        if (!origin) origin = GetComponent<XROrigin>();
    }

    void LateUpdate()
    {
        if (!origin || !origin.Camera || !origin.CameraFloorOffsetObject) return;
        var offset = origin.CameraFloorOffsetObject.transform;
        if (!haveBase) { baseOffset = offset.localPosition; haveBase = true; }

        // eye height above the tracked floor; ignored until the headset is actually tracking
        float eye = origin.Camera.transform.localPosition.y;
        if (eye > 0.3f && eye > standingEye) standingEye = eye;

        float lift = standingEye > 0f ? Mathf.Clamp(minEyeHeight - standingEye, 0f, maxLift) : 0f;
        offset.localPosition = baseOffset + Vector3.up * lift;
    }
}
