using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

using gbfr.qol.buildcard.Export;
using gbfr.qol.buildcard.Hooks;
using gbfr.qol.buildcard.Model;

namespace gbfr.qol.buildcard.Tests;

public class CardFileTests
{
    private static BuildCard Card() => new(1, new WeaponPart(2, GameText.EmptyIdHash, false),
        new TraitBoard(false, [1, 0, 0, 0], [10, 10, 10, 20],
            [new StyleBoard(0x20, "猛攻：攻撃力アップ", "猛攻", 1, [[new TraitCell(110, true)], [], [], []])]),
        new MasteryPercents(150, 100, 50, 0), [new OverMasteryLine(3, 1, 0, 2.5f), null, null, null], [7, null, null, null])
    {
        CharaName = "グラン",
    };

    [Fact]
    public void JsonIsAsciiAndReadsBack()
    {
        string json = JsonSerializer.Serialize(Card(), BuildCardJson.Default.BuildCard);

        Assert.All(json, c => Assert.True(c < 0x80));
        var card = JsonSerializer.Deserialize(json, BuildCardJson.Default.BuildCard)!;
        Assert.Equal("グラン", card.CharaName);
        Assert.Equal("猛攻", card.Traits.Styles[0].Name);
        Assert.Equal(new TraitCell(110, true), card.Traits.Styles[0].Cells[0][0]);
        Assert.Equal(new OverMasteryLine(3, 1, 0, 2.5f), card.OverMastery[0]);
        Assert.Equal([7u, null, null, null], card.Summons);
    }

    [Fact]
    public void PngHoldsTheTextAfterTheHeader()
    {
        string path = Path.GetTempFileName();
        try
        {
            Png.Write(path, new byte[2 * 3], 2, 1, ("BuildCard", "{\"a\":1}"));

            var chunks = Chunks(File.ReadAllBytes(path));
            Assert.Equal(["IHDR", "tEXt", "IDAT", "IEND"], chunks.Select(c => c.Type));
            Assert.Equal("BuildCard\0{\"a\":1}", Encoding.Latin1.GetString(chunks[1].Data));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void PngRejectsTextOutsideLatin1()
    {
        string path = Path.GetTempFileName();
        try
        {
            Assert.Throws<ArgumentException>(() => Png.Write(path, new byte[3], 1, 1, ("BuildCard", "猛攻")));
        }
        finally
        {
            File.Delete(path);
        }
    }

    // Each chunk's type and data after the signature, checking each CRC
    private static List<(string Type, byte[] Data)> Chunks(byte[] png)
    {
        var chunks = new List<(string, byte[])>();
        for (int at = 8; at < png.Length;)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(at));
            var typeAndData = png.AsSpan(at + 4, 4 + length);
            Assert.Equal(Crc(typeAndData), BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(at + 8 + length)));
            chunks.Add((Encoding.ASCII.GetString(typeAndData[..4]), typeAndData[4..].ToArray()));
            at += 12 + length;
        }
        return chunks;
    }

    private static uint Crc(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in data)
        {
            crc ^= b;
            for (int k = 0; k < 8; k++)
                crc = (crc & 1) != 0 ? 0xEDB88320 ^ (crc >> 1) : crc >> 1;
        }
        return crc ^ 0xFFFFFFFF;
    }
}
