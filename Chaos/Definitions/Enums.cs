namespace Chaos.Definitions;

public enum AoeShape
{
    None,
    Front,
    AllAround,
    FrontalCone,
    FrontalDiamond,
    Circle,
    Square,
    CircleOutline,
    SquareOutline
}

[Flags]
public enum TargetFilter : ulong
{
    None,
    FriendlyOnly = 1,
    HostileOnly = 1 << 1,
    NeutralOnly = 1 << 2,
    NonFriendlyOnly = 1 << 3,
    NonHostileOnly = 1 << 4,
    NonNeutralOnly = 1 << 5,
    AliveOnly = 1 << 6,
    DeadOnly = 1 << 7,
    AislingsOnly = 1 << 8,
    MonstersOnly = 1 << 9,
    MerchantsOnly = 1 << 10,
    NonAislingsOnly = 1 << 11,
    NonMonstersOnly = 1 << 12,
    NonMerchantsOnly = 1 << 13,
    SelfOnly = 1 << 14,
    OthersOnly = 1 << 15,
    GroupOnly = 1 << 16
}

public enum VisibilityType
{
    Normal,
    Hidden,
    TrueHidden,
    GmHidden
}

public enum VisionType
{
    Normal,
    Blind,
    TrueBlind
}

/// <summary>
///     One of the 5 elemental disciplines a Sorcerer can pick during progression (Floor 3's first pick, Floor 5's
///     second pick). Server-internal only - deliberately separate from
///     <see cref="Chaos.DarkAges.Definitions.Element" /> (the shared combat damage-element enum, which has no
///     "Arcane" member and represents a different concept: a creature's current offensive/defensive damage type,
///     not a Sorcerer's chosen specialization path). See <see cref="Chaos.Utilities.SorcererProgressionHelper" />
///     for how a pair of these resolves to a final
///     <see cref="Chaos.DarkAges.Definitions.AdvClass" /> specialization.
/// </summary>
public enum SorcererElement : byte
{
    Fire = 1,
    Earth = 2,
    Water = 3,
    Wind = 4,
    Arcane = 5
}