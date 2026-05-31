using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditController : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _fileName;

    [SerializeField]
    private Image _sourceImage;

    [SerializeField]
    private TMP_InputField _inputWidth;

    [SerializeField]
    private TMP_InputField _inputHeight;

    [SerializeField]
    private Image _resizedImage;

    [SerializeField]
    private TMP_InputField _inputPaletteCount;

    [SerializeField]
    private GameObject _colorPrefab;

    [SerializeField]
    private Transform _paletteTransform;

    private Texture2D _sourceTexture;
    private Texture2D _resizedTexture;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnOpen()
    {
        string path = UnityEditor.EditorUtility.OpenFilePanelWithFilters(
            "âÊëúÇëIë",
            "",
            new string[]
            {
                "Image Files", "png,jpg,jpeg"
            }
        );

        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("ÉLÉÉÉìÉZÉã");
            return;
        }

        Debug.Log(path);

        byte[] fileBytes = File.ReadAllBytes(path);

        _sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!_sourceTexture.LoadImage(fileBytes))
        {
            DestroyImmediate(_sourceTexture);
            Debug.LogError("âÊëúÇÃì«Ç›çûÇ›Ç…é∏îsÇµÇ‹ÇµÇΩ: " + path);
            return;
        }

        _fileName.SetText(path);

        Debug.Log($"ì«Ç›çûÇ›ê¨å˜: {_sourceTexture.width} x {_sourceTexture.height}");

        Sprite sprite = Sprite.Create(
                _sourceTexture,
                new Rect(0, 0, _sourceTexture.width, _sourceTexture.height),
                new Vector2(0.5f, 0.5f), 100f
            );

        _sourceImage.sprite = sprite;
        _sourceImage.SetNativeSize();

        RectTransform sourceImageRectTransform = _sourceImage.GetComponent<RectTransform>();
        RectTransform parentRectTransform = _sourceImage.transform.parent.GetComponent<RectTransform>();

        float parentHeight = parentRectTransform.rect.height;
        float aspectRatio = (float)_sourceTexture.width / _sourceTexture.height;
        float previewWidth = parentHeight * aspectRatio;

        sourceImageRectTransform.SetSizeWithCurrentAnchors( RectTransform.Axis.Vertical, parentHeight );
        sourceImageRectTransform.SetSizeWithCurrentAnchors( RectTransform.Axis.Horizontal, previewWidth );

        _inputWidth.text = _sourceTexture.width + "";
        _inputHeight.text = _sourceTexture.height + "";
    }

    public void OnChangeWidth()
    {
        float aspectRatio = (float)_sourceTexture.width / _sourceTexture.height;

        _inputHeight.SetTextWithoutNotify((int)(float.Parse(_inputWidth.text) / aspectRatio) + "");

        Resize();
    }

    public void OnChangeHeight()
    {
        float aspectRatio = (float)_sourceTexture.width / _sourceTexture.height;

        _inputWidth.SetTextWithoutNotify((int)(float.Parse(_inputHeight.text) * aspectRatio) + "");

        Resize();
    }

    public void Resize()
    {
        int destinationWidth = int.Parse(_inputWidth.text);
        int destinationHeight = int.Parse(_inputHeight.text);

        Color32[] sourcePixels = _sourceTexture.GetPixels32();
        Color32[] destinationPixels = new Color32[destinationWidth * destinationHeight];

        float scaleX = (float)_sourceTexture.width / destinationWidth;
        float scaleY = (float)_sourceTexture.height / destinationHeight;

        for (int destinationY = 0; destinationY < destinationHeight; destinationY++)
        {
            float sourceYMin = destinationY * scaleY;
            float sourceYMax = (destinationY + 1) * scaleY;

            int sourceYStart = Mathf.FloorToInt(sourceYMin);
            int sourceYEnd = Mathf.CeilToInt(sourceYMax);

            for (int destinationX = 0; destinationX < destinationWidth; destinationX++)
            {
                float sourceXMin = destinationX * scaleX;
                float sourceXMax = (destinationX + 1) * scaleX;

                int sourceXStart = Mathf.FloorToInt(sourceXMin);
                int sourceXEnd = Mathf.CeilToInt(sourceXMax);

                float red = 0;
                float green = 0;
                float blue = 0;
                float alpha = 0;
                float totalWeight = 0;

                for (int sourceY = sourceYStart; sourceY < sourceYEnd; sourceY++)
                {
                    float overlapY = Mathf.Min(sourceY + 1.0f, sourceYMax) - Mathf.Max(sourceY, sourceYMin);

                    if (overlapY <= 0.0f)
                    {
                        continue;
                    }

                    int rowOffset = sourceY * _sourceTexture.width;

                    for (int sourceX = sourceXStart; sourceX < sourceXEnd; sourceX++)
                    {
                        float overlapX = Mathf.Min(sourceX + 1.0f, sourceXMax) - Mathf.Max(sourceX, sourceXMin);

                        if (overlapX <= 0)
                        {
                            continue;
                        }

                        float weight = overlapX * overlapY;

                        Color32 color = sourcePixels[rowOffset + sourceX];

                        red += color.r * weight;
                        green += color.g * weight;
                        blue += color.b * weight;
                        alpha += color.a * weight;

                        totalWeight += weight;
                    }
                }

                destinationPixels[destinationY * destinationWidth + destinationX] =
                    new Color32(
                        (byte)Mathf.RoundToInt(red / totalWeight),
                        (byte)Mathf.RoundToInt(green / totalWeight),
                        (byte)Mathf.RoundToInt(blue / totalWeight),
                        (byte)Mathf.RoundToInt(alpha / totalWeight)
                    );
            }
        }

        _resizedTexture = new Texture2D(destinationWidth, destinationHeight, TextureFormat.RGBA32, false);

        _resizedTexture.SetPixels32(destinationPixels);

        _resizedTexture.filterMode = FilterMode.Point;
        _resizedTexture.wrapMode = TextureWrapMode.Clamp;
        _resizedTexture.Apply(false, false);

        Sprite sprite = Sprite.Create(_resizedTexture, new Rect(0, 0, _resizedTexture.width, _resizedTexture.height), new Vector2(0.5f, 0.5f), 100f );

        _resizedImage.sprite = sprite;
        _resizedImage.SetNativeSize();

        RectTransform resizedImageRectTransform = _resizedImage.GetComponent<RectTransform>();
        RectTransform parentRectTransform = _resizedImage.transform.parent.GetComponent<RectTransform>();

        float parentHeight = parentRectTransform.rect.height;
        float aspectRatio = (float)_resizedTexture.width / _resizedTexture.height;
        float previewWidth = parentHeight * aspectRatio;

        resizedImageRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, parentHeight);
        resizedImageRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, previewWidth);
    }

    public void OnChangePaletteCount()
    {
        Color32[] colors = GeneratePalette(_resizedTexture, int.Parse(_inputPaletteCount.text));
        Debug.Log(string.Join(", ", colors));

        for (int index = _paletteTransform.childCount - 1; index >= 0; index--)
        {
            Destroy(_paletteTransform.GetChild(index).gameObject);
        }

        foreach (Color32 color in colors)
        {
            GameObject gameObject = Instantiate(_colorPrefab, _paletteTransform);
            gameObject.transform.GetChild(0).GetComponent<Image>().color = color;
        }
    }


    // WuPaletteGenerator

    private struct Box
    {
        public int RedMinimum;
        public int RedMaximum;
        public int GreenMinimum;
        public int GreenMaximum;
        public int BlueMinimum;
        public int BlueMaximum;
    }

    private enum Axis
    {
        Red,
        Green,
        Blue
    }

    private const int IndexBits = 5;
    private const int IndexCount = 33;
    private const int TableSize = IndexCount * IndexCount * IndexCount;

    public static Color32[] GeneratePalette(Texture2D sourceTexture, int paletteColorCount)
    {
        Color32[] pixels = sourceTexture.GetPixels32();

        long[] weights = new long[TableSize];
        long[] reds = new long[TableSize];
        long[] greens = new long[TableSize];
        long[] blues = new long[TableSize];
        double[] moments = new double[TableSize];

        BuildHistogram(pixels, weights, reds, greens, blues, moments);
        BuildMoments(weights, reds, greens, blues, moments);

        Box[] boxes = new Box[paletteColorCount];
        double[] variances = new double[paletteColorCount];

        boxes[0] = new Box
        {
            RedMinimum = 0,
            RedMaximum = 32,
            GreenMinimum = 0,
            GreenMaximum = 32,
            BlueMinimum = 0,
            BlueMaximum = 32
        };

        int generatedColorCount = 1;

        for (int index = 1; index < paletteColorCount; index++)
        {
            int boxToSplit = 0;
            double maxVariance = variances[0];

            for (int boxIndex = 1; boxIndex < generatedColorCount; boxIndex++)
            {
                if (variances[boxIndex] > maxVariance)
                {
                    maxVariance = variances[boxIndex];
                    boxToSplit = boxIndex;
                }
            }

            if (!Cut( boxes[boxToSplit], out Box firstBox, out Box secondBox, weights, reds, greens, blues ))
            {
                break;
            }

            boxes[boxToSplit] = firstBox;
            boxes[generatedColorCount] = secondBox;

            variances[boxToSplit] = Variance(firstBox, weights, reds, greens, blues, moments);
            variances[generatedColorCount] = Variance(secondBox, weights, reds, greens, blues, moments);

            generatedColorCount++;
        }

        Color32[] palette = new Color32[generatedColorCount];

        for (int index = 0; index < generatedColorCount; index++)
        {
            long weight = Volume(boxes[index], weights);

            if (weight == 0)
            {
                palette[index] = new Color32(0, 0, 0, 255);
                continue;
            }

            byte red = (byte)Mathf.Clamp((Volume(boxes[index], reds) / weight), 0, 255);
            byte green = (byte)Mathf.Clamp((Volume(boxes[index], greens) / weight), 0, 255);
            byte blue = (byte)Mathf.Clamp((Volume(boxes[index], blues) / weight), 0, 255);

            palette[index] = new Color32(red, green, blue, 255);
        }

        return palette;
    }

    private static void BuildHistogram( Color32[] pixels, long[] weights, long[] reds, long[] greens, long[] blues, double[] moments )
    {
        foreach (Color32 color in pixels)
        {
            int redIndex = (color.r >> (8 - IndexBits)) + 1;
            int greenIndex = (color.g >> (8 - IndexBits)) + 1;
            int blueIndex = (color.b >> (8 - IndexBits)) + 1;

            int tableIndex = GetIndex(redIndex, greenIndex, blueIndex);

            weights[tableIndex]++;
            reds[tableIndex] += color.r;
            greens[tableIndex] += color.g;
            blues[tableIndex] += color.b;

            moments[tableIndex] += color.r * color.r + color.g * color.g + color.b * color.b;
        }
    }

    private static void BuildMoments( long[] weights, long[] reds, long[] greens, long[] blues, double[] moments )
    {
        for (int redIndex = 1; redIndex < IndexCount; redIndex++)
        {
            long[] areaWeights = new long[IndexCount];
            long[] areaReds = new long[IndexCount];
            long[] areaGreens = new long[IndexCount];
            long[] areaBlues = new long[IndexCount];
            double[] areaMoments = new double[IndexCount];

            for (int greenIndex = 1; greenIndex < IndexCount; greenIndex++)
            {
                long lineWeight = 0;
                long lineRed = 0;
                long lineGreen = 0;
                long lineBlue = 0;
                double lineMoment = 0;

                for (int blueIndex = 1; blueIndex < IndexCount; blueIndex++)
                {
                    int index = GetIndex(redIndex, greenIndex, blueIndex);

                    lineWeight += weights[index];
                    lineRed += reds[index];
                    lineGreen += greens[index];
                    lineBlue += blues[index];
                    lineMoment += moments[index];

                    areaWeights[blueIndex] += lineWeight;
                    areaReds[blueIndex] += lineRed;
                    areaGreens[blueIndex] += lineGreen;
                    areaBlues[blueIndex] += lineBlue;
                    areaMoments[blueIndex] += lineMoment;

                    int previousRedIndex = GetIndex(redIndex - 1, greenIndex, blueIndex);

                    weights[index] = weights[previousRedIndex] + areaWeights[blueIndex];
                    reds[index] = reds[previousRedIndex] + areaReds[blueIndex];
                    greens[index] = greens[previousRedIndex] + areaGreens[blueIndex];
                    blues[index] = blues[previousRedIndex] + areaBlues[blueIndex];
                    moments[index] = moments[previousRedIndex] + areaMoments[blueIndex];
                }
            }
        }
    }
    private static bool Cut( Box box, out Box firstBox, out Box secondBox, long[] weights, long[] reds, long[] greens, long[] blues )
    {
        double redScore = Maximize(box, Axis.Red, out int redCut, weights, reds, greens, blues);
        double greenScore = Maximize(box, Axis.Green, out int greenCut, weights, reds, greens, blues);
        double blueScore = Maximize(box, Axis.Blue, out int blueCut, weights, reds, greens, blues);

        Axis cutAxis;
        int cutPosition;

        if (redScore >= greenScore && redScore >= blueScore)
        {
            cutAxis = Axis.Red;
            cutPosition = redCut;
        }
        else if (greenScore >= redScore && greenScore >= blueScore)
        {
            cutAxis = Axis.Green;
            cutPosition = greenCut;
        }
        else
        {
            cutAxis = Axis.Blue;
            cutPosition = blueCut;
        }

        if (cutPosition < 0)
        {
            firstBox = box;
            secondBox = box;
            return false;
        }

        firstBox = box;
        secondBox = box;

        switch (cutAxis)
        {
            case Axis.Red:
                firstBox.RedMaximum = cutPosition;
                secondBox.RedMinimum = cutPosition;
                break;

            case Axis.Green:
                firstBox.GreenMaximum = cutPosition;
                secondBox.GreenMinimum = cutPosition;
                break;

            case Axis.Blue:
                firstBox.BlueMaximum = cutPosition;
                secondBox.BlueMinimum = cutPosition;
                break;
        }

        return true;
    }

    private static double Maximize( Box box, Axis axis, out int cutPosition, long[] weights, long[] reds, long[] greens, long[] blues )
    {
        cutPosition = -1;

        long wholeWeight = Volume(box, weights);
        long wholeRed = Volume(box, reds);
        long wholeGreen = Volume(box, greens);
        long wholeBlue = Volume(box, blues);

        if (wholeWeight == 0)
        {
            return 0.0;
        }

        double bestScore = 0.0;

        int firstPosition;
        int lastPosition;

        switch (axis)
        {
            case Axis.Red:
                firstPosition = box.RedMinimum + 1;
                lastPosition = box.RedMaximum;
                break;

            case Axis.Green:
                firstPosition = box.GreenMinimum + 1;
                lastPosition = box.GreenMaximum;
                break;

            default:
                firstPosition = box.BlueMinimum + 1;
                lastPosition = box.BlueMaximum;
                break;
        }

        for (int position = firstPosition; position < lastPosition; position++)
        {
            long firstWeight = PartialVolume(box, axis, position, weights);
            long firstRed = PartialVolume(box, axis, position, reds);
            long firstGreen = PartialVolume(box, axis, position, greens);
            long firstBlue = PartialVolume(box, axis, position, blues);

            if (firstWeight == 0)
            {
                continue;
            }

            long secondWeight = wholeWeight - firstWeight;

            if (secondWeight == 0)
            {
                continue;
            }

            long secondRed = wholeRed - firstRed;
            long secondGreen = wholeGreen - firstGreen;
            long secondBlue = wholeBlue - firstBlue;

            double firstScore =
                ((double)firstRed * firstRed +
                 (double)firstGreen * firstGreen +
                 (double)firstBlue * firstBlue) / firstWeight;

            double secondScore =
                ((double)secondRed * secondRed +
                 (double)secondGreen * secondGreen +
                 (double)secondBlue * secondBlue) / secondWeight;

            double score = firstScore + secondScore;

            if (score > bestScore)
            {
                bestScore = score;
                cutPosition = position;
            }
        }

        return bestScore;
    }

    private static long PartialVolume(Box box, Axis axis, int position, long[] table)
    {
        Box partialBox = box;

        switch (axis)
        {
            case Axis.Red:
                partialBox.RedMaximum = position;
                break;

            case Axis.Green:
                partialBox.GreenMaximum = position;
                break;

            case Axis.Blue:
                partialBox.BlueMaximum = position;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
        }

        return Volume(partialBox, table);
    }

    private static double Variance( Box box, long[] weights, long[] reds, long[] greens, long[] blues, double[] moments )
    {
        long weight = Volume(box, weights);

        if (weight == 0)
        {
            return 0;
        }

        long red = Volume(box, reds);
        long green = Volume(box, greens);
        long blue = Volume(box, blues);

        double moment = Volume(box, moments);

        return moment - ((double)red * red + (double)green * green + (double)blue * blue) / weight;
    }

    private static long Volume(Box box, long[] table)
    {
        return
            table[GetIndex(box.RedMaximum, box.GreenMaximum, box.BlueMaximum)]
            - table[GetIndex(box.RedMaximum, box.GreenMaximum, box.BlueMinimum)]
            - table[GetIndex(box.RedMaximum, box.GreenMinimum, box.BlueMaximum)]
            + table[GetIndex(box.RedMaximum, box.GreenMinimum, box.BlueMinimum)]
            - table[GetIndex(box.RedMinimum, box.GreenMaximum, box.BlueMaximum)]
            + table[GetIndex(box.RedMinimum, box.GreenMaximum, box.BlueMinimum)]
            + table[GetIndex(box.RedMinimum, box.GreenMinimum, box.BlueMaximum)]
            - table[GetIndex(box.RedMinimum, box.GreenMinimum, box.BlueMinimum)];
    }

    private static double Volume(Box box, double[] table)
    {
        return
            table[GetIndex(box.RedMaximum, box.GreenMaximum, box.BlueMaximum)]
            - table[GetIndex(box.RedMaximum, box.GreenMaximum, box.BlueMinimum)]
            - table[GetIndex(box.RedMaximum, box.GreenMinimum, box.BlueMaximum)]
            + table[GetIndex(box.RedMaximum, box.GreenMinimum, box.BlueMinimum)]
            - table[GetIndex(box.RedMinimum, box.GreenMaximum, box.BlueMaximum)]
            + table[GetIndex(box.RedMinimum, box.GreenMaximum, box.BlueMinimum)]
            + table[GetIndex(box.RedMinimum, box.GreenMinimum, box.BlueMaximum)]
            - table[GetIndex(box.RedMinimum, box.GreenMinimum, box.BlueMinimum)];
    }

    private static int GetIndex(int redIndex, int greenIndex, int blueIndex)
    {
        return (redIndex * IndexCount + greenIndex) * IndexCount + blueIndex;
    }
}