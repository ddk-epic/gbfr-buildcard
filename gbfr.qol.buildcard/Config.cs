using System.ComponentModel;
using gbfr.qol.buildcard.Template.Configuration;

namespace gbfr.qol.buildcard.Configuration;

public class Config : Configurable<Config>
{
    /*
        User Properties:
            - Please put all of your configurable properties here.
    
        By default, configuration saves as "Config.json" in mod user config folder.    
        Need more config files/classes? See Configuration.cs
    
        Available Attributes:
        - Category
        - DisplayName
        - Description
        - DefaultValue

        // Technically Supported but not Useful
        - Browsable
        - Localizable

        The `DefaultValue` attribute is used as part of the `Reset` button in Reloaded-Launcher.
    */

    [DisplayName("Log File Access")]
    [Description("Writes every file the game opens to FileLog.txt in the mod folder.")]
    [DefaultValue(false)]
    public bool LogFiles { get; set; } = false;

    [DisplayName("Dump Reflection")]
    [Description("Writes every reflected game class to ReflectionDump.cs in the mod folder each time the config is saved with this on.")]
    [DefaultValue(false)]
    public bool DumpReflection { get; set; } = false;
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}
