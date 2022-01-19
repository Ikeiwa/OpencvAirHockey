using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D), typeof(BezierCurve))]
[ExecuteInEditMode]
public class BezierCurveCollider2D : MonoBehaviour
{
    private EdgeCollider2D col;
    private BezierCurve curve;

    private void Awake()
    {
        col = GetComponent<EdgeCollider2D>();
        curve = GetComponent<BezierCurve>();
        curve.OnCurveChanged += UpdateCollider;

        UpdateCollider();
    }

    private void OnDestroy()
    {
        if(curve)
            curve.OnCurveChanged -= UpdateCollider;
    }

    public void UpdateCollider()
    {

        if (curve.points != null)
        {
            List<Vector2> points = new List<Vector2>();

            for (int i = 0; i < curve.points.Length; i++)
            {
                points.Add(curve.points[i]);
            }
            col.SetPoints(points);
        }
    }
}
