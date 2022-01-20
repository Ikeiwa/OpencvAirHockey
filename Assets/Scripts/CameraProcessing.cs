using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Emgu.CV;
using Emgu.CV.Aruco;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UnityEngine.UI;
using TMPro;

public class CameraProcessing : MonoBehaviour
{
    public int cameraIndex = 0;
    public int fps = 30;
    public int width = 1280;
    public int height = 720;
    public float markersLength = 80;
    public PlayerHand[] hands;
    public RawImage imgDisplay;

    private VideoCapture capture;
    private Mat camImg;
    private DetectorParameters arucoParameters;
    private Dictionary arucoDictionary;

    public void SetCamera(int id)
    {
        //id = id.Split(new string[] {" - "}, StringSplitOptions.RemoveEmptyEntries)[0];
        cameraIndex = id;
    }

    public void SetResolution(string res)
    {
        string[] infos = res.Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
        width = int.Parse(infos[0]);
        height = int.Parse(infos[1]);
        fps = int.Parse(infos[2]);
    }

    public void StartTracking()
    {
        arucoParameters = DetectorParameters.GetDefault();
        arucoDictionary = new Dictionary(Dictionary.PredefinedDictionaryName.Dict4X4_100);

        capture = new VideoCapture(cameraIndex, VideoCapture.API.DShow, new Tuple<CapProp, int>[]
        {
            new Tuple<CapProp, int>(CapProp.Fps, fps), 
            new Tuple<CapProp, int>(CapProp.FrameWidth,width), 
            new Tuple<CapProp, int>(CapProp.FrameHeight,height)
        });
        camImg = new Mat(capture.Height, capture.Width, DepthType.Default, 3);
        capture.ImageGrabbed += CaptureOnImageGrabbed;
        capture.Start();
    }

    Vector2 GetMarkerCenter(VectorOfPointF corner)
    {
        Vector2 center = new Vector2(
            (corner[0].X + corner[1].X + corner[2].X + corner[3].X) / 4.0f / capture.Width,
            1-((corner[0].Y + corner[1].Y + corner[2].Y + corner[3].Y) / 4.0f / capture.Height));
        return center;
    }

    private void CaptureOnImageGrabbed(object sender, EventArgs e)
    {
        capture.Retrieve(camImg);

        if (!camImg.IsEmpty)
        {
            VectorOfInt ids = new VectorOfInt();
            VectorOfVectorOfPointF corners = new VectorOfVectorOfPointF();
            VectorOfVectorOfPointF rejected = new VectorOfVectorOfPointF();
            ArucoInvoke.DetectMarkers(camImg, arucoDictionary, corners, ids, arucoParameters, rejected);

            if (ids.Size > 0)
            {
                ArucoInvoke.DrawDetectedMarkers(camImg, corners, ids, new MCvScalar(255, 0, 255));

                Dictionary<int, Vector2> markers = new Dictionary<int, Vector2>();

                for (int i = 0; i < ids.Size; i++)
                {
                    markers.Add(ids[i], GetMarkerCenter(corners[i]));
                }

                UnityMainThreadDispatcher.Instance().Enqueue(UpdateTrackers(markers));
            }
        }

        UnityMainThreadDispatcher.Instance().Enqueue(UpdateImageDisplay(camImg));
    }

    public IEnumerator UpdateImageDisplay(Mat imgResult)
    {
        imgDisplay.texture = imgResult.ToTexture2D();
        yield return null;
    }

    public IEnumerator UpdateTrackers(Dictionary<int,Vector2> markers)
    {
        if (hands.Length > 0)
        {
            for (int i = 0; i < hands.Length; i++)
            {
                if(markers.ContainsKey(i))
                    hands[i].position = (markers[i] - new Vector2(0.5f, 0.5f)) * 8;
            }
        }

        yield return null;
    }

    private void OnApplicationQuit()
    {
        if (capture != null)
        {
            capture.Stop();
            capture.Dispose();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }
}
