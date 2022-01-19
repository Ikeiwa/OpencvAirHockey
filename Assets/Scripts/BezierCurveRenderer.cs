using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer), typeof(BezierCurve))]
[ExecuteInEditMode]
public class BezierCurveRenderer : MonoBehaviour
{
    private LineRenderer line;
    private BezierCurve curve;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        curve = GetComponent<BezierCurve>();
        curve.OnCurveChanged += UpdateLine;

        UpdateLine();
    }

    private void OnDestroy()
    {
        if (curve)
            curve.OnCurveChanged -= UpdateLine;
    }

    [ContextMenu("Update Line")]
    public void UpdateLine()
    {
        if(!line) line = GetComponent<LineRenderer>();
        if(!curve) curve = GetComponent<BezierCurve>();
        line.useWorldSpace = false;

        if (curve.points != null)
        {
            line.positionCount = curve.points.Length;
            line.SetPositions(curve.points);
        }
    }
}
