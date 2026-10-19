using gbfr.qol.buildcard.Card;
using gbfr.qol.buildcard.Game;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Model;

namespace gbfr.qol.buildcard.Card.Sections;

// Shows or hides another player's name under the name band.
public class PlayerNameSection : ICardSection
{
    private readonly UiTree _tree;
    private readonly Func<bool> _shown;

    public PlayerNameSection(UiTree tree, Func<bool> shown)
    {
        _tree = tree;
        _shown = shown;
    }

    public void Write(IReadOnlyDictionary<int, UiObject> objects, BuildCard card) =>
        _tree.SetActive(objects[CardLayout.Status.PlayerName], _shown());
}
