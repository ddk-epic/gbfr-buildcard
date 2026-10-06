using System.Runtime.InteropServices;
using System.Text;

using NenTools.Reloaded.ScanManager.Interfaces;

namespace gbfr.qol.buildcard.Hooks;

// Sets UI texts through the game's Text setter.
// Signature and parameters from Nenkai's gbfr.qol.detailedpercentages (MIT).
public unsafe class GameText
{
    private const int BufferSize = 0x400;
    public const uint NoHash = 0x887AE0B0;  // hash of an empty text id

    private readonly nint _buffer = Marshal.AllocHGlobal(BufferSize);

    // TextComponentSetText(text, string, text id hash, unknown)
    private delegate* unmanaged<nint, GameString*, uint, int, void> _setText;

    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan("TextComponentSetText", signatureGroup, address =>
            _setText = (delegate* unmanaged<nint, GameString*, uint, int, void>)(nint)address);
    }

    // hash: the custom XXHash32 of a text id
    public void Set(nint text, string value, uint hash = NoHash)
    {
        if (_setText == null)
            return;
        int length = Encoding.UTF8.GetBytes(value, new Span<byte>((void*)_buffer, BufferSize));
        var str = new GameString { Ptr = _buffer, Length = (uint)length };
        _setText(text, &str, hash, -1);
    }

    public struct GameString
    {
        public nint Ptr;
        public uint Length;
    }
}
