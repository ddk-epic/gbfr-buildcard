using gbfr.qol.buildcard.Configuration;
using gbfr.qol.buildcard.Card;
using gbfr.qol.buildcard.Game;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Model;
using gbfr.qol.buildcard.Sources;

namespace gbfr.qol.buildcard.Card.Sections;

// Writes the Master Traits board in the configured palette.
public class MasterTraitsSection : ICardSection
{
    private readonly TextService _text;
    private readonly TraitTables _traitTables;
    private readonly UiTree _tree;
    private readonly Func<MasterTraitsPalette> _palette;

    public MasterTraitsSection(TextService text, TraitTables traitTables, UiTree tree, Func<MasterTraitsPalette> palette)
    {
        _text = text;
        _traitTables = traitTables;
        _tree = tree;
        _palette = palette;
    }

    public void Write(IReadOnlyDictionary<int, UiObject> objects, BuildCard card)
    {
        var traits = card.Traits;
        var board = CardLayout.MasterTraits.Boards[traits.Captain ? 1 : 0];
        var palette = _palette();
        int shown = (int)(Enum.IsDefined(palette) ? palette : MasterTraitsPalette.Blue);
        var styles = traits.Styles;

        for (int s = 0; s < styles.Length; s++)
        {
            _text.Set(objects[CardLayout.MasterTraits.PerkNames[s]], styles[s].Name);
            var stars = CardLayout.MasterTraits.PerkStars[s];
            for (int k = 0; k < stars.Length; k++)
                _tree.SetActive(objects[stars[k]], k < styles[s].Perks);
        }
        for (int s = 0; s < styles.Length; s++)
        {
            var title = objects[CardLayout.MasterTraits.StyleTitles[s]];
            if (styles[s].TitleTextId is { } titleId)
                _text.Set(title, styles[s].Title, titleId);
            else
                _text.Set(title, "");
        }
        foreach (var layout in CardLayout.MasterTraits.Boards)
            _tree.SetActive(objects[layout.Root], layout.Root == board.Root);
        for (int p = 0; p < CardLayout.MasterTraits.Scenes.Length; p++)
        {
            _tree.SetActive(objects[CardLayout.MasterTraits.Scenes[p]], p == shown);
            _tree.SetActive(objects[board.Palettes[p]], p == shown);
        }
        for (int s = 0; s < styles.Length; s++)
        {
            var ranks = styles[s].Cells;
            for (int r = 0; r < ranks.Length; r++)
            {
                _text.Set(objects[board.Styles[s][r].Count], $"{traits.Spent[r]}");
                _text.Set(objects[board.Styles[s][r].Budget], $"/{traits.Budgets[r]}");
            }
            for (int r = 0; r < ranks.Length; r++)
            {
                for (int c = 0; c < ranks[r].Length; c++)
                {
                    var cell = ranks[r][c];
                    var ids = board.Styles[s][r].Cells[c];
                    SetCell(objects[ids.On], card.CharaKey, cell is { Picked: true } ? cell : null);
                    SetCell(objects[ids.Off], card.CharaKey, cell is { Picked: false } ? cell : null);
                    _tree.SetActive(objects[ids.Picked[shown]], cell is { Picked: true });
                    _tree.SetActive(objects[ids.Base[shown]], cell is not { Picked: true });
                }
            }
        }
    }

    // Fills the cell text with the cell's description, or empties it
    private void SetCell(UiObject obj, uint charaKey, TraitCell? cell)
    {
        if (cell is { } c)
            _text.Wrap(obj, CardLayout.MasterTraits.CellTextWidth,
                text => _traitTables.FillDescription(text, charaKey, c.Slot), CardLayout.MasterTraits.CellIconScale);
        else
            _text.Set(obj, "");
    }
}
