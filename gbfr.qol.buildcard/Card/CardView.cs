using System.Drawing;

using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Game;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Model;

namespace gbfr.qol.buildcard.Card;

// Writes the card to status01 through its binders: Write, then Layout.
public class CardView
{
    private readonly UiTree _tree;
    private readonly ControllerLifetimes _lifetimes;
    private readonly GameApi _api;
    private readonly ICardBinder[] _binders;
    private readonly ILogger _logger;
    private UiObject? _root;
    private Dictionary<int, UiObject>? _objects;  // null when the prefab hash differs
    private bool _loggedHash;

    public CardView(UiTree tree, ControllerLifetimes lifetimes, GameApi api, ILogger logger, params ICardBinder[] binders)
    {
        _tree = tree;
        _lifetimes = lifetimes;
        _api = api;
        _logger = logger;
        _binders = binders;
        lifetimes.Destroyed += OnDestroyed;
    }

    public void OnShown(CharaInfo charaInfo, BuildCard card)
    {
        if (Bind(charaInfo) is not { } objects)
            return;
        foreach (var binder in _binders)
            binder.Write(objects, card);
        foreach (var binder in _binders)
            binder.Layout(objects, card);
    }

    // status01's objects by Id, kept until its ControllerStatus is destroyed
    private Dictionary<int, UiObject>? Bind(CharaInfo charaInfo)
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
public interface ICardBinder
{
    void Write(IReadOnlyDictionary<int, UiObject> objects, BuildCard card);

    // Wraps and joins the texts the game's fill and the Write phase set
    void Layout(IReadOnlyDictionary<int, UiObject> objects, BuildCard card) { }
}
