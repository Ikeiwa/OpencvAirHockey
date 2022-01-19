using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BezierCurve : MonoBehaviour
{
    [System.Serializable]
    public class Handle
    {
        public Vector3 position;
        public Vector3 directionForward = Vector3.forward;
        public Vector3 directionBack = Vector3.back;
        public bool splitted = false;
    }

    public delegate void OnCurveChangedHandler();
    public event OnCurveChangedHandler OnCurveChanged;

    public Handle[] handles;
    public bool loop = false;
    public bool flat = false;

    public Vector3[] points { get; private set; }

    [Range(1, 100)]
    public int SubSteps = 10;
    [Range(0.0001f, 1f)]
    public float percentStep = 0.01f;

    private void OnDestroy()
    {
        OnCurveChanged = null;
    }

    private void OnDrawGizmos()
    {
        if (handles != null && handles.Length > 1)
        {
            int length = handles.Length - (loop ? 0 : 1);
            for (int i = 0; i < length; i++)
            {
                float step = 1.0f / SubSteps;
                for (int s = 0; s < SubSteps; s++)
                {
                    float percent = step * s;
                    Gizmos.DrawLine(GetPoint(i,percent),GetPoint(i,percent+step));
                }
            }
        }
    }

    public Vector3 GetPoint(float percent)
    {
        if (loop)
            percent = percent % 1;
        else
            percent = Mathf.Clamp01(percent);

        percent *= handles.Length;

        int handle = Mathf.FloorToInt(percent);
        float t = percent - handle;

        return GetPoint(handle, t);
    }

    public Vector3 GetPoint (int handle, float t)
    {
        Handle h1 = handles[handle];
        Handle h2 = handles[(handle+1) % handles.Length];

        t = Mathf.Clamp01(t);
        float invT = 1f - t;
        return transform.TransformPoint(
            invT * invT * invT * h1.position +
            3f * invT * invT * t * (h1.position+h1.directionForward) +
            3f * invT * t * t * (h2.position+h2.directionBack) +
            t * t * t * h2.position);
    }

    public void UpdateCurve()
    {
        if (flat && handles != null && handles.Length > 1)
        {
            List<Vector3> newPoints = new List<Vector3>();

            int length = handles.Length - (loop ? 0 : 1);
            for (int i = 0; i < length; i++)
            {
                float step = 1.0f / SubSteps;
                for (int s = 0; s < SubSteps; s++)
                {
                    float percent = step * s;
                    newPoints.Add(transform.InverseTransformPoint(GetPoint(i, percent)));
                }
            }

            newPoints.Add(transform.InverseTransformPoint(GetPoint(handles.Length - 2, 1)));

            if (loop)
                newPoints.Add(transform.InverseTransformPoint(GetPoint(0, 0)));

            points = newPoints.ToArray();
        }

        OnCurveChanged?.Invoke();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(BezierCurve))]
public class BezierCurveDrawer : Editor
{
    private int currentHandle = 0;
    private int currentSide = 0;

    void OnSceneGUI()
    {
        BezierCurve curve = (BezierCurve)target;
        Handles.CapFunction handleCap = Handles.SphereHandleCap;
        Handles.matrix = curve.transform.localToWorldMatrix;

        if (curve.handles != null && curve.handles.Length > 0)
        {
            for (int i = 0; i < curve.handles.Length; i++)
            {
                bool isCurrent = currentHandle == i;
                
                Vector3 handleStart = curve.handles[i].position + curve.handles[i].directionBack;
                Vector3 handleEnd = curve.handles[i].position + curve.handles[i].directionForward;

                if (isCurrent)
                {
                    Handles.color = Color.green;
                    Handles.DrawLine(curve.handles[i].position,handleEnd);
                    Handles.DrawLine(curve.handles[i].position,handleStart);

                    if (currentSide == 1)
                    {
                        handleStart = Handles.PositionHandle(handleStart, Quaternion.identity);
                        if (curve.flat)
                            handleStart.Scale(new Vector3(1,1,0));
                        curve.handles[i].directionBack = handleStart - curve.handles[i].position;
                        if (!curve.handles[i].splitted)
                            curve.handles[i].directionForward = -curve.handles[i].directionBack;
                    }
                    else
                    {
                        handleEnd = Handles.PositionHandle(handleEnd, Quaternion.identity);
                        if (curve.flat)
                            handleEnd.Scale(new Vector3(1, 1, 0));
                        curve.handles[i].directionForward = handleEnd - curve.handles[i].position;
                        if (!curve.handles[i].splitted)
                            curve.handles[i].directionBack = -curve.handles[i].directionForward;
                    }
                    
                    curve.handles[i].position = Handles.PositionHandle(curve.handles[i].position, Quaternion.identity);
                    if (curve.flat)
                        curve.handles[i].position.Scale(new Vector3(1, 1, 0));

                    if (Handles.Button(handleStart, Quaternion.identity, 0.025f, 0.025f, handleCap)) {currentSide = 1;}
                    if (Handles.Button(handleEnd, Quaternion.identity, 0.025f, 0.025f, handleCap)) {currentSide = 0;}

                    Handles.color = Color.red;
                    Handles.SphereHandleCap(0,curve.handles[i].position,Quaternion.identity, 0.05f, EventType.Repaint);
                }
                else
                {
                    
                    Handles.color = curve.handles[i].splitted ? Color.yellow : Color.blue;
                    if (Handles.Button(curve.handles[i].position, Quaternion.identity, 0.05f, 0.05f, handleCap))
                    {
                        currentHandle = i;
                    }
                }

                Handles.Label(curve.handles[i].position,i.ToString());
            }

            curve.UpdateCurve();
        }
        
    }
}
#endif