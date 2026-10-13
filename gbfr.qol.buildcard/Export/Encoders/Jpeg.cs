using System.Text;

namespace gbfr.qol.buildcard.Export.Encoders;

// Writes 8-bit RGB images as baseline JFIF JPEG with 4:2:0 chroma and COM segments.
public static class Jpeg
{
    // bytes
    public const int MaxCommentLength = 65533;

    // ITU T.81 Annex K, natural order
    private static readonly byte[] LumaQuant =
    [
        16, 11, 10, 16, 24, 40, 51, 61, 12, 12, 14, 19, 26, 58, 60, 55,
        14, 13, 16, 24, 40, 57, 69, 56, 14, 17, 22, 29, 51, 87, 80, 62,
        18, 22, 37, 56, 68, 109, 103, 77, 24, 35, 55, 64, 81, 104, 113, 92,
        49, 64, 78, 87, 103, 121, 120, 101, 72, 92, 95, 98, 112, 100, 103, 99,
    ];

    private static readonly byte[] ChromaQuant =
    [
        17, 18, 24, 47, 99, 99, 99, 99, 18, 21, 26, 66, 99, 99, 99, 99,
        24, 26, 56, 99, 99, 99, 99, 99, 47, 66, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
    ];

    // natural index of each zigzag position
    private static readonly byte[] Zigzag =
    [
        0, 1, 8, 16, 9, 2, 3, 10, 17, 24, 32, 25, 18, 11, 4, 5,
        12, 19, 26, 33, 40, 48, 41, 34, 27, 20, 13, 6, 7, 14, 21, 28,
        35, 42, 49, 56, 57, 50, 43, 36, 29, 22, 15, 23, 30, 37, 44, 51,
        58, 59, 52, 45, 38, 31, 39, 46, 53, 60, 61, 54, 47, 55, 62, 63,
    ];

    private static readonly HuffmanTable LumaDc = new(0x00,
        [0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0],
        [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11]);

    private static readonly HuffmanTable ChromaDc = new(0x01,
        [0, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0],
        [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11]);

    private static readonly HuffmanTable LumaAc = new(0x10,
        [0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 0x7D],
        [
            0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12, 0x21, 0x31, 0x41, 0x06, 0x13, 0x51, 0x61, 0x07,
            0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xA1, 0x08, 0x23, 0x42, 0xB1, 0xC1, 0x15, 0x52, 0xD1, 0xF0,
            0x24, 0x33, 0x62, 0x72, 0x82, 0x09, 0x0A, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x25, 0x26, 0x27, 0x28,
            0x29, 0x2A, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49,
            0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69,
            0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
            0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7,
            0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3, 0xC4, 0xC5,
            0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xE1, 0xE2,
            0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8,
            0xF9, 0xFA,
        ]);

    private static readonly HuffmanTable ChromaAc = new(0x11,
        [0, 2, 1, 2, 4, 4, 3, 4, 7, 5, 4, 4, 0, 1, 2, 0x77],
        [
            0x00, 0x01, 0x02, 0x03, 0x11, 0x04, 0x05, 0x21, 0x31, 0x06, 0x12, 0x41, 0x51, 0x07, 0x61, 0x71,
            0x13, 0x22, 0x32, 0x81, 0x08, 0x14, 0x42, 0x91, 0xA1, 0xB1, 0xC1, 0x09, 0x23, 0x33, 0x52, 0xF0,
            0x15, 0x62, 0x72, 0xD1, 0x0A, 0x16, 0x24, 0x34, 0xE1, 0x25, 0xF1, 0x17, 0x18, 0x19, 0x1A, 0x26,
            0x27, 0x28, 0x29, 0x2A, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
            0x49, 0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
            0x69, 0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
            0x88, 0x89, 0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5,
            0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3,
            0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA,
            0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8,
            0xF9, 0xFA,
        ]);

    // DCT basis: row u, column x
    private static readonly float[] Cosines = MakeCosines();

