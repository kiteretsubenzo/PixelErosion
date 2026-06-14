using UnityEngine;
using UnityEngine.UI;

public class EditUtility
{

    // リサイズ
    public static Texture2D Resize(int destinationWidth, int destinationHeight, Texture2D sourceTexture)
    {
        Color32[] sourcePixels = sourceTexture.GetPixels32();
        Color32[] destinationPixels = new Color32[destinationWidth * destinationHeight];

        float scaleX = (float)sourceTexture.width / destinationWidth;
        float scaleY = (float)sourceTexture.height / destinationHeight;

        for (int destinationY = 0; destinationY < destinationHeight; destinationY++)
        { 
            float sourceYMin = destinationY * scaleY;
            float sourceYMax = (destinationY + 1) * scaleY;

            int sourceYStart = Mathf.Clamp(Mathf.FloorToInt(sourceYMin), 0, sourceTexture.height - 1);
            int sourceYEnd = Mathf.Clamp(Mathf.CeilToInt(sourceYMax), 0, sourceTexture.height);

            for (int destinationX = 0; destinationX < destinationWidth; destinationX++)
            {
                float sourceXMin = destinationX * scaleX;
                float sourceXMax = (destinationX + 1) * scaleX;

                int sourceXStart = Mathf.Clamp(Mathf.FloorToInt(sourceXMin), 0, sourceTexture.width - 1);
                int sourceXEnd = Mathf.Clamp(Mathf.CeilToInt(sourceXMax), 0, sourceTexture.width);

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

                    int rowOffset = sourceY * sourceTexture.width;

                    for (int sourceX = sourceXStart; sourceX < sourceXEnd; sourceX++)
                    {
                        float overlapX = Mathf.Min(sourceX + 1.0f, sourceXMax) - Mathf.Max(sourceX, sourceXMin);

                        if (overlapX <= 0)
                        {
                            continue;
                        }

                        float weight = overlapX * overlapY;

                        if(sourcePixels.Length <= rowOffset + sourceX)
                        {
                            Debug.Log("error");
                        }
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

        Texture2D resizedTexture = new Texture2D(destinationWidth, destinationHeight, TextureFormat.RGBA32, false);

        resizedTexture.SetPixels32(destinationPixels);

        resizedTexture.filterMode = FilterMode.Point;
        resizedTexture.wrapMode = TextureWrapMode.Clamp;
        resizedTexture.Apply(false, false);

        return resizedTexture;
    }

    // パレット作成（WuPaletteGenerator）
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

            if (!Cut(boxes[boxToSplit], out Box firstBox, out Box secondBox, weights, reds, greens, blues))
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

    private static void BuildHistogram(Color32[] pixels, long[] weights, long[] reds, long[] greens, long[] blues, double[] moments)
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

    private static void BuildMoments(long[] weights, long[] reds, long[] greens, long[] blues, double[] moments)
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
    private static bool Cut(Box box, out Box firstBox, out Box secondBox, long[] weights, long[] reds, long[] greens, long[] blues)
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

    private static double Maximize(Box box, Axis axis, out int cutPosition, long[] weights, long[] reds, long[] greens, long[] blues)
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
                throw new System.ArgumentOutOfRangeException(nameof(axis), axis, null);
        }

        return Volume(partialBox, table);
    }

    private static double Variance(Box box, long[] weights, long[] reds, long[] greens, long[] blues, double[] moments)
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


    // ディザあり減色
    public static byte[,] ReduceWithDither(Texture2D sourceTexture, Color32[] palette)
    {
        int width = sourceTexture.width;
        int height = sourceTexture.height;

        Color32[] sourcePixels = sourceTexture.GetPixels32();

        float[] redValues = new float[sourcePixels.Length];
        float[] greenValues = new float[sourcePixels.Length];
        float[] blueValues = new float[sourcePixels.Length];
        float[] alphaValues = new float[sourcePixels.Length];

        for (int index = 0; index < sourcePixels.Length; index++)
        {
            redValues[index] = sourcePixels[index].r;
            greenValues[index] = sourcePixels[index].g;
            blueValues[index] = sourcePixels[index].b;
            alphaValues[index] = sourcePixels[index].a;
        }

        byte[,] matrix = new byte[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (height - 1 - y) * width + x;

                Color32 oldColor = new Color32(
                    (byte)Mathf.Clamp(Mathf.RoundToInt(redValues[index]), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(greenValues[index]), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(blueValues[index]), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(alphaValues[index]), 0, 255)
                );

                byte nearestIndex = FindNearestColorIndex(oldColor, palette);
                Color32 newColor = palette[nearestIndex];

                matrix[y, x] = nearestIndex;

                float redError = redValues[index] - newColor.r;
                float greenError = greenValues[index] - newColor.g;
                float blueError = blueValues[index] - newColor.b;
                float alphaError = alphaValues[index] - newColor.a;

                AddError(x + 1, y, width, height, redValues, greenValues, blueValues, alphaValues, redError, greenError, blueError, alphaError, 7f / 16f);
                AddError(x - 1, y + 1, width, height, redValues, greenValues, blueValues, alphaValues, redError, greenError, blueError, alphaError, 3f / 16f);
                AddError(x, y + 1, width, height, redValues, greenValues, blueValues, alphaValues, redError, greenError, blueError, alphaError, 5f / 16f);
                AddError(x + 1, y + 1, width, height, redValues, greenValues, blueValues, alphaValues, redError, greenError, blueError, alphaError, 1f / 16f);
            }
        }

        return matrix;
    }

