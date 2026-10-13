using gbfr.qol.buildcard.Configuration;
using gbfr.qol.buildcard.Model;

namespace gbfr.qol.buildcard.Hooks;

// Writes the Master Traits board in the configured palette.
public class MasterTraitsBinder : ICardBinder
{
    private readonly GameText _text;
    private readonly MasterTraits _masterTraits;
    private readonly UiObjects _objects;
    private readonly Func<MasterTraitsPalette> _palette;

    public MasterTraitsBinder(GameText text, MasterTraits masterTraits, UiObjects objects, Func<MasterTraitsPalette> palette)
    {
        _text = text;
        _masterTraits = masterTraits;
        _objects = objects;
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
                _objects.SetActive(objects[stars[k]], k < styles[s].Perks);
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
            _objects.SetActive(objects[layout.Root], layout.Root == board.Root);
        for (int p = 0; p < CardLayout.MasterTraits.Scenes.Length; p++)
        {
            _objects.SetActive(objects[CardLayout.MasterTraits.Scenes[p]], p == shown);
            _objects.SetActive(objects[board.Palettes[p]], p == shown);
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
                    _objects.SetActive(objects[ids.Picked[shown]], cell is { Picked: true });
                    _objects.SetActive(objects[ids.Base[shown]], cell is not { Picked: true });
                }
            }
        }
    }

    // Fills the cell text with the cell's description, or empties it
    private void SetCell(UiObject obj, uint charaKey, TraitCell? cell)
    {
        if (cell is { } c)
            _text.Wrap(obj, CardLayout.MasterTraits.CellTextWidth,
                text => _masterTraits.FillDescription(text, charaKey, c.Slot), CardLayout.MasterTraits.CellIconScale);
        else
            _text.Set(obj, "");
    }
}
