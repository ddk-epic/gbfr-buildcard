using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Game;
using gbfr.qol.buildcard.Host;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Interop.Native;
using gbfr.qol.buildcard.Model;
using gbfr.qol.buildcard.Sources;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Card;

// Fills the card's weapon with the equip menu's skin view and loads its art through ui::icon::LoadWeaponParty.
public unsafe class WeaponArt : IFeature
{
    private const int WantedMax = 6;

    private readonly IReloadedHooks _hooks;
    private readonly GameApi _api;
    private readonly ILogger _logger;
    private readonly MenuState _menu;
    private SkinViewOwner** _owner;
    private SkinViewOwner* _swapped;
    private SkinView _saved;
    private uint _key = TextTable.EmptyIdHash;
    private bool _effectOff;

    private delegate byte IsOpen(nint loader);
    private delegate void Collect(nint loader);
    private IHook<IsOpen>? _isOpenHook;
    private IHook<Collect>? _collectHook;

    public WeaponArt(IReloadedHooks hooks, GameApi api, ILogger logger, MenuState menu)
    {
        _hooks = hooks;
        _api = api;
        _logger = logger;
        _menu = menu;
    }

    public Capability[] Requires =>
        [_api.FillCharacterStatus, _api.EnableSkinViewApply, _api.LoadWeaponPartyIsOpen, _api.LoadWeaponPartyCollect];

    // Reads the skin view's address from the mov rax, [rip + disp32] at EnableSkinView vfunc 9.
    public void Start()
    {
        _owner = (SkinViewOwner**)PeImage.RipGlobal((byte*)_api.EnableSkinViewApply.Address, [0x48, 0x8B, 0x05]);
        if (_owner == null)
        {
            _logger.WriteLine("[gbfr.qol.buildcard] Skin view not found", Color.Red);
            return;
        }
        _isOpenHook = _hooks.CreateHook<IsOpen>(IsOpenImpl, _api.LoadWeaponPartyIsOpen.Address).Activate();
        _collectHook = _hooks.CreateHook<Collect>(CollectImpl, _api.LoadWeaponPartyCollect.Address).Activate();
    }

    // Sets the weapon whose art is loaded: the skin's when one is set.
    public void OnShown(CharaInfo charaInfo, BuildCard card)
    {
        _key = card.Weapon.SkinKey != TextTable.EmptyIdHash ? card.Weapon.SkinKey : card.Weapon.Key;
        _effectOff = card.Weapon.EffectOff;
    }

    // Fills the card with the equip menu's skin view
    public void OnFilling()
    {
        if (_owner == null || *_owner == null)
            return;
        _swapped = *_owner;
        ref var view = ref _swapped->View;
        _saved = view;
        view.WeaponSkin = 1;
        view.ForceSkinTag = 0;
        view.WeaponEffectOff = 1;
    }

    public void OnFilled()
    {
        if (_swapped == null)
            return;
        _swapped->View = _saved;
        _swapped = null;
    }

    // Open while the party menu is open, or while the Character Details menu is open and a card weapon is set.
    private byte IsOpenImpl(nint loader)
    {
        return _isOpenHook!.OriginalFunction(loader) != 0 || CardShown() ? (byte)1 : (byte)0;
    }

    // Collects the party's weapons while the party menu is open, then adds the card's weapon.
    private void CollectImpl(nint loader)
    {
        if (_isOpenHook!.OriginalFunction(loader) != 0)
            _collectHook!.OriginalFunction(loader);
        if (!CardShown())
            return;

        ref var list = ref *(Loader*)loader;
        long count = list.Count;
        if (count is < 0 or >= WantedMax)
            return;
        byte effectOff = _effectOff ? (byte)1 : (byte)0;
        for (int i = 0; i < count; i++)
        {
            if (list.Wanted[i].Key == _key && list.Wanted[i].EffectOff == effectOff)
                return;
        }
        ref var added = ref list.Wanted[(int)count];
        added.Key = _key;
        added.EffectOff = effectOff;
        list.Count = count + 1;
    }

    // Whether the Character Details menu is open with a card filled.
    private bool CardShown() => _key != TextTable.EmptyIdHash && _menu.IsOpen;

    // ui::icon::LoadWeaponParty
    [StructLayout(LayoutKind.Explicit)]
    private struct Loader
    {
        [FieldOffset(0x40)] public long Count;
        [FieldOffset(0x48)] public WantedArray Wanted;
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    private struct WantedEntry
    {
        [FieldOffset(0x0)] public uint Key;
        [FieldOffset(0x4)] public byte EffectOff;
    }

    [InlineArray(WantedMax)]
    private struct WantedArray
    {
        private WantedEntry _element;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct SkinViewOwner
    {
        [FieldOffset(0x1112)] public SkinView View;
    }

    // the bytes ui::fsm::action::EnableSkinView sets
    [StructLayout(LayoutKind.Sequential)]
    private struct SkinView
    {
        public byte ForceSkinTag;
        public byte WeaponSkin;
        public byte WeaponEffectOff;
    }
}
