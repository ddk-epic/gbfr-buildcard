using System.ComponentModel;
using gbfr.qol.buildcard.Template.Configuration;

namespace gbfr.qol.buildcard.Configuration;

public class Config : Configurable<Config>
{
    [DisplayName("Log File Access")]
    [Description("Writes every file the game opens to FileLog.txt in the mod folder.")]
    [DefaultValue(false)]
    public bool LogFiles { get; set; } = false;

    [DisplayName("Log UI Text")]
    [Description("Writes every change to a UI text, with the game code that set it, to TextLog.txt in the mod folder.")]
    [DefaultValue(false)]
    public bool LogText { get; set; } = false;

    [DisplayName("Dump Build Data")]
    [Description("Writes the character data each Character Details page is filled from to the Dumps folder in the mod folder.")]
    [DefaultValue(false)]
    public bool DumpBuild { get; set; } = false;

    [DisplayName("Dump Reflection")]
    [Description("Writes every reflected game class to ReflectionDump.cs in the mod folder each time the config is saved with this on.")]
    [DefaultValue(false)]
    public bool DumpReflection { get; set; } = false;
}

public class ConfiguratorMixin : ConfiguratorMixinBase
{
}
