using System.Drawing;

using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Interop.Game;

namespace gbfr.qol.buildcard.Host;

// Starts each feature whose game functions were found, in the order added; logs one line per feature left out.
public sealed class Features
{
    private readonly ILogger _logger;
    private readonly List<IFeature> _features = [];

    public Features(ILogger logger)
    {
        _logger = logger;
    }

    public void Add(params IFeature[] features) => _features.AddRange(features);

    // After the loader runs the scans
    public void Start()
    {
        foreach (var feature in _features)
        {
            string name = feature.GetType().Name;
            var missing = feature.Requires.Where(capability => !capability.Found).Select(capability => capability.Name).Distinct().ToList();
            if (missing.Count != 0)
            {
                _logger.WriteLine($"[gbfr.qol.buildcard] {name} disabled: {string.Join(", ", missing)} not found", Color.Red);
                continue;
            }
            try
            {
                feature.Start();
            }
            catch (Exception e)
            {
                _logger.WriteLine($"[gbfr.qol.buildcard] {name} failed to start: {e}", Color.Red);
            }
        }
    }
}

// A part of the mod that starts once the game functions it requires are found.
public interface IFeature
{
    Capability[] Requires { get; }

    void Start();
}
