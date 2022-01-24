using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using Emgu.CV;
using Emgu.CV.Aruco;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class CameraProcessing : MonoBehaviour
{
    public int cameraIndex = 0;
    public int fps = 30;
    public int width = 1280;
    public int height = 720;
    public float markersLength = 80;
    public int threshold = 25;
    public PlayerHand[] hands;
    public RawImage imgDisplay;
    public GameObject calibrationBoard;

    private VideoCapture capture;
    private Mat camImg;
    private DetectorParameters arucoParameters;
    private Dictionary arucoDictionary;
    private bool calibrated = false;
    private Mat calibrationMat;

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

    PointF GetMarkerCenterCV(VectorOfPointF corner)
    {
        PointF center = new PointF(
            (corner[0].X + corner[1].X + corner[2].X + corner[3].X) / 4.0f,
            (corner[0].Y + corner[1].Y + corner[2].Y + corner[3].Y) / 4.0f);
        return center;
    }

    private void CaptureOnImageGrabbed(object sender, EventArgs e)
    {
        Mat camView = new Mat(capture.Height, capture.Width, DepthType.Default, 3);
        capture.Retrieve(camView);

        if (!camView.IsEmpty)
        {
            VectorOfInt ids = new VectorOfInt();
            VectorOfVectorOfPointF corners = new VectorOfVectorOfPointF();
            VectorOfVectorOfPointF rejected = new VectorOfVectorOfPointF();

            CvInvoke.CvtColor(camView, camView, ColorConversion.Bgr2Hsv);
            Mat camValue = new Mat(capture.Height, capture.Width, DepthType.Default, 1);
            CvInvoke.Threshold(camView.Split()[2], camValue, threshold, 255, ThresholdType.Binary);

            if (calibrated)
            {
                CvInvoke.WarpPerspective(camValue,camValue,calibrationMat,new Size(capture.Width, capture.Height));
            }

            ArucoInvoke.DetectMarkers(camValue, arucoDictionary, corners, ids, arucoParameters, rejected);

            if (ids.Size > 0)
            {
                ArucoInvoke.DrawDetectedMarkers(camValue, corners, ids, new MCvScalar(255, 0, 255));

                Dictionary<int, Vector2> markers = new Dictionary<int, Vector2>();

                for (int i = 0; i < ids.Size; i++)
                {
                    if(!markers.ContainsKey(ids[i]))
                        markers.Add(ids[i], GetMarkerCenter(corners[i]));
                }

                if(calibrated)
                    UnityMainThreadDispatcher.Instance().Enqueue(UpdateTrackers(markers));
                else
                {
                    if (ids.Size == 4)
                    {
                        int[] cornerIds = ids.ToArray();

                        PointF tl = corners[cornerIds.FirstOrDefault(i => i == 2)][0];
                        PointF tr = corners[cornerIds.FirstOrDefault(i => i == 3)][1];
                        PointF br = corners[cornerIds.FirstOrDefault(i => i == 0)][2];
                        PointF bl = corners[cornerIds.FirstOrDefault(i => i == 1)][3];

                        PointF[] rect = new PointF[] {tl, tr, br, bl};

                        PointF[] dst = new PointF[]
                        {
                            new PointF(0,0),
                            new PointF(capture.Width-1,0),
                            new PointF(capture.Width-1,capture.Height-1),
                            new PointF(0,capture.Height-1)
                        };

                        calibrationMat = CvInvoke.GetPerspectiveTransform(rect, dst);
                        calibrated = true;
                    }
                }
            }

            CvInvoke.CvtColor(camValue,camImg,ColorConversion.Gray2Bgr);
            UnityMainThreadDispatcher.Instance().Enqueue(UpdateImageDisplay(camImg));
        }
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
                    hands[i].position = Vector3.Scale(markers[i] - new Vector2(0.5f, 0.5f), new Vector3(7.8f,3.8f,1));
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
        if (calibrated && calibrationBoard.activeSelf)
            calibrationBoard.SetActive(false);

        if (Input.GetKeyDown(KeyCode.C))
        {
            calibrationBoard.SetActive(true);
            calibrated = false;
        }

        if(Input.GetKeyDown(KeyCode.V))
            imgDisplay.gameObject.SetActive(!imgDisplay.gameObject.activeSelf);

        if (Input.mouseScrollDelta.y != 0)
        {
            threshold += (int)Input.mouseScrollDelta.y;
        }
    }
}
