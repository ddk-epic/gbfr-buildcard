using System.Drawing;

using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using NenTools.Reloaded.ScanManager.Interfaces;
using gbfrelink.utility.manager.Interfaces;
using gbfr.qol.buildcard.Template;
using gbfr.qol.buildcard.Configuration;
using gbfr.qol.buildcard.Hooks;
using gbfr.qol.buildcard.Hooks.Reflection;
#if DEBUG
using System.Diagnostics;
#endif

namespace gbfr.qol.buildcard;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    /// <summary>
    /// Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    /// Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    /// Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    /// <summary>
    /// Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;

    private FileLogger? _fileLogger;
    private TextHooks? _textHooks;
    private CharaStatusHooks? _charaStatusHooks;
    private CardWriter? _cardWriter;
    private ReflectionHooks? _reflectionHooks;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

#if DEBUG
        // Attaches debugger in debug mode; ignored in release.
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

        _fileLogger = new FileLogger(_hooks, Path.Combine(modFolder, "FileLog.txt")) { Enabled = _configuration.LogFiles };
        _fileLogger.Init(scanManager, "granblue_fantasy_relink_er");

        _textHooks = new TextHooks(_hooks, Path.Combine(modFolder, "TextLog.txt")) { Logging = _configuration.LogText };
        _textHooks.Init(scanManager, "granblue_fantasy_relink_er");

        _charaStatusHooks = new CharaStatusHooks(_hooks, Path.Combine(modFolder, "Dumps")) { Dumping = _configuration.DumpBuild };
        _charaStatusHooks.Init(scanManager, "granblue_fantasy_relink_er");

        _cardWriter = new CardWriter(_textHooks, _logger);
        _cardWriter.Init(scanManager, "granblue_fantasy_relink_er");
        _charaStatusHooks.Filled += _cardWriter.OnFilled;

        _reflectionHooks = new ReflectionHooks(scanManager, _hooks);
        _reflectionHooks.Init("granblue_fantasy_relink_er");
    }

    private void DumpReflection()
    {
        if (_reflectionHooks is null)
            return;

        if (!_reflectionHooks.HasLoadedObjects)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Reflection not loaded yet, dump skipped", Color.Yellow);
            return;
        }

        string path = Path.Combine(_modLoader.GetDirectoryForModId(_modConfig.ModId), "ReflectionDump.cs");
        _reflectionHooks.DumpAll(path);
        _logger.WriteLine($"[{_modConfig.ModId}] Dumped {_reflectionHooks.ObjectCount} reflected classes to {path}");
    }

    #region Standard Overrides
    public override void ConfigurationUpdated(Config configuration)
    {
        _configuration = configuration;
        if (_fileLogger is not null)
            _fileLogger.Enabled = configuration.LogFiles;
        if (_textHooks is not null)
            _textHooks.Logging = configuration.LogText;
        if (_charaStatusHooks is not null)
            _charaStatusHooks.Dumping = configuration.DumpBuild;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");

        if (configuration.DumpReflection)
            DumpReflection();
    }
    #endregion

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}