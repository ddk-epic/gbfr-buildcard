using gbfr.qol.buildcard.Sources;

namespace gbfr.qol.buildcard.Model;

// A character's build as the card shows it.
public sealed record BuildCard(uint CharaKey, WeaponPart Weapon, TraitBoard Traits, MasteryPercents? Masteries,
    OverMasteryRow?[] OverMastery, uint?[] Summons)
{
    public string CharaName { get; init; } = "";

    // the language of the card's texts
    public string Language { get; init; } = "";
}

public sealed record WeaponPart(uint Key, uint SkinKey, bool EffectOff);

// Spent and Budgets per rank, shared by the styles
public sealed record TraitBoard(bool Captain, int[] Spent, int[] Budgets, StyleBoard[] Styles);

// Title and Name empty without a title; Cells by rank and position
public sealed record StyleBoard(uint? TitleTextId, string Title, string Name, int Perks, TraitCell?[][] Cells);

public sealed record TraitCell(int Slot, bool Picked);

// Value as the game shows it, a percentage when Percent
public sealed record OverMasteryRow(OverMasteryLine Line, int Value, bool Percent);

public sealed record MasteryPercents(int Offense, int Defense, int Collection, int Transcendence);
