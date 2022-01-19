using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraTrackBounds : MonoBehaviour
{
    //the Bounds object we want the camera to follow
    public Bounds targetBounds;
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        float screenRatio = (float)Screen.width / (float)Screen.height;
        float targetRatio = targetBounds.size.x / targetBounds.size.y;

        if (screenRatio >= targetRatio)
        {
            cam.orthographicSize = targetBounds.size.y / 2;
        }
        else
        {
            float differenceInSize = targetRatio / screenRatio;
            cam.orthographicSize = targetBounds.size.y / 2 * differenceInSize;
        }

        transform.position = new Vector3(targetBounds.center.x, targetBounds.center.y, -1f);
    }

}
