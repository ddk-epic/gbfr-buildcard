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
    private CardWriter? _cardWriter;
    private SaveCardButton? _saveCardButton;

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

        // Signatures exist for Endless Ragnarok only.
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

        _gameText = new GameText(_hooks);
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

        _cardWriter = new CardWriter(_gameText, _textWrap, _masterTraits, _masteries, _weaponArtHooks, _logger);
        _cardWriter.Init(scanManager, "granblue_fantasy_relink_er");
        _charaStatusHooks.Filled += _cardWriter.OnFilled;

        _gameText.Add(CardIds.SaveCardTextId, "Save Card");
        _saveCardButton = new SaveCardButton(_hooks);
        _saveCardButton.Init(scanManager, "granblue_fantasy_relink_er");
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