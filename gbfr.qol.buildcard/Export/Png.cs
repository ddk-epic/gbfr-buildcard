using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace gbfr.qol.buildcard.Export;

// Writes 8-bit RGB images as PNG.
public static class Png
{
    private static readonly uint[] CrcTable = MakeCrcTable();

    public static void Write(string path, byte[] rgb, int width, int height)
    {
        using var file = File.Create(path);
        file.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0), width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8;
        header[9] = 2;
        WriteChunk(file, "IHDR", header);

        using var data = new MemoryStream();
        using (var zlib = new ZLibStream(data, CompressionLevel.Optimal, leaveOpen: true))
        {
            int stride = width * 3;
            var row = new byte[stride + 1];
            for (int y = 0; y < height; y++)
            {
                FilterRow(rgb, y * stride, y > 0 ? (y - 1) * stride : -1, stride, row);
                zlib.Write(row);
            }
        }
        WriteChunk(file, "IDAT", data.ToArray());
        WriteChunk(file, "IEND", []);
    }

    // PNG filter type 4 (Paeth): each byte minus its predicted neighbour; above is -1 on the first row
    private static void FilterRow(byte[] rgb, int offset, int above, int stride, byte[] row)
    {
        row[0] = 4;
        for (int i = 0; i < stride; i++)
        {
            int a = i >= 3 ? rgb[offset + i - 3] : 0;
            int b = above >= 0 ? rgb[above + i] : 0;
            int c = i >= 3 && above >= 0 ? rgb[above + i - 3] : 0;
            int p = a + b - c;
            int pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
            int predictor = pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
            row[i + 1] = (byte)(rgb[offset + i] - predictor);
        }
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(number, data.Length);
        stream.Write(number);
        byte[] name = Encoding.ASCII.GetBytes(type);
        stream.Write(name);
        stream.Write(data);
        uint crc = Crc(Crc(0xFFFFFFFF, name), data) ^ 0xFFFFFFFF;
        BinaryPrimitives.WriteUInt32BigEndian(number, crc);
        stream.Write(number);
    }

    private static uint Crc(uint crc, byte[] data)
    {
        foreach (byte b in data)
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc;
    }

    private static uint[] MakeCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            table[n] = c;
        }
        return table;
    }
}
