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

public class CameraProcessing : MonoBehaviour
{
    public int cameraIndex = 0;
    public string leftMarker = "marker_left.png";
    public string rightMarker = "marker_right.png";
    public float markersLength = 80;
    public PlayerHand[] hands;
    public RawImage imgDisplay;

    private VideoCapture capture;
    private Mat camImg;
    private DetectorParameters arucoParameters;
    private Dictionary arucoDictionary;

    private bool hasFrame = false;

    private void Awake()
    {
        arucoParameters = DetectorParameters.GetDefault();
        arucoDictionary = new Dictionary(Dictionary.PredefinedDictionaryName.Dict4X4_100);

        WebCamDevice[] devices = WebCamTexture.devices;

        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log(i + " : " + devices[i].name);
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

                /*#region Estimate pose for each marker using camera calibration matrix and distortion coefficents
                Mat rvecs = new Mat(); // rotation vector
                Mat tvecs = new Mat(); // translation vector
                ArucoInvoke.EstimatePoseSingleMarkers(corners, markersLength, cameraMatrix, distortionMatrix, rvecs, tvecs);
                #endregion

                #region Draw 3D orthogonal axis on markers using estimated pose
                for (int i = 0; i < ids.Size; i++)
                {
                    using (Mat rvecMat = rvecs.Row(i))
                    using (Mat tvecMat = tvecs.Row(i))
                    using (VectorOfDouble rvec = new VectorOfDouble())
                    using (VectorOfDouble tvec = new VectorOfDouble())
                    {
                        double[] values = new double[3];
                        rvecMat.CopyTo(values);
                        rvec.Push(values);
                        tvecMat.CopyTo(values);
                        tvec.Push(values);
                        ArucoInvoke.DrawAxis(camImg,
                            cameraMatrix,
                            distortionMatrix,
                            rvec,
                            tvec,
                            markersLength * 0.5f);

                    }
                }
                #endregion*/
            }
        }

        hasFrame = true;
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
        }
    }
}
