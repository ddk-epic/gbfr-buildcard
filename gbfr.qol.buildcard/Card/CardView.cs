using System.Drawing;

using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Game;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Model;

namespace gbfr.qol.buildcard.Card;

// Finds status01's objects once per fill and writes the card through its binders: Write, then Layout.
public class CardView
{
    private readonly UiTree _tree;
    private readonly TextService _text;
    private readonly CardSession _session;
    private readonly ICardBinder[] _binders;
    private readonly ILogger _logger;
    private bool _loggedHash;

    public CardView(UiTree tree, TextService text, CardSession session, ILogger logger, params ICardBinder[] binders)
    {
        _tree = tree;
        _text = text;
        _session = session;
        _logger = logger;
        _binders = binders;
    }

    public void OnShown(CharaInfo charaInfo, BuildCard card)
    {
        var objects = _tree.Find(charaInfo);
        if (objects == null)
            return;
        uint hash = UiTree.PrefabHash(objects);
        if (hash != CardLayout.Status01.PrefabHash)
        {
            if (!_loggedHash)
                _logger.WriteLine($"[gbfr.qol.buildcard] status01 has {objects.Count} objects hashing to {hash:X8}, the build {CardLayout.Status01.ObjectCount} hashing to {CardLayout.Status01.PrefabHash:X8}: card not written", Color.Red);
            _loggedHash = true;
            return;
        }

        foreach (var binder in _binders)
            binder.Write(objects, card);
        foreach (var binder in _binders)
            binder.Layout(objects, card);
        _session.SetCharaName(_text.Read(objects[CardLayout.Status.CharaName]));
    }
}

// Writes one section of the card.
public interface ICardBinder
{
    void Write(IReadOnlyDictionary<int, UiObject> objects, BuildCard card);

    // Wraps and joins the texts the game's fill and the Write phase set
    void Layout(IReadOnlyDictionary<int, UiObject> objects, BuildCard card) { }
}
