using Reloaded.Mod.Interfaces;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace gbfr.qol.buildcard.Template.Configuration;

public class Configurable<TParentType> : IUpdatableConfigurable where TParentType : Configurable<TParentType>, new()
{
    public static JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    // Raised with the new configuration when the file changes
    [Browsable(false)]
    public event Action<IUpdatableConfigurable>? ConfigurationUpdated;

    [JsonIgnore]
    [Browsable(false)]
    public string? FilePath { get; private set; }

    [JsonIgnore]
    [Browsable(false)]
    public string? ConfigName { get; private set; }

    [JsonIgnore]
    [Browsable(false)]
    private FileSystemWatcher? ConfigWatcher { get; set; }

    public Configurable() { }

    private void Initialize(string filePath, string configName)
    {
        FilePath = filePath;
        ConfigName = configName;

        MakeConfigWatcher();
        Save = OnSave;
    }

    public void DisposeEvents()
    {
        ConfigWatcher?.Dispose();
        ConfigurationUpdated = null;
    }

    [JsonIgnore]
    [Browsable(false)]
    public Action? Save { get; private set; }

    [Browsable(false)]
    private static object _readLock = new object();

    // Loads the configuration, the defaults when the file is missing or unreadable
    public static TParentType FromFile(string filePath, string configName)
    {
        try
        {
            return ReadFrom(filePath, configName);
        }
        catch (Exception)
        {
            var result = new TParentType();
            result.Initialize(filePath, configName);
            return result;
        }
    }

    private void MakeConfigWatcher()
    {
        ConfigWatcher = new FileSystemWatcher(Path.GetDirectoryName(FilePath)!, Path.GetFileName(FilePath)!);
        ConfigWatcher.Changed += (sender, e) => OnConfigurationUpdated();
        ConfigWatcher.EnableRaisingEvents = true;
    }

    // Reloads the configuration and raises the updated event
    private void OnConfigurationUpdated()
    {
        lock (_readLock)
        {
            TParentType newConfig;
            try
            {
                newConfig = Utilities.TryGetValue(() => ReadFrom(FilePath!, ConfigName!), 250, 2);
            }
            catch (Exception)
            {
                return;
            }
            newConfig.ConfigurationUpdated = ConfigurationUpdated;

            DisposeEvents();

            newConfig.ConfigurationUpdated?.Invoke(newConfig);
        }
    }

    private void OnSave()
    {
        var parent = (TParentType)this;
        File.WriteAllText(FilePath!, JsonSerializer.Serialize(parent, SerializerOptions));
    }

    private static TParentType ReadFrom(string filePath, string configName)
    {
        var result = (File.Exists(filePath)
            ? JsonSerializer.Deserialize<TParentType>(File.ReadAllBytes(filePath), SerializerOptions)
            : new TParentType()) ?? new TParentType();

        result.Initialize(filePath, configName);
        return result;
    }
}