    // quality: libjpeg's 1 to 100; texts: each COM segment's keyword and Latin-1 text
    public static void Write(string path, byte[] rgb, int width, int height, int quality, params (string Keyword, string Text)[] texts)
    {
        byte[][] comments = texts.Select(t => Comment(t.Keyword, t.Text)).ToArray();
        byte[] luma = ScaleQuant(LumaQuant, quality);
        byte[] chroma = ScaleQuant(ChromaQuant, quality);

        using var file = new BufferedStream(File.Create(path), 1 << 16);
        file.Write([0xFF, 0xD8]);
        WriteSegment(file, 0xE0, [.. "JFIF"u8, 0, 1, 1, 0, 0, 1, 0, 1, 0, 0]);
        foreach (byte[] comment in comments)
            WriteSegment(file, 0xFE, comment);
        WriteSegment(file, 0xDB, [0x00, .. Zigzag.Select(i => luma[i])]);
        WriteSegment(file, 0xDB, [0x01, .. Zigzag.Select(i => chroma[i])]);
        WriteSegment(file, 0xC0, [8, (byte)(height >> 8), (byte)height, (byte)(width >> 8), (byte)width, 3, 1, 0x22, 0, 2, 0x11, 1, 3, 0x11, 1]);
        foreach (var table in new[] { LumaDc, LumaAc, ChromaDc, ChromaAc })
            WriteSegment(file, 0xC4, [table.Class, .. table.Counts, .. table.Symbols]);
        WriteSegment(file, 0xDA, [3, 1, 0x00, 2, 0x11, 3, 0x11, 0, 63, 0]);
        WriteScan(file, rgb, width, height, luma, chroma);
        file.Write([0xFF, 0xD9]);
    }

    // libjpeg's jpeg_quality_scaling
    private static byte[] ScaleQuant(byte[] table, int quality)
    {
        quality = Math.Clamp(quality, 1, 100);
        int scale = quality < 50 ? 5000 / quality : 200 - quality * 2;
        return table.Select(q => (byte)Math.Clamp((q * scale + 50) / 100, 1, 255)).ToArray();
    }

    // keyword, a 0 separator and the text, both Latin-1
    private static byte[] Comment(string keyword, string text)
    {
        if (keyword.Length == 0 || (keyword + text).Any(c => c is '\0' or > '\u00FF') || keyword.Length + 1 + text.Length > MaxCommentLength)
            throw new ArgumentException($"COM {keyword}: the keyword must not be empty, both Latin-1 without 0 and {MaxCommentLength} bytes at most");
        return [.. Encoding.Latin1.GetBytes(keyword), 0, .. Encoding.Latin1.GetBytes(text)];
    }

    private static void WriteSegment(Stream stream, byte marker, byte[] data)
    {
        int length = data.Length + 2;
        stream.Write([0xFF, marker, (byte)(length >> 8), (byte)length]);
        stream.Write(data);
    }

    // 16x16 MCUs of four Y, one Cb and one Cr block
    private static void WriteScan(Stream stream, byte[] rgb, int width, int height, byte[] luma, byte[] chroma)
    {
        var bits = new BitWriter(stream);
        var y = new float[4][];
        for (int i = 0; i < 4; i++)
            y[i] = new float[64];
        var cb = new float[64];
        var cr = new float[64];
        var scratch = new float[64];
        var coefficients = new int[64];
        int dcY = 0, dcCb = 0, dcCr = 0;

        for (int top = 0; top < height; top += 16)
        {
            for (int left = 0; left < width; left += 16)
            {
                Array.Clear(cb);
                Array.Clear(cr);
                for (int row = 0; row < 16; row++)
                {
                    int offset = Math.Min(top + row, height - 1) * width;
                    for (int column = 0; column < 16; column++)
                    {
                        int pixel = (offset + Math.Min(left + column, width - 1)) * 3;
                        float r = rgb[pixel], g = rgb[pixel + 1], b = rgb[pixel + 2];
                        y[(row >> 3) * 2 + (column >> 3)][(row & 7) * 8 + (column & 7)] = 0.299f * r + 0.587f * g + 0.114f * b - 128;
                        int at = (row >> 1) * 8 + (column >> 1);
                        cb[at] += -0.168736f * r - 0.331264f * g + 0.5f * b;
                        cr[at] += 0.5f * r - 0.418688f * g - 0.081312f * b;
                    }
                }
                for (int i = 0; i < 64; i++)
                {
                    cb[i] *= 0.25f;
                    cr[i] *= 0.25f;
                }

                foreach (float[] block in y)
                    dcY = EncodeBlock(bits, block, scratch, coefficients, luma, dcY, LumaDc, LumaAc);
                dcCb = EncodeBlock(bits, cb, scratch, coefficients, chroma, dcCb, ChromaDc, ChromaAc);
                dcCr = EncodeBlock(bits, cr, scratch, coefficients, chroma, dcCr, ChromaDc, ChromaAc);
            }
        }
        bits.Flush();
    }

