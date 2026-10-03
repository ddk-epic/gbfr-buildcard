using System.Runtime.InteropServices;

using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Hooks.Definitions;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Hooks;

// Hooks the function that fills a CharaInfo from a character, the Character Details page among others. With dumping
// on, writes what it's filled from to the Dumps folder: the character struct, the equipment record it points to at
// 0x5E60 (sigils, skills) and the CharaInfo component.
public unsafe class CharaStatusHooks
{
    private const int CharaSize = 0x5F00;
    private const int EquipSize = 0x1000;
    private const int CharaInfoSize = 0x900;

    private readonly IReloadedHooks _hooks;
    private readonly string _folder;
    private StreamWriter? _index;

    public bool Dumping { get; set; }

    // Raised after the game fills a CharaInfo: the component and the character struct.
    public event Action<nint, nint>? Filled;

    private delegate void FillCharacterStatus(nint charaInfo, nint chara, int index);
    private IHook<FillCharacterStatus>? _fillHook;

    [DllImport("kernel32.dll")]
    private static extern bool ReadProcessMemory(nint process, nint address, byte[] buffer, nint size, out nint read);

    public CharaStatusHooks(IReloadedHooks hooks, string dumpFolder)
    {
        _hooks = hooks;
        _folder = dumpFolder;
    }

    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan(nameof(FillCharacterStatus), signatureGroup, address =>
            _fillHook = _hooks.CreateHook<FillCharacterStatus>(FillImpl, address).Activate());
    }

    private void FillImpl(nint charaInfo, nint chara, int index)
    {
        _fillHook!.OriginalFunction(charaInfo, chara, index);
        if (chara == 0)
            return;

        Filled?.Invoke(charaInfo, chara);
        if (Dumping)
            Dump(charaInfo, chara, index);
    }

    private void Dump(nint charaInfo, nint chara, int index)
    {
        if (_index is null)
        {
            Directory.CreateDirectory(_folder);
            _index = new StreamWriter(Path.Combine(_folder, "index.txt"), append: false) { AutoFlush = true };
        }

        uint key = *(uint*)(chara + 0x5EA8);
        nint equip = *(nint*)(chara + 0x5E60);
        DumpMemory(chara, CharaSize, $"chara_{key:X8}.bin");
        DumpMemory(charaInfo, CharaInfoSize, $"charainfo_{key:X8}.bin");
        if (equip != 0)
            DumpMemory(equip, EquipSize, $"equip_{key:X8}.bin");
        _index.WriteLine($"{DateTime.Now:HH:mm:ss.fff} key={key:X8} index={index} charaInfo={charaInfo:X} chara={chara:X} equip={equip:X}");
    }

    // Reads through ReadProcessMemory (-1: this process) so a dump running past the end of an allocation fails
    // instead of crashing.
    private void DumpMemory(nint address, int size, string name)
    {
        var buffer = new byte[size];
        ReadProcessMemory(-1, address, buffer, size, out nint read);
        File.WriteAllBytes(Path.Combine(_folder, name), buffer[..(int)read]);
    }
}
