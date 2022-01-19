using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum FieldSide
{
    None,
    Left,
    Right
}

public class ScoreManager : MonoBehaviour
{
    public TextMeshPro leftScoreDisplay;
    public TextMeshPro rightScoreDisplay;
    public Ball ball;

    private int scoreLeft = 0;
    private int scoreRight = 0;
    private FieldSide hasScored = FieldSide.None;

    public void AddPoint(bool right)
    {
        if (hasScored == FieldSide.None)
        {
            if (right)
            {
                scoreRight++;
                rightScoreDisplay.text = scoreRight.ToString();
                hasScored = FieldSide.Right;
            }
            else
            {
                scoreLeft++;
                leftScoreDisplay.text = scoreLeft.ToString();
                hasScored = FieldSide.Left;
            }

            ball.Respawn(hasScored);
            hasScored = FieldSide.None;
        }
    }
}
