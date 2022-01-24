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
    private DetectorParameters arucoParameters;
    private Dictionary arucoDictionary;
    private bool calibrated = false;
    private Mat calibrationMat;
    private bool previewActive = false;
    private Texture2D previewTexture;

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
        capture.ImageGrabbed += CaptureOnImageGrabbed;
        capture.Start();
    }

    Vector2 GetMarkerCenter(VectorOfPointF corner)
    {
        Vector2 center = new Vector2(
            (corner[0].X + corner[1].X + corner[2].X + corner[3].X) / 4.0f / capture.Width,
            1 - ((corner[0].Y + corner[1].Y + corner[2].Y + corner[3].Y) / 4.0f / capture.Height));
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
                CvInvoke.WarpPerspective(camValue, camValue, calibrationMat, new Size(capture.Width, capture.Height));
            }

            ArucoInvoke.DetectMarkers(camValue, arucoDictionary, corners, ids, arucoParameters, rejected);

            if (ids.Size > 0)
            {
                ArucoInvoke.DrawDetectedMarkers(camValue, corners, ids, new MCvScalar(255, 0, 255));

                Dictionary<int, Vector2> markers = new Dictionary<int, Vector2>();

                for (int i = 0; i < ids.Size; i++)
                {
                    if (!markers.ContainsKey(ids[i]))
                        markers.Add(ids[i], GetMarkerCenter(corners[i]));
                }

                if (calibrated)
                    UnityMainThreadDispatcher.Instance().Enqueue(UpdateTrackers(new Dictionary<int, Vector2>(markers)));
                else
                {
                    if (ids.Size == 4)
                    {

                        List<float> sums = new List<float>(new float[]
                        {
                            GetMarkerCenterCV(corners[0]).X + GetMarkerCenterCV(corners[0]).Y,
                            GetMarkerCenterCV(corners[1]).X + GetMarkerCenterCV(corners[1]).Y,
                            GetMarkerCenterCV(corners[2]).X + GetMarkerCenterCV(corners[2]).Y,
                            GetMarkerCenterCV(corners[3]).X + GetMarkerCenterCV(corners[3]).Y
                        });

                        int indexTL = sums.IndexOf(sums.Min()), indexBR = sums.IndexOf(sums.Max());
                        PointF topLeft = corners[indexTL][0];
                        PointF botRight = corners[indexBR][2];
                        List<int> idbuffers = new List<int>(new int[] { 0, 1, 2, 3 });
                        idbuffers.RemoveAt(indexBR < indexTL ? indexTL : indexBR);
                        idbuffers.RemoveAt(indexBR < indexTL ? indexBR : indexTL);

                        PointF topRight = corners[idbuffers[0]][0].Y < corners[idbuffers[1]][0].Y ? corners[idbuffers[0]][1] : corners[idbuffers[1]][1];
                        PointF botLeft = corners[idbuffers[0]][0].Y < corners[idbuffers[1]][0].Y ? corners[idbuffers[1]][3] : corners[idbuffers[0]][3];

                        PointF[] rect = new PointF[] { topLeft, topRight, botRight, botLeft };

                        PointF[] dst = new PointF[]
                        {
                            new PointF(0,0),
                            new PointF(capture.Width-1,0),
                            new PointF(capture.Width-1,capture.Height-1),
                            new PointF(0,capture.Height-1)
                        };

                        calibrationMat = CvInvoke.GetPerspectiveTransform(rect, dst);
                        if (!previewActive)
                            calibrated = true;
                    }
                }
            }

            if (previewActive)
            {
                Mat camImg = new Mat();
                CvInvoke.CvtColor(camValue, camImg, ColorConversion.Gray2Bgr);
                UnityMainThreadDispatcher.Instance().Enqueue(UpdateImageDisplay(camImg));
            }
            
            camValue.Dispose();
        }
    }

    public IEnumerator UpdateImageDisplay(Mat imgResult)
    {
        Destroy(previewTexture);
        imgResult.ToTexture2D(ref previewTexture);
        imgDisplay.texture = previewTexture;
        imgResult.Dispose();
        yield return null;
    }

    public IEnumerator UpdateTrackers(Dictionary<int, Vector2> markers)
    {
        if (hands.Length > 0)
        {
            for (int i = 0; i < hands.Length; i++)
            {
                if (markers.ContainsKey(i))
                    hands[i].position = Vector3.Scale(markers[i] - new Vector2(0.5f, 0.5f), new Vector3(7.8f, 3.8f, 1));
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

        if (Input.GetKeyDown(KeyCode.V))
        {
            previewActive = !previewActive;
            imgDisplay.gameObject.SetActive(previewActive);
        }

        if (Input.mouseScrollDelta.y != 0)
        {
            threshold += (int)Input.mouseScrollDelta.y;
        }
    }
}
