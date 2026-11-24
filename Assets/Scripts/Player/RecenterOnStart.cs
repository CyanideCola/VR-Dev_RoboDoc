using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

// Puts the player on the Home teleport anchor, facing the way it faces, when the game scene starts,
// wherever they happen to be standing in their real room.
public class RecenterOnStart : MonoBehaviour
{
    public XROrigin origin;
    public Transform home;      // the Home teleport anchor
    public float maxWait = 5f;  // seconds to wait for the headset to start tracking

    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(0.5f);   // let tracking settle (real time: the game may start paused)
        // a headset that is not tracking yet reports the camera at the floor; wait until it is at head height
        float waited = 0.5f;
        while (origin.Camera.transform.localPosition.y < 0.3f && waited < maxWait)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        Vector3 p = home.position;
        p.y = origin.Camera.transform.position.y;
        origin.MoveCameraToWorldLocation(p);
        origin.MatchOriginUpCameraForward(home.up, home.forward);
    }
}
