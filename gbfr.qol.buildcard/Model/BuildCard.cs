using gbfr.qol.buildcard.Hooks;

namespace gbfr.qol.buildcard.Model;

// A character's build as the card shows it.
public sealed record BuildCard(uint CharaKey, WeaponPart Weapon, TraitBoard Traits, MasteryPercents? Masteries,
    OverMasteryLine?[] OverMastery, uint?[] Summons);

public sealed record WeaponPart(uint Key, uint MirageKey, bool AlternateArt);

// Spent and Budgets per rank, shared by the styles
public sealed record TraitBoard(bool Captain, int[] Spent, int[] Budgets, StyleBoard[] Styles);

// Title and Name empty without a title; Cells by rank and position
public sealed record StyleBoard(uint? TitleTextId, string Title, string Name, int Perks, TraitCell?[][] Cells);

public sealed record TraitCell(int Slot, bool Picked);

public sealed record MasteryPercents(int Offense, int Defense, int Collection, int Transcendence);
