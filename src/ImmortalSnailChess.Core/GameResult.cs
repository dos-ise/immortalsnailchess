namespace ImmortalSnailChess.Core;

public enum GameResult
{
    Ongoing,
    WhiteWins,
    BlackWins,
    Draw
}

public enum GameEndReason
{
    None,
    Checkmate,
    Stalemate,
    SnailCapturedKing,
    FiftyMoveRule,
    ThreefoldRepetition,
    InsufficientMaterial
}