    // ディザなし減色
    public static byte[,] ReduceWithoutDither(Texture2D sourceTexture, Color32[] palette)
    {
        int width = sourceTexture.width;
        int height = sourceTexture.height;

        Color32[] sourcePixels = sourceTexture.GetPixels32();

        byte[,] matrix = new byte[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (height - 1 - y) * width + x;
                matrix[y, x] = FindNearestColorIndex(sourcePixels[index], palette);
            }
        }

        return matrix;
    }

    private static byte FindNearestColorIndex(Color32 color, Color32[] palette)
    {
        int nearestIndex = 0;
        int nearestDistance = int.MaxValue;

        for (int index = 0; index < palette.Length; index++)
        {
            int red = color.r - palette[index].r;
            int green = color.g - palette[index].g;
            int blue = color.b - palette[index].b;
            int alpha = color.a - palette[index].a;

            int distance = red * red + green * green + blue * blue + alpha * alpha;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = index;
            }
        }

        return (byte)nearestIndex;
    }

    private static void AddError(
        int x,
        int y,
        int width,
        int height,
        float[] redValues,
        float[] greenValues,
        float[] blueValues,
        float[] alphaValues,
        float redError,
        float greenError,
        float blueError,
        float alphaError,
        float factor)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        int index = y * width + x;

        redValues[index] += redError * factor;
        greenValues[index] += greenError * factor;
        blueValues[index] += blueError * factor;
        alphaValues[index] += alphaError * factor;
    }

    // テクスチャをImageにセットして大きさを自動調整
    public static void SetImage(Image image, Texture2D texture)
    {
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);

        image.sprite = sprite;
        image.SetNativeSize();

        RectTransform imageRectTransform = image.GetComponent<RectTransform>();
        RectTransform parentRectTransform = image.transform.parent.GetComponent<RectTransform>();

        float parentHeight = parentRectTransform.rect.height;
        float aspectRatio = (float)texture.width / texture.height;
        float previewWidth = parentHeight * aspectRatio;

        imageRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, parentHeight);
        imageRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, previewWidth);
    }

    // brightness : -100 ～ 100   // 0 が無補正 明度
    // saturation : -100 ～ 100   // 0 が無補正、-100 でグレースケール 彩度
    // contrast   : -100 ～ 100   // 0 が無補正 コントラスト
    // sharpness  : 0 ～ 5   // 0 が無補正 シャープネス
    public static Texture2D Retouch( Texture2D sourceTexture, float brightness, float saturation, float contrast, float sharpness)
    {
        brightness = Mathf.Clamp(brightness, -100f, 100f);
        saturation = Mathf.Clamp(saturation, -100f, 100f);
        contrast = Mathf.Clamp(contrast, -100f, 100f);

        Color32[] sourcePixels = sourceTexture.GetPixels32();
        Color32[] destinationPixels = new Color32[sourcePixels.Length];

        float brightnessOffset = brightness / 100f;
        float saturationFactor = 1f + saturation / 100f;

        float contrastFactor = (100f + contrast) / 100f;
        contrastFactor *= contrastFactor;

        for (int index = 0; index < sourcePixels.Length; index++)
        {
            Color32 sourceColor = sourcePixels[index];

            float red = sourceColor.r / 255f;
            float green = sourceColor.g / 255f;
            float blue = sourceColor.b / 255f;
            float alpha = sourceColor.a / 255f;

            // 明度
            red += brightnessOffset;
            green += brightnessOffset;
            blue += brightnessOffset;

            // コントラスト
            red = ((red - 0.5f) * contrastFactor) + 0.5f;
            green = ((green - 0.5f) * contrastFactor) + 0.5f;
            blue = ((blue - 0.5f) * contrastFactor) + 0.5f;

            // 彩度
            float luminance = red * 0.299f + green * 0.587f + blue * 0.114f;

            red = luminance + (red - luminance) * saturationFactor;
            green = luminance + (green - luminance) * saturationFactor;
            blue = luminance + (blue - luminance) * saturationFactor;

            destinationPixels[index] = new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(red * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(green * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(blue * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255)
            );
        }

        sharpness = Mathf.Clamp(sharpness, 0f, 5f);

        if (sharpness > 0f)
        {
            ApplyUnsharpMask(
                destinationPixels,
                sourceTexture.width,
                sourceTexture.height,
                sharpness
            );
        }

        Texture2D destinationTexture = new Texture2D(
            sourceTexture.width,
            sourceTexture.height,
            TextureFormat.RGBA32,
            false
        );

        destinationTexture.filterMode = sourceTexture.filterMode;
        destinationTexture.wrapMode = sourceTexture.wrapMode;
        destinationTexture.SetPixels32(destinationPixels);
        destinationTexture.Apply(false, false);

        return destinationTexture;
    }

    private static void ApplyUnsharpMask( Color32[] pixels, int width, int height, float sharpness )
    {
        Color32[] sourcePixels = (Color32[])pixels.Clone();
        Color32[] blurredPixels = new Color32[pixels.Length];

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                float red =
                    sourcePixels[(y - 1) * width + (x - 1)].r * 1 +
                    sourcePixels[(y - 1) * width + (x + 0)].r * 2 +
                    sourcePixels[(y - 1) * width + (x + 1)].r * 1 +
                    sourcePixels[(y + 0) * width + (x - 1)].r * 2 +
                    sourcePixels[(y + 0) * width + (x + 0)].r * 4 +
                    sourcePixels[(y + 0) * width + (x + 1)].r * 2 +
                    sourcePixels[(y + 1) * width + (x - 1)].r * 1 +
                    sourcePixels[(y + 1) * width + (x + 0)].r * 2 +
                    sourcePixels[(y + 1) * width + (x + 1)].r * 1;

                float green =
                    sourcePixels[(y - 1) * width + (x - 1)].g * 1 +
                    sourcePixels[(y - 1) * width + (x + 0)].g * 2 +
                    sourcePixels[(y - 1) * width + (x + 1)].g * 1 +
                    sourcePixels[(y + 0) * width + (x - 1)].g * 2 +
                    sourcePixels[(y + 0) * width + (x + 0)].g * 4 +
                    sourcePixels[(y + 0) * width + (x + 1)].g * 2 +
                    sourcePixels[(y + 1) * width + (x - 1)].g * 1 +
                    sourcePixels[(y + 1) * width + (x + 0)].g * 2 +
                    sourcePixels[(y + 1) * width + (x + 1)].g * 1;

                float blue =
                    sourcePixels[(y - 1) * width + (x - 1)].b * 1 +
                    sourcePixels[(y - 1) * width + (x + 0)].b * 2 +
                    sourcePixels[(y - 1) * width + (x + 1)].b * 1 +
                    sourcePixels[(y + 0) * width + (x - 1)].b * 2 +
                    sourcePixels[(y + 0) * width + (x + 0)].b * 4 +
                    sourcePixels[(y + 0) * width + (x + 1)].b * 2 +
                    sourcePixels[(y + 1) * width + (x - 1)].b * 1 +
                    sourcePixels[(y + 1) * width + (x + 0)].b * 2 +
                    sourcePixels[(y + 1) * width + (x + 1)].b * 1;

                blurredPixels[y * width + x] = new Color32(
                    (byte)(red / 16f),
                    (byte)(green / 16f),
                    (byte)(blue / 16f),
                    sourcePixels[y * width + x].a
                );
            }
        }

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int index = y * width + x;

                float red =
                    sourcePixels[index].r +
                    (sourcePixels[index].r - blurredPixels[index].r) * sharpness;

                float green =
                    sourcePixels[index].g +
                    (sourcePixels[index].g - blurredPixels[index].g) * sharpness;

                float blue =
                    sourcePixels[index].b +
                    (sourcePixels[index].b - blurredPixels[index].b) * sharpness;

                pixels[index] = new Color32(
                    (byte)Mathf.Clamp(Mathf.RoundToInt(red), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(green), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(blue), 0, 255),
                    sourcePixels[index].a
                );
            }
        }
    }
}