    // Codes one block; returns its DC.
    private static int EncodeBlock(BitWriter bits, float[] block, float[] scratch, int[] coefficients, byte[] quant,
        int previousDc, HuffmanTable dc, HuffmanTable ac)
    {
        Dct(block, scratch);
        for (int i = 0; i < 64; i++)
        {
            int natural = Zigzag[i];
            coefficients[i] = (int)MathF.Round(block[natural] / quant[natural], MidpointRounding.AwayFromZero);
        }

        int difference = coefficients[0] - previousDc;
        int size = Magnitude(difference);
        bits.Write(dc.Codes[size], dc.Lengths[size]);
        bits.Write(Amplitude(difference, size), size);

        int zeros = 0;
        for (int i = 1; i < 64; i++)
        {
            int value = coefficients[i];
            if (value == 0)
            {
                zeros++;
                continue;
            }
            for (; zeros >= 16; zeros -= 16)
                bits.Write(ac.Codes[0xF0], ac.Lengths[0xF0]);
            size = Magnitude(value);
            int symbol = zeros << 4 | size;
            bits.Write(ac.Codes[symbol], ac.Lengths[symbol]);
            bits.Write(Amplitude(value, size), size);
            zeros = 0;
        }
        if (zeros > 0)
            bits.Write(ac.Codes[0x00], ac.Lengths[0x00]);
        return coefficients[0];
    }

    // In place: 8x8 DCT-II
    private static void Dct(float[] block, float[] scratch)
    {
        for (int row = 0; row < 8; row++)
            for (int u = 0; u < 8; u++)
            {
                float sum = 0;
                for (int x = 0; x < 8; x++)
                    sum += Cosines[u * 8 + x] * block[row * 8 + x];
                scratch[row * 8 + u] = sum;
            }
        for (int column = 0; column < 8; column++)
            for (int v = 0; v < 8; v++)
            {
                float sum = 0;
                for (int y = 0; y < 8; y++)
                    sum += Cosines[v * 8 + y] * scratch[y * 8 + column];
                block[v * 8 + column] = sum;
            }
    }

    private static float[] MakeCosines()
    {
        var cosines = new float[64];
        for (int u = 0; u < 8; u++)
            for (int x = 0; x < 8; x++)
                cosines[u * 8 + x] = (float)((u == 0 ? Math.Sqrt(0.125) : 0.5) * Math.Cos((2 * x + 1) * u * Math.PI / 16));
        return cosines;
    }

    // bit length of the absolute value
    private static int Magnitude(int value) => 32 - int.LeadingZeroCount(Math.Abs(value));

    // low size bits, negative values as one's complement
    private static int Amplitude(int value, int size) => (value < 0 ? value - 1 : value) & ((1 << size) - 1);

    // A DHT table with each symbol's code and length.
    private sealed class HuffmanTable
    {
        public readonly byte Class;
        public readonly byte[] Counts;
        public readonly byte[] Symbols;
        public readonly int[] Codes = new int[256];
        public readonly int[] Lengths = new int[256];

        // tableClass: class high nibble, id low nibble; counts: symbols per code length
        public HuffmanTable(byte tableClass, byte[] counts, byte[] symbols)
        {
            Class = tableClass;
            Counts = counts;
            Symbols = symbols;
            int code = 0, k = 0;
            for (int length = 1; length <= 16; length++, code <<= 1)
                for (int i = 0; i < counts[length - 1]; i++, code++, k++)
                {
                    Codes[symbols[k]] = code;
                    Lengths[symbols[k]] = length;
                }
        }
    }

    // Entropy-coded bits with a 0 stuffed after each 0xFF byte.
    private sealed class BitWriter(Stream stream)
    {
        private uint _buffer;
        private int _count;

        public void Write(int value, int length)
        {
            _buffer = _buffer << length | (uint)value;
            _count += length;
            while (_count >= 8)
            {
                _count -= 8;
                byte b = (byte)(_buffer >> _count);
                stream.WriteByte(b);
                if (b == 0xFF)
                    stream.WriteByte(0);
            }
        }

        // pads the last byte with 1 bits
        public void Flush()
        {
            if (_count > 0)
                Write((1 << (8 - _count)) - 1, 8 - _count);
        }
    }
}
