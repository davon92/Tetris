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
    Hold
}

public enum TetrisGameMode
{
    Solo,
    VersusCpu,
    LocalVersus
}

public enum CpuDifficulty
{
    Easy,
    Normal,
    Hard
}
