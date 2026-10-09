using System.Runtime.InteropServices;
using System.Text;

using NenTools.Reloaded.ScanManager.Interfaces;

namespace gbfr.qol.buildcard.Hooks;

// Sets UI texts through the game's Text setter and reads the loaded language's texts by id.
// Signature and parameters from Nenkai's gbfr.qol.detailedpercentages (MIT).
public unsafe class GameText
{
    private const int BufferSize = 0x400;
    public const uint EmptyIdHash = 0x887AE0B0;

    // TextComponentSetText's mov rdx, [rip + disp32] loading the text manager
    private const int ManagerLoad = 0x26;

    private readonly nint _buffer = Marshal.AllocHGlobal(BufferSize);
    private nint* _manager;

    // TextComponentSetText(text, string, text id hash, unknown)
    private delegate* unmanaged<nint, GameString*, uint, int, void> _setText;
    // TextLookup(text manager, string out, text id hash, sub-id hash)
    private delegate* unmanaged<nint, TextView*, uint, uint, void> _lookup;

    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan("TextComponentSetText", signatureGroup, address =>
        {
            _setText = (delegate* unmanaged<nint, GameString*, uint, int, void>)(nint)address;
            _manager = ManagerFrom((byte*)address + ManagerLoad);
        });
        scanManager.AddScan("TextLookup", signatureGroup, address =>
            _lookup = (delegate* unmanaged<nint, TextView*, uint, uint, void>)(nint)address);
    }

    // hash: the custom XXHash32 of a text id
    public void Set(nint text, string value, uint hash = EmptyIdHash)
    {
        if (_setText == null)
            return;
        int length = Encoding.UTF8.GetBytes(value, new Span<byte>((void*)_buffer, BufferSize));
        var str = new GameString { Ptr = _buffer, Length = (uint)length };
        _setText(text, &str, hash, -1);
    }

    // The text of a text id in the loaded language, null-terminated; Ptr 0 when unavailable
    public TextView Lookup(uint hash, uint subId = EmptyIdHash)
    {
        var view = new TextView();
        if (_lookup != null && _manager != null && *_manager != 0)
            _lookup(*_manager, &view, hash, subId);
        return view;
    }

    public string Find(uint hash)
    {
        var view = Lookup(hash);
        return view.Ptr == 0 || view.Length <= 0 ? "" : Encoding.UTF8.GetString((byte*)view.Ptr, (int)view.Length);
    }

    private static nint* ManagerFrom(byte* load)
    {
        if (load[0] != 0x48 || load[1] != 0x8B || load[2] != 0x15)
            return null;
        return (nint*)(load + 7 + *(int*)(load + 3));
    }

    public struct GameString
    {
        public nint Ptr;
        public uint Length;
    }

    public struct TextView
    {
        public nint Ptr;
        public long Length;
    }
}
