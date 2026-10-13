using gbfr.qol.buildcard.Hooks;
using gbfr.qol.buildcard.Model;

namespace gbfr.qol.buildcard.Tests;

public class BuildCardAssemblerTests
{
    private const uint Empty = GameText.EmptyIdHash;

    private static readonly Dictionary<uint, string> Texts = new()
    {
        [0x10] = "Onslaught: Attack Up",
        [0x20] = "猛攻：攻撃力アップ",
    };

    private static BuildCard Build(MasterTraitCell[] cells, CharaEntry[] entries, int[]? percents = null) =>
        BuildCardAssembler.Build(
            new CharaBuild(1, 2, 3, false, entries, new OverMasteryLine?[4], new uint?[4]),
            cells, percents, (id, _) => Texts.GetValueOrDefault(id, ""));

    // slot: style * 100 + perk rank, 10 * (rank + 1) + position - 1, or 50 + position - 1
    private static MasterTraitCell Cell(int slot, uint key, uint title = Empty) => new(slot, key, title);

    [Fact]
    public void PlacesCellsAtTheirRankAndPosition()
    {
        var card = Build([Cell(110, 1), Cell(131, 2)], [new(1, 1), new(2, 0)]);

        var cells = card.Traits.Styles[1].Cells;
        Assert.Equal(new TraitCell(110, true), cells[0][0]);
        Assert.Equal(new TraitCell(131, false), cells[2][1]);
        Assert.Equal(1, card.Traits.Spent[0]);
        Assert.Equal(0, card.Traits.Spent[2]);
    }

    [Fact]
    public void ShowsTheNormalBoardWithoutCellsPastItsSlots()
    {
        var card = Build([Cell(59, 1)], [new(1, 1)]);

        Assert.False(card.Traits.Captain);
        Assert.Equal([4, 8, 8, 10], card.Traits.Styles[0].Cells.Select(rank => rank.Length));
        Assert.Equal(new TraitCell(59, true), card.Traits.Styles[0].Cells[3][9]);
    }

    [Fact]
    public void ShowsTheCaptainBoardForACellPastTheNormalSlots()
    {
        var card = Build([Cell(60, 1)], [new(1, 0)]);

        Assert.True(card.Traits.Captain);
        Assert.Equal([4, 8, 8, 14], card.Traits.Styles[0].Cells.Select(rank => rank.Length));
        Assert.Equal(new TraitCell(60, false), card.Traits.Styles[0].Cells[3][10]);
    }

    [Fact]
    public void SumsSpentPerRankAcrossStyles()
    {
        var card = Build([Cell(10, 1), Cell(111, 2), Cell(212, 3)], [new(1, 1), new(2, 1), new(3, 0)]);

        Assert.Equal([2, 0, 0, 0], card.Traits.Spent);
        Assert.Equal([10, 10, 10, 20], card.Traits.Budgets);
    }

    [Fact]
    public void CountsPicksAndNamesTheStyleFromItsFirstPerk()
    {
        var card = Build([Cell(100, 1, 0x10), Cell(101, 2), Cell(102, 3), Cell(200, 4, 0x20)],
            [new(1, 1), new(2, 1), new(3, 0), new(4, 0)]);

        var style = card.Traits.Styles[1];
        Assert.Equal(2, style.Perks);
        Assert.Equal(0x10u, style.TitleTextId);
        Assert.Equal("Onslaught: Attack Up", style.Title);
        Assert.Equal("Onslaught", style.Name);
        Assert.Equal("猛攻", card.Traits.Styles[2].Name);
    }

    [Fact]
    public void LeavesAStyleWithoutAPerkTitleUnnamed()
    {
        var card = Build([Cell(0, 1)], [new(1, 1)]);

        var style = card.Traits.Styles[0];
        Assert.Null(style.TitleTextId);
        Assert.Equal("", style.Title);
        Assert.Equal("", style.Name);
        Assert.Equal(1, style.Perks);
    }

    [Fact]
    public void IgnoresUnknownEntriesAndCellsPastTheStyles()
    {
        var card = Build([Cell(310, 1)], [new(1, 1), new(9, 1)]);

        Assert.All(card.Traits.Styles, style => Assert.All(style.Cells, rank => Assert.All(rank, Assert.Null)));
        Assert.Equal([0, 0, 0, 0], card.Traits.Spent);
    }

    [Fact]
    public void KeepsTheFirstCellOfASharedEffectKey()
    {
        var card = Build([Cell(110, 1), Cell(111, 1)], [new(1, 1)]);

        Assert.NotNull(card.Traits.Styles[1].Cells[0][0]);
        Assert.Null(card.Traits.Styles[1].Cells[0][1]);
    }

    [Fact]
    public void MapsMasteryCategories()
    {
        Assert.Equal(new MasteryPercents(1, 2, 3, 4), Build([], [], [1, 2, 3, 4]).Masteries);
        Assert.Null(Build([], []).Masteries);
    }
}
