namespace ImmortalSnailChess.Core;

/// <summary>
/// Full game state: board, side to move, castling rights, en passant target,
/// clocks, per-color move counters (for snail activation) and repetition tracking.
/// </summary>
public class GameState
{
    // Castling rights bitmask.
    public const int WhiteKingside = 1;
    public const int WhiteQueenside = 2;
    public const int BlackKingside = 4;
    public const int BlackQueenside = 8;

    public Board Board { get; }
    public PieceColor SideToMove { get; set; }
    public int CastlingRights { get; set; }
    public int EnPassantTarget { get; set; } = -1;
    public int HalfmoveClock { get; set; }
    public int FullmoveNumber { get; set; } = 1;

    /// <summary>Number of moves made by each color (drives snail activation).</summary>
    public int WhiteMoveCount { get; set; }
    public int BlackMoveCount { get; set; }

    public GameResult Result { get; set; } = GameResult.Ongoing;
    public GameEndReason EndReason { get; set; } = GameEndReason.None;

    public Move? LastMove { get; set; }
    public (int From, int To)? LastSnailStep { get; set; }

    private readonly Dictionary<string, int> _repetitionCounts = new();

    public GameState(Board board)
    {
        Board = board;
    }

    public int MoveCountOf(PieceColor color) => color == PieceColor.White ? WhiteMoveCount : BlackMoveCount;

    public bool CanCastleKingside(PieceColor color) =>
        (CastlingRights & (color == PieceColor.White ? WhiteKingside : BlackKingside)) != 0;

    public bool CanCastleQueenside(PieceColor color) =>
        (CastlingRights & (color == PieceColor.White ? WhiteQueenside : BlackQueenside)) != 0;

    public void RemoveCastlingRight(int right) => CastlingRights &= ~right;

    public string RepetitionKey()
    {
        var board = Board;
        var sb = new System.Text.StringBuilder(96);
        for (int rank = 7; rank >= 0; rank--)
        {
            int empty = 0;
            for (int file = 0; file < 8; file++)
            {
                var piece = board[file, rank];
                if (piece == null)
                {
                    empty++;
                }
                else
                {
                    if (empty > 0)
                    {
                        sb.Append(empty);
                        empty = 0;
                    }
                    sb.Append(piece);
                }
            }
            if (empty > 0)
                sb.Append(empty);
            if (rank > 0)
                sb.Append('/');
        }
        sb.Append(' ').Append(SideToMove == PieceColor.White ? 'w' : 'b');
        sb.Append(' ').Append(CastlingRights);
        sb.Append(' ').Append(EnPassantTarget);
        sb.Append(' ').Append(Board.WhiteSnail ?? -1).Append(',').Append(Board.BlackSnail ?? -1);
        sb.Append(' ').Append(WhiteMoveCount % SnailLogic.StepInterval)
          .Append(',').Append(BlackMoveCount % SnailLogic.StepInterval);
        return sb.ToString();
    }

    public void RecordRepetition()
    {
        string key = RepetitionKey();
        _repetitionCounts.TryGetValue(key, out int count);
        _repetitionCounts[key] = count + 1;
    }

    public int RepetitionCount()
    {
        _repetitionCounts.TryGetValue(RepetitionKey(), out int count);
        return count;
    }

    public GameState Clone()
    {
        var clone = new GameState(Board.Clone())
        {
            SideToMove = SideToMove,
            CastlingRights = CastlingRights,
            EnPassantTarget = EnPassantTarget,
            HalfmoveClock = HalfmoveClock,
            FullmoveNumber = FullmoveNumber,
            WhiteMoveCount = WhiteMoveCount,
            BlackMoveCount = BlackMoveCount,
            Result = Result,
            EndReason = EndReason,
            LastMove = LastMove,
            LastSnailStep = LastSnailStep
        };
        foreach (var (key, count) in _repetitionCounts)
            clone._repetitionCounts[key] = count;
        return clone;
    }

    public static GameState Initial()
    {
        var state = new GameState(Board.Initial())
        {
            SideToMove = PieceColor.White,
            CastlingRights = WhiteKingside | WhiteQueenside | BlackKingside | BlackQueenside
        };
        state.RecordRepetition();
        return state;
    }
}
