using ImmortalSnailChess.Core;

namespace ImmortalSnailChess.Tests;

/// <summary>
/// Test helper to build game states from a FEN-like board description.
/// </summary>
public static class TestBoard
{
    /// <summary>
    /// Builds a state from a FEN board part (e.g. "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR").
    /// Snails and move counters can be set explicitly.
    /// </summary>
    public static GameState Make(
        string placement,
        PieceColor sideToMove = PieceColor.White,
        string? whiteSnail = null,
        string? blackSnail = null,
        int whiteMoves = 0,
        int blackMoves = 0,
        int castling = GameState.WhiteKingside | GameState.WhiteQueenside |
                       GameState.BlackKingside | GameState.BlackQueenside)
    {
        var board = new Board();
        var ranks = placement.Split('/');
        if (ranks.Length != 8)
            throw new ArgumentException("Placement must have 8 ranks.");

        for (int r = 0; r < 8; r++)
        {
            string rank = ranks[7 - r]; // FEN lists rank 8 first
            int file = 0;
            foreach (char c in rank)
            {
                if (char.IsDigit(c))
                {
                    file += c - '0';
                }
                else
                {
                    var color = char.IsUpper(c) ? PieceColor.White : PieceColor.Black;
                    var type = char.ToLower(c) switch
                    {
                        'p' => PieceType.Pawn,
                        'n' => PieceType.Knight,
                        'b' => PieceType.Bishop,
                        'r' => PieceType.Rook,
                        'q' => PieceType.Queen,
                        'k' => PieceType.King,
                        _ => throw new ArgumentException($"Unknown piece '{c}'.")
                    };
                    board[file, r] = new Piece(type, color);
                    file++;
                }
            }
        }

        if (whiteSnail != null)
            board.WhiteSnail = Square.Parse(whiteSnail);
        if (blackSnail != null)
            board.BlackSnail = Square.Parse(blackSnail);

        return new GameState(board)
        {
            SideToMove = sideToMove,
            CastlingRights = castling,
            WhiteMoveCount = whiteMoves,
            BlackMoveCount = blackMoves
        };
    }

    public static string Name(Move move) => move.ToString();
}
