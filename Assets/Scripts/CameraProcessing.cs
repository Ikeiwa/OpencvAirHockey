using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UnityEngine.UI;

public class CameraProcessing : MonoBehaviour
{
    public int cameraIndex = 0;
    public int resolution = 0;
    public RawImage imgDisplay;
    private VideoCapture capture;
    private Mat camImg;
    private Texture2D camTexture;

    private bool hasFrame = false;

    private void Awake()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        int cameraCount = devices.Length;
        foreach (WebCamDevice device in devices)
        {
            Debug.Log(device.name);
        }

        capture = new VideoCapture(cameraIndex, VideoCapture.API.DShow, new Tuple<CapProp, int>[]
        {
            new Tuple<CapProp, int>(CapProp.Fps, 30), 
            new Tuple<CapProp, int>(CapProp.FrameWidth,1280), 
            new Tuple<CapProp, int>(CapProp.FrameHeight,720)
        });
        camImg = new Mat(capture.Height, capture.Width, DepthType.Default, 3);
        capture.ImageGrabbed += CaptureOnImageGrabbed;
        capture.Start();
    }

    private void CaptureOnImageGrabbed(object sender, EventArgs e)
    {
        capture.Retrieve(camImg);
        Debug.Log("Frame");
        hasFrame = true;
    }

    private void OnApplicationQuit()
    {
        capture.Stop();
        capture.Dispose();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (capture.IsOpened && hasFrame)
        {
            hasFrame = false;
            imgDisplay.texture = camImg.ToTexture2D();
            //capture.Grab();
        }
    }
}
