using NenTools.Reloaded.ScanManager.Interfaces;

namespace gbfr.qol.buildcard.Hooks;

// Reads a chara's mastery percentages with the game's function behind the Masteries screen.
public unsafe class Masteries
{
    // MasteryPercent's categories
    public const int Offense = 0, Defense = 1, Collection = 2, Transcendence = 3;
    private const int Categories = 4;

    // SetMasteryPercents' mov rcx, [rip + disp32] loading the mastery manager
    private const int ManagerLoad = 0x30;

    private nint* _manager;

    // MasteryPercent(mastery manager, category, chara key)
    private delegate* unmanaged<nint, int, uint, int> _percent;

    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan("MasteryPercent", signatureGroup, address =>
            _percent = (delegate* unmanaged<nint, int, uint, int>)(nint)address);
        scanManager.AddScan("SetMasteryPercents", signatureGroup, address =>
            _manager = PeImage.RipGlobal((byte*)address + ManagerLoad, [0x48, 0x8B, 0x0D]));
    }

    // The chara's percentage per category, as the Masteries screen shows it; null when unavailable
    public int[]? ReadPercents(uint charaKey)
    {
        if (_percent == null || _manager == null || *_manager == 0)
            return null;
        var percents = new int[Categories];
        for (int i = 0; i < Categories; i++)
            percents[i] = _percent(*_manager, i, charaKey);
        return percents;
    }
}
