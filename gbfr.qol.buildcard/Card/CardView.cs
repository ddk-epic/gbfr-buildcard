using System.Drawing;

using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Core;
using gbfr.qol.buildcard.Game;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Model;

namespace gbfr.qol.buildcard.Card;

// Writes the card to status01 through its sections: Write, then Layout.
public class CardView : IFeature
{
    private readonly UiTree _tree;
    private readonly CardSession _session;
    private readonly ControllerLifetimes _lifetimes;
    private readonly GameApi _api;
    private readonly ICardSection[] _sections;
    private readonly ILogger _logger;
    private UiObject? _root;
    private Dictionary<int, UiObject>? _objects;  // null when the prefab hash differs
    private bool _loggedHash;

    public CardView(UiTree tree, CardSession session, ControllerLifetimes lifetimes, GameApi api, ILogger logger,
        params ICardSection[] sections)
    {
        _tree = tree;
        _session = session;
        _lifetimes = lifetimes;
        _api = api;
        _logger = logger;
        _sections = sections;
        lifetimes.Destroyed += OnDestroyed;
    }

    public Capability[] Requires => [];

    public IFeature[] DependsOn => [_tree];

    public void Start() => _session.Shown += OnShown;

    public void OnShown(CharaInfo charaInfo, BuildCard card)
    {
        if (FindObjects(charaInfo) is not { } objects)
            return;
        foreach (var section in _sections)
            section.Write(objects, card);
        foreach (var section in _sections)
            section.Layout(objects, card);
    }

    // status01's objects by Id, kept until its ControllerStatus is destroyed
    private Dictionary<int, UiObject>? FindObjects(CharaInfo charaInfo)
    {
        if (charaInfo.Object is not { } root)
            return null;
        if (root == _root)
            return _objects;
        var objects = _tree.Find(charaInfo);
        if (objects == null)
            return null;
        uint hash = UiTree.PrefabHash(objects);
        if (hash != CardLayout.Status01.PrefabHash)
        {
            if (!_loggedHash)
                _logger.WriteLine($"[gbfr.qol.buildcard] status01 has {objects.Count} objects hashing to {hash:X8}, the build {CardLayout.Status01.ObjectCount} hashing to {CardLayout.Status01.PrefabHash:X8}: card not written", Color.Red);
            _loggedHash = true;
            objects = null;
        }
        if (_lifetimes.Hooks(_api.ControllerStatusDestroy))
            (_root, _objects) = (root, objects);
        return objects;
    }

    private void OnDestroyed(UiComponent controller)
    {
        if (controller.Object != _root)
            return;
        _root = null;
        _objects = null;
    }
}

// Writes one section of the card.
public interface ICardSection
{
    void Write(IReadOnlyDictionary<int, UiObject> objects, BuildCard card);

    // Wraps and joins the texts the game's fill and the Write phase set
    void Layout(IReadOnlyDictionary<int, UiObject> objects, BuildCard card) { }
}
