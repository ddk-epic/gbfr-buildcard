namespace gbfr.qol.buildcard.Export;

// Resizes 8-bit RGB images: averages the source pixels a target pixel covers when shrinking, interpolates when enlarging.
public static class Resample
{
    public static byte[] Resize(byte[] rgb, int width, int height, int targetWidth, int targetHeight)
    {
        var rows = new float[height * targetWidth * 3];
        var (xStart, xWeights) = Weights(width, targetWidth);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < targetWidth; x++)
                for (int k = 0; k < xWeights[x].Length; k++)
                {
                    int src = (y * width + xStart[x] + k) * 3, dst = (y * targetWidth + x) * 3;
                    float w = xWeights[x][k];
                    rows[dst] += rgb[src] * w;
                    rows[dst + 1] += rgb[src + 1] * w;
                    rows[dst + 2] += rgb[src + 2] * w;
                }

        var output = new byte[targetWidth * targetHeight * 3];
        var (yStart, yWeights) = Weights(height, targetHeight);
        for (int y = 0; y < targetHeight; y++)
            for (int i = 0; i < targetWidth * 3; i++)
            {
                float sum = 0;
                for (int k = 0; k < yWeights[y].Length; k++)
                    sum += rows[(yStart[y] + k) * targetWidth * 3 + i] * yWeights[y][k];
                output[y * targetWidth * 3 + i] = (byte)Math.Clamp(sum + 0.5f, 0, 255);
            }
        return output;
    }

    // Each target pixel's first source pixel and the weights of the source pixels from it.
    private static (int[] Start, float[][] Weights) Weights(int size, int targetSize)
    {
        double scale = (double)size / targetSize;
        var start = new int[targetSize];
        var weights = new float[targetSize][];
        for (int i = 0; i < targetSize; i++)
        {
            if (scale >= 1)
            {
                double from = i * scale, to = (i + 1) * scale;
                int first = (int)from, last = Math.Min((int)Math.Ceiling(to), size);
                start[i] = first;
                weights[i] = new float[last - first];
                for (int j = first; j < last; j++)
                    weights[i][j - first] = (float)((Math.Min(to, j + 1) - Math.Max(from, j)) / scale);
            }
            else
            {
                double center = Math.Clamp((i + 0.5) * scale - 0.5, 0, size - 1);
                int first = Math.Min((int)center, size - 2);
                float t = (float)(center - first);
                start[i] = first;
                weights[i] = [1 - t, t];
            }
        }
        return (start, weights);
    }
}
