/// <summary>
/// Every rebindable gameplay control, in the order the options screen lists
/// them. Bindings are stored as one entry per action, so adding a control here
/// is all it takes for the router, the rebind screen and the saved profile to
/// pick it up.
/// </summary>
public enum GameAction
{
    MoveLeft,
    MoveRight,
    SoftDrop,
    HardDrop,
    RotateClockwise,
    RotateCounterClockwise,
    Hold,
    CastOffensive,
    CastDefensive
}

public static class GameActionInfo
{
    /// <summary>How many actions exist. Bindings are arrays indexed by action.</summary>
    public const int Count = 9;

    public static string DisplayName(GameAction action)
    {
        return action switch
        {
            GameAction.MoveLeft => "MOVE LEFT",
            GameAction.MoveRight => "MOVE RIGHT",
            GameAction.SoftDrop => "FAST FALL",
            GameAction.HardDrop => "HARD DROP",
            GameAction.RotateClockwise => "ROTATE RIGHT",
            GameAction.RotateCounterClockwise => "ROTATE LEFT",
            GameAction.Hold => "HOLD",
            GameAction.CastOffensive => "OFFENSIVE MAGIC",
            _ => "DEFENSIVE MAGIC"
        };
    }

    /// <summary>The command an action dispatches. Movement repeats are handled separately.</summary>
    public static TetrisCommand ToCommand(GameAction action)
    {
        return action switch
        {
            GameAction.MoveLeft => TetrisCommand.MoveLeft,
            GameAction.MoveRight => TetrisCommand.MoveRight,
            GameAction.SoftDrop => TetrisCommand.SoftDrop,
            GameAction.HardDrop => TetrisCommand.HardDrop,
            GameAction.RotateClockwise => TetrisCommand.RotateClockwise,
            GameAction.RotateCounterClockwise => TetrisCommand.RotateCounterClockwise,
            GameAction.Hold => TetrisCommand.Hold,
            GameAction.CastOffensive => TetrisCommand.CastOffensive,
            _ => TetrisCommand.CastDefensive
        };
    }
}
