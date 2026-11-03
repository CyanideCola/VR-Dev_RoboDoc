using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

public class RecenterOnStart : MonoBehaviour
{
    public XROrigin origin;
    public Transform home;      // the Home teleport anchor

    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);   // let tracking settle
        Vector3 p = home.position;
        p.y = origin.Camera.transform.position.y;
        origin.MoveCameraToWorldLocation(p);
        origin.MatchOriginUpCameraForward(home.up, home.forward);
    }
}