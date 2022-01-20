using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

[Serializable]
public enum WebcamFormat
{
    Any,
    Unknown,
    ARGB = 100,
    XRGB,
    I420 = 200,
    NV12,
    YV12,
    Y800,
    YVYU = 300,
    YUY2,
    UYVY,
    HDYC,
    MJPEG = 400,
    H264
}

[Serializable]
public struct WebcamCaps
{
    public int id;
    public int minCX;
    public int minCY;
    public int maxCX;
    public int maxCY;
    public int granularityCX;
    public int granularityCY;
    public int minInterval;
    public int maxInterval;
    public int rating;
    public WebcamFormat format;

    public float Fps()
    {
        return 10000000f / minInterval;
    }

    public override string ToString()
    {
        return minCX + " - " + minCY + " - " + (int)Fps() + " - " + format;
    }
}

[Serializable]
public class WebcamInfos
{
    public int id;
    public string name;
    public string path;
    public WebcamCaps[] caps;

    public override string ToString()
    {
        return id + " - " + name;
    }
}

[Serializable]
public class WebcamList
{
    public List<WebcamInfos> list;
}

public class WebcamManager : MonoBehaviour
{
    #region DllImport
    [DllImport("dshowcapture_x64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "create_capture")]
    private static extern IntPtr create_capture_x64();

    [DllImport("dshowcapture_x64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_json_length")]
    private static extern int get_json_length_x64(IntPtr cap);

    [DllImport("dshowcapture_x64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_json")]
    private static extern void get_json_x64(IntPtr cap, [Out] StringBuilder namebuffer, int bufferlength);

    [DllImport("dshowcapture_x64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "destroy_capture")]
    private static extern void destroy_capture_x64(IntPtr cap);

    [DllImport("dshowcapture_x86", CallingConvention = CallingConvention.Cdecl, EntryPoint = "create_capture")]
    private static extern IntPtr create_capture_x86();

    [DllImport("dshowcapture_x86", CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_json_length")]
    private static extern int get_json_length_x86(IntPtr cap);

    [DllImport("dshowcapture_x86", CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_json")]
    private static extern void get_json_x86(IntPtr cap, [Out] StringBuilder namebuffer, int bufferlength);

    [DllImport("dshowcapture_x86", CallingConvention = CallingConvention.Cdecl, EntryPoint = "destroy_capture")]
    private static extern void destroy_capture_x86(IntPtr cap);
    #endregion

    public List<WebcamInfos> cameras;
    public TMP_Dropdown cameraSelector;
    public TMP_Dropdown resolutionSelector;
    public CameraProcessing cameraProcessing;

    private static string ListCameraDetails_x64()
    {
        IntPtr cap = create_capture_x64();
        int num = get_json_length_x64(cap);
        StringBuilder stringBuilder = new StringBuilder(num);
        get_json_x64(cap, stringBuilder, num);
        destroy_capture_x64(cap);
        return stringBuilder.ToString();
    }

    private static string ListCameraDetails_x86()
    {
        IntPtr cap = create_capture_x86();
        int num = get_json_length_x86(cap);
        StringBuilder stringBuilder = new StringBuilder(num);
        get_json_x86(cap, stringBuilder, num);
        destroy_capture_x86(cap);
        return stringBuilder.ToString();
    }

    public List<WebcamInfos> ListCameras()
    {
        string str = null;
        string str2;
        if (Environment.Is64BitProcess)
        {
            str2 = ListCameraDetails_x64();
        }
        else
        {
            str2 = ListCameraDetails_x86();
        }

        //Debug.Log("Camera JSON: " + str2);

        return JsonUtility.FromJson<WebcamList>("{\"list\":" + str2 + "}").list;
    }

    private void Awake()
    {
        cameraSelector.onValueChanged.AddListener(OnCameraSelected);
        resolutionSelector.onValueChanged.AddListener(OnResolutionSelected);
    }

    private void OnCameraSelected(int index)
    {
        List<string> resSelection = new List<string>();
        foreach (WebcamCaps caps in cameras[index].caps)
        {
            resSelection.Add(caps.ToString());
        }
        resolutionSelector.ClearOptions();
        resolutionSelector.AddOptions(resSelection);
        cameraProcessing.SetCamera(index);
    }

    private void OnResolutionSelected(int index)
    {
        cameraProcessing.SetResolution(resolutionSelector.options[index].text);
    }

    private void Start()
    {
        cameras = ListCameras();

        if (cameras.Count > 0)
        {
            List<string> camSelection = new List<string>();
            cameras.ForEach(x => {
                camSelection.Add(x.ToString());
            });

            cameraSelector.AddOptions(camSelection);
            OnCameraSelected(0);
            OnResolutionSelected(0);
        }
    }
}
