public enum TetriminoType
{
    I, O, T, J, L, S, Z
}

public enum TetrisCommand
{
    MoveLeft,
    MoveRight,
    SoftDrop,
    HardDrop,
    RotateClockwise,
    RotateCounterClockwise,
    Hold,

    /// <summary>Spends mana on the spell aimed at the opponent.</summary>
    CastOffensive,

    /// <summary>Spends mana on the spell aimed at the caster's own board.</summary>
    CastDefensive
}

/// <summary>
/// New values are appended rather than inserted: the mode is a serialized
/// field on <see cref="GameFlowController"/>, and Unity stores enums by
/// ordinal, so reordering would silently repoint the scene at another mode.
/// </summary>
public enum TetrisGameMode
{
    /// <summary>Endless solo: play until the board tops out, gravity by level.</summary>
    Marathon,

    VersusCpu,
    LocalVersus,

    /// <summary>Solo race to a line target. The clock, not the score, is the result.</summary>
    Sprint
}

public enum CpuDifficulty
{
    Easy,
    Normal,
    Hard
}
