using System.Drawing;

using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using NenTools.Reloaded.ScanManager.Interfaces;
using gbfrelink.utility.manager.Interfaces;
using gbfr.qol.buildcard.Template;
using gbfr.qol.buildcard.Configuration;
using gbfr.qol.buildcard.Hooks;
using gbfr.qol.buildcard.Export;
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

    private GameText? _gameText;
    private TextWrap? _textWrap;
    private MasterTraits? _masterTraits;
    private Masteries? _masteries;
    private CharaStatusHooks? _charaStatusHooks;
    private WeaponArtHooks? _weaponArtHooks;
    private UiObjects? _uiObjects;
    private CardWriter? _cardWriter;
    private SaveCardButton? _saveCardButton;
    private StatusGuide? _statusGuide;
    private SavedNotice? _savedNotice;
    private SwapChainHooks? _swapChainHooks;
    private CardFile? _cardFile;
    private SteamScreenshots? _steamScreenshots;
    private CardExport? _cardExport;

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

        _gameText = new GameText();
        _gameText.Init(scanManager, "granblue_fantasy_relink_er");

        _textWrap = new TextWrap(_gameText);
        _textWrap.Init(scanManager, "granblue_fantasy_relink_er");

        _masterTraits = new MasterTraits();
        _masterTraits.Init(scanManager, "granblue_fantasy_relink_er");

        _masteries = new Masteries();
        _masteries.Init(scanManager, "granblue_fantasy_relink_er");

        _charaStatusHooks = new CharaStatusHooks(_hooks);
        _charaStatusHooks.Init(scanManager, "granblue_fantasy_relink_er");

        _weaponArtHooks = new WeaponArtHooks(_hooks, _logger);
        _weaponArtHooks.Init();

        _uiObjects = new UiObjects(_logger);
        _uiObjects.Init(scanManager, "granblue_fantasy_relink_er");

        _cardWriter = new CardWriter(_gameText, _textWrap, _masterTraits, _masteries, _weaponArtHooks, _uiObjects, _logger);
        _cardWriter.Init(scanManager, "granblue_fantasy_relink_er");
        _charaStatusHooks.Filled += _cardWriter.OnFilled;

        _saveCardButton = new SaveCardButton(_hooks);
        _saveCardButton.Init(scanManager, "granblue_fantasy_relink_er");
        _statusGuide = new StatusGuide(_hooks, _logger, _uiObjects);
        _statusGuide.Init();
        _savedNotice = new SavedNotice(_hooks, _logger, _uiObjects);
        _savedNotice.Init(scanManager, "granblue_fantasy_relink_er");
        _cardFile = new CardFile(_logger);
        _steamScreenshots = new SteamScreenshots(() => _configuration.SteamScreenshots, _logger);
        _cardFile.Saved += _savedNotice.OnSaved;
        _cardFile.Saved += _steamScreenshots.OnSaved;
        _cardExport = new CardExport(_saveCardButton, _statusGuide, new CardRedraw(_hooks), _savedNotice, _cardFile,
            _weaponArtHooks.CardShown, _cardWriter.CharaName, _logger);
        _saveCardButton.Tick += _savedNotice.OnTick;
        _saveCardButton.Tick += _cardExport.OnTick;
        _swapChainHooks = new SwapChainHooks(_hooks, _logger);
        _swapChainHooks.Presenting += _cardExport.OnPresenting;
        _swapChainHooks.Init();
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