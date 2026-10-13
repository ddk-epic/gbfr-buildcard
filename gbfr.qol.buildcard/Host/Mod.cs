using System.Drawing;

using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using NenTools.Reloaded.ScanManager.Interfaces;
using gbfrelink.utility.manager.Interfaces;
using gbfr.qol.buildcard.Template;
using gbfr.qol.buildcard.Configuration;
using gbfr.qol.buildcard.Card;
using gbfr.qol.buildcard.Card.Binders;
using gbfr.qol.buildcard.Export;
using gbfr.qol.buildcard.Export.Overlays;
using gbfr.qol.buildcard.Export.Sinks;
using gbfr.qol.buildcard.Game;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Interop.Native;
using gbfr.qol.buildcard.Model;
using gbfr.qol.buildcard.Sources;
#if DEBUG
using System.Diagnostics;
#endif

namespace gbfr.qol.buildcard.Host;

public class Mod : ModBase
{
    private readonly IModLoader _modLoader;

    private readonly IReloadedHooks? _hooks;

    private readonly ILogger _logger;

    private readonly IMod _owner;

    private Config _configuration;

    private readonly IModConfig _modConfig;

    private GameThread? _gameThread;
    private Vtables? _vtables;
    private GameApi? _api;
    private Features? _features;
    private ControllerLifetimes? _lifetimes;
    private UiTree? _uiTree;
    private TextTable? _textTable;
    private TextService? _textService;
    private TraitTables? _traitTables;
    private MasteryReader? _masteryReader;
    private OverMasteryParams? _overMasteryParams;
    private CharaStatusHooks? _charaStatusHooks;
    private CardSession? _cardSession;
    private MenuState? _menu;
    private WeaponArt? _weaponArt;
    private CardView? _cardView;
    private Input? _input;
    private SaveCardFooter? _saveCardFooter;
    private StatusGuide? _statusGuide;
    private StatusBackground? _statusBackground;
    private NoticeSink? _noticeSink;
    private SavedNotice? _savedNotice;
    private SwapChainHooks? _swapChainHooks;
    private RenderThread? _renderThread;
    private UiFrameCapture? _capture;
    private CardFile? _cardFile;
    private SteamScreenshots? _steamScreenshots;
    private ExportController? _exportController;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

#if DEBUG
        Debugger.Launch();
#endif

        if (!_modLoader.GetController<IScanManager>().TryGetTarget(out IScanManager? scanManager))
        {
            _logger.WriteLine($"[{_modConfig.ModId}] ERROR: Unable to get IScanManager", Color.Red);
            return;
        }

        if (!_modLoader.GetController<IUserDefinedParams>().TryGetTarget(out IUserDefinedParams? userDefinedParams))
        {
            _logger.WriteLine($"[{_modConfig.ModId}] ERROR: Unable to get IUserDefinedParams", Color.Red);
            return;
        }

        if (userDefinedParams.GetGameVersion() != GameVersion.RelinkEndlessRagnarok)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] ERROR: Unsupported game version {userDefinedParams.GetGameVersion()}", Color.Red);
            return;
        }

        if (_hooks is null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] ERROR: Unable to get IReloadedHooks", Color.Red);
            return;
        }

        string modFolder = _modLoader.GetDirectoryForModId(_modConfig.ModId);
        scanManager.InitializeScans(Path.Combine(modFolder, "Signatures"), _modConfig.ModId);

        _gameThread = new GameThread(_logger);
        _vtables = new Vtables(_logger);
        _api = new GameApi(scanManager, _vtables);
        _features = new Features(_logger);

        _lifetimes = new ControllerLifetimes(_hooks, _api, _logger);
        _uiTree = new UiTree(_logger, _api);
        _textTable = new TextTable(_api);
        _textService = new TextService(_uiTree, _textTable, _api);
        _traitTables = new TraitTables(_api);
        _masteryReader = new MasteryReader(_api);
        _overMasteryParams = new OverMasteryParams(_api);
        _menu = new MenuState(_hooks, _api, _logger);

        _charaStatusHooks = new CharaStatusHooks(_hooks, _uiTree, _textService, _api, _logger);
        _cardSession = new CardSession(_traitTables, _masteryReader, _overMasteryParams, _textTable, _logger);
        _charaStatusHooks.CharacterShown += _cardSession.OnCharacterShown;

        _weaponArt = new WeaponArt(_hooks, _api, _logger, _menu);
        _charaStatusHooks.Filling += _weaponArt.OnFilling;
        _charaStatusHooks.Filled += _weaponArt.OnFilled;
        _cardSession.Shown += _weaponArt.OnShown;

        var overMasteryBinder = new OverMasteryBinder(_textService, _textTable, _uiTree, _api);
        var summonsBinder = new SummonsBinder(_uiTree, _api);
        _cardView = new CardView(_uiTree, _cardSession, _lifetimes, _api, _logger,
            new GearBinder(_textService, _textTable),
            new MasterTraitsBinder(_textService, _traitTables, _uiTree, () => _configuration.MasterTraitsPalette),
            new MasteriesBinder(_textService, _textTable),
            overMasteryBinder,
            summonsBinder);
        _statusBackground = new StatusBackground(_hooks, _api, _lifetimes, _logger, _uiTree, _menu);

        _input = new Input(_hooks, _api, _logger);
        _saveCardFooter = new SaveCardFooter(_hooks, _api);
        _statusGuide = new StatusGuide(_hooks, _api, _lifetimes, _uiTree, _menu);
        _noticeSink = new NoticeSink(_api, _gameThread);
        _savedNotice = new SavedNotice(_hooks, _api, _lifetimes, _uiTree, _noticeSink);
        _cardFile = new CardFile(() => _configuration.CardFormat, _gameThread, _logger);
        _steamScreenshots = new SteamScreenshots(() => _configuration.SteamScreenshots, _logger);
        _cardFile.Saved += _noticeSink.OnSaved;
        _cardFile.Saved += _steamScreenshots.OnSaved;
        _swapChainHooks = new SwapChainHooks(_hooks, _logger);
        _renderThread = new RenderThread(_logger, _swapChainHooks);
        _capture = new UiFrameCapture(new UiRedraw(_hooks), _renderThread, _logger);
        _exportController = new ExportController(_input, _menu, _cardSession, _capture, _cardFile, _gameThread,
            _swapChainHooks, _logger, _statusGuide, _savedNotice);
        _input.ShortcutUpdated += _gameThread.Pump;
        _renderThread.Presenting += _ => _gameThread.OnPresenting();
        _renderThread.Presenting += _capture.OnPresenting;
        _swapChainHooks.Presenting += _renderThread.OnPresenting;

        _features.Add(_lifetimes, _uiTree, _textTable, _textService, _traitTables, _masteryReader, _overMasteryParams, _menu,
            _charaStatusHooks, _weaponArt, overMasteryBinder, summonsBinder, _cardView, _statusBackground, _input,
            _saveCardFooter, _statusGuide, _noticeSink, _savedNotice, _swapChainHooks, _exportController);
        _api.ScansFound += _features.Start;
        _modLoader.OnModLoaderInitialized += _features.Start;
        if (_api.AllScansFound)
            _features.Start();

        _logger.WriteLine($"[{_modConfig.ModId}] Loaded");
    }

    #region Standard Overrides
    public override void ConfigurationUpdated(Config configuration)
    {
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
    }
    #endregion

    #region For Exports, Serialization etc.
#pragma warning disable CS8618
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}