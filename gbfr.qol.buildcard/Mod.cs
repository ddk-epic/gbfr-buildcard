using System.Drawing;

using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using NenTools.Reloaded.ScanManager.Interfaces;
using gbfrelink.utility.manager.Interfaces;
using gbfr.qol.buildcard.Template;
using gbfr.qol.buildcard.Configuration;
using gbfr.qol.buildcard.Hooks;
using gbfr.qol.buildcard.Export;
using gbfr.qol.buildcard.Host;
using gbfr.qol.buildcard.Model;
#if DEBUG
using System.Diagnostics;
#endif

namespace gbfr.qol.buildcard;

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
    private UiControllers? _uiControllers;
    private UiObjects? _uiObjects;
    private GameText? _gameText;
    private MasterTraits? _masterTraits;
    private Masteries? _masteries;
    private CharaStatusHooks? _charaStatusHooks;
    private CardSession? _cardSession;
    private CharacterDetailsMenu? _menu;
    private WeaponArtHooks? _weaponArtHooks;
    private CardView? _cardView;
    private SaveCardButton? _saveCardButton;
    private StatusGuide? _statusGuide;
    private StatusBackground? _statusBackground;
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
        _vtables = new Vtables(_hooks, _logger);
        _uiControllers = new UiControllers(_vtables, _logger);
        _uiControllers.Init();

        _uiObjects = new UiObjects(_logger, _vtables);
        _uiObjects.Init(scanManager, "granblue_fantasy_relink_er");

        _gameText = new GameText(_uiObjects, _vtables);
        _gameText.Init(scanManager, "granblue_fantasy_relink_er");

        _masterTraits = new MasterTraits();
        _masterTraits.Init(scanManager, "granblue_fantasy_relink_er");

        _masteries = new Masteries();
        _masteries.Init(scanManager, "granblue_fantasy_relink_er");

        _charaStatusHooks = new CharaStatusHooks(_hooks, _uiObjects, _logger);
        _charaStatusHooks.Init(scanManager, "granblue_fantasy_relink_er");
        _cardSession = new CardSession(_masterTraits, _masteries, _gameText, _logger);
        _charaStatusHooks.CharacterShown += _cardSession.OnCharacterShown;

        _menu = new CharacterDetailsMenu(_hooks, _logger);
        _menu.Init(scanManager, "granblue_fantasy_relink_er");

        _weaponArtHooks = new WeaponArtHooks(_vtables, _logger, _menu);
        _weaponArtHooks.Init();
        _cardSession.Shown += _weaponArtHooks.OnShown;

        var overMasteryBinder = new OverMasteryBinder(_gameText, _uiObjects, _vtables);
        overMasteryBinder.Init(scanManager, "granblue_fantasy_relink_er");
        var summonsBinder = new SummonsBinder(_uiObjects, _vtables);
        summonsBinder.Init(scanManager, "granblue_fantasy_relink_er");
        _cardView = new CardView(_uiObjects, _gameText, _cardSession, _logger,
            new GearBinder(_gameText),
            new MasterTraitsBinder(_gameText, _masterTraits, _uiObjects, () => _configuration.MasterTraitsPalette),
            new MasteriesBinder(_gameText),
            overMasteryBinder,
            summonsBinder);
        _cardSession.Shown += _cardView.OnShown;

        _saveCardButton = new SaveCardButton(_hooks, _logger);
        _saveCardButton.Init(scanManager, "granblue_fantasy_relink_er");
        _statusGuide = new StatusGuide(_vtables, _uiControllers, _uiObjects, _menu);
        _statusGuide.Init();
        _statusBackground = new StatusBackground(_vtables, _uiControllers, _logger, _uiObjects, _menu);
        _statusBackground.Init();
        _savedNotice = new SavedNotice(_vtables, _uiControllers, _uiObjects, _gameThread);
        _savedNotice.Init(scanManager, "granblue_fantasy_relink_er");
        _cardFile = new CardFile(_gameThread, _logger);
        _steamScreenshots = new SteamScreenshots(() => _configuration.SteamScreenshots, _logger);
        _cardFile.Saved += _savedNotice.OnSaved;
        _cardFile.Saved += _steamScreenshots.OnSaved;
        _renderThread = new RenderThread(_logger);
        _capture = new UiFrameCapture(new UiRedraw(_hooks), _renderThread, _logger);
        _exportController = new ExportController(_saveCardButton, _menu, _cardSession, _capture, _cardFile, _gameThread,
            _logger, _statusGuide, _savedNotice);
        _saveCardButton.ShortcutUpdated += _gameThread.Pump;
        _gameThread.Tick += _exportController.OnTick;
        _renderThread.Presenting += _ => _gameThread.OnPresenting();
        _renderThread.Presenting += _capture.OnPresenting;
        _swapChainHooks = new SwapChainHooks(_hooks, _logger);
        _swapChainHooks.Presenting += _renderThread.OnPresenting;
        _swapChainHooks.Init();

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