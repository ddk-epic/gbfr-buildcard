using gbfr.qol.buildcard.Core;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Interop.Native;

namespace gbfr.qol.buildcard.Sources;

// Reads a chara's mastery percentages with the game's function behind the Masteries screen.
public unsafe class MasteryReader : IFeature
{
    // MasteryPercent's categories
    public const int Offense = 0, Defense = 1, Collection = 2, Transcendence = 3;
    private const int Categories = 4;

    // SetMasteryPercents' mov rcx, [rip + disp32] loading the mastery manager
    private const int ManagerLoad = 0x30;

    private readonly GameApi _api;
    private nint* _manager;

    // MasteryPercent(mastery manager, category, chara key)
    private delegate* unmanaged<nint, int, uint, int> _percent;

    public MasteryReader(GameApi api)
    {
        _api = api;
    }

    public Capability[] Requires => [_api.MasteryPercent, _api.SetMasteryPercents];

    public void Start()
    {
        _percent = (delegate* unmanaged<nint, int, uint, int>)_api.MasteryPercent.Address;
        _manager = PeImage.RipGlobal((byte*)_api.SetMasteryPercents.Address + ManagerLoad, [0x48, 0x8B, 0x0D]);
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
