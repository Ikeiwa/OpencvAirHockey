using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using UnityEngine;
using Emgu.CV;
using Emgu.CV.Aruco;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MarkerGenerator : MonoBehaviour
{
    public int markersX = 4;
    public int markersY = 4;
    public int markersLength = 80;
    public int markersSeparation = 30;
    public int borderBits = 1;
    public string markerName = "marker";

#if UNITY_EDITOR
    [ContextMenu("Generate Tag")]
    void GenerateTag()
    {
        Size imageSize = new Size();
        int margins = markersSeparation;
        imageSize.Width = markersX * (markersLength + markersSeparation) - markersSeparation + 2 * margins;
        imageSize.Height = markersY * (markersLength + markersSeparation) - markersSeparation + 2 * margins;

        Dictionary dictionary = new Dictionary(Dictionary.PredefinedDictionaryName.Dict4X4_100);
        GridBoard board = new GridBoard(markersX, markersY, markersLength, markersSeparation, dictionary);
        Mat boardImage = new Mat();
        board.Draw(imageSize, boardImage, margins, borderBits);
        CvInvoke.Imwrite(Path.Combine(Application.dataPath, "StreamingAssets", markerName+".png"), boardImage);
    }
#endif
}
