namespace ImmortalSnailChess.Core;

/// <summary>
/// Move generation. Snails are walls: no piece may land on or pass through a
/// snail (knights jump over), and snails never give check.
/// </summary>
public static class MoveGenerator
{
    private static readonly (int Df, int Dr)[] KnightOffsets =
    {
        (-2, -1), (-2, 1), (-1, -2), (-1, 2), (1, -2), (1, 2), (2, -1), (2, 1)
    };

    private static readonly (int Df, int Dr)[] KingOffsets =
    {
        (-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1)
    };

    private static readonly (int Df, int Dr)[] RookDirections =
    {
        (-1, 0), (1, 0), (0, -1), (0, 1)
    };

    private static readonly (int Df, int Dr)[] BishopDirections =
    {
        (-1, -1), (-1, 1), (1, -1), (1, 1)
    };

    /// <summary>
    /// Generates all legal moves of the side to move. A move is legal if, after
    /// the full sequence (conventional move + the mover's snail activation), the
    /// game has not ended by a snail king capture and the mover's king is safe.
    /// </summary>
    public static List<Move> GetLegalMoves(GameState state)
    {
        var pseudo = GeneratePseudoLegalMoves(state);
        var legal = new List<Move>(pseudo.Count);
        var mover = state.SideToMove;

        foreach (var move in pseudo)
        {
            var clone = state.Clone();
            Game.MakeMove(clone, move);

            if (clone.Result != GameResult.Ongoing)
            {
                // The game ended (snail captured a king, or mate/stalemate):
                // the move is legal.
                legal.Add(move);
            }
            else if (!IsInCheck(clone, mover))
            {
                legal.Add(move);
            }
        }

        return legal;
    }

    public static List<Move> GeneratePseudoLegalMoves(GameState state)
    {
        var moves = new List<Move>(32);
        var board = state.Board;
        var color = state.SideToMove;

        for (int sq = 0; sq < Board.Size; sq++)
        {
            if (board[sq] is not Piece piece)
                continue;
            if (piece.Color != color)
                continue;
            if (board.IsSnailSquare(sq))
                continue; // nothing can stand on a snail square

            switch (piece.Type)
            {
                case PieceType.Pawn:
                    GeneratePawnMoves(state, sq, color, moves);
                    break;
                case PieceType.Knight:
                    GenerateKnightMoves(board, sq, color, moves);
                    break;
                case PieceType.Bishop:
                    GenerateSlidingMoves(board, sq, color, BishopDirections, moves);
                    break;
                case PieceType.Rook:
                    GenerateSlidingMoves(board, sq, color, RookDirections, moves);
                    break;
                case PieceType.Queen:
                    GenerateSlidingMoves(board, sq, color, RookDirections, moves);
                    GenerateSlidingMoves(board, sq, color, BishopDirections, moves);
                    break;
                case PieceType.King:
                    GenerateKingMoves(state, sq, color, moves);
                    break;
            }
        }

        return moves;
    }

    private static void GeneratePawnMoves(GameState state, int sq, PieceColor color, List<Move> moves)
    {
        var board = state.Board;
        int file = Square.File(sq);
        int rank = Square.Rank(sq);
        int dir = color == PieceColor.White ? 1 : -1;
        int startRank = color == PieceColor.White ? 1 : 6;
        int promoRank = color == PieceColor.White ? 7 : 0;

        // Forward one
        int r1 = rank + dir;
        if (Square.IsValid(file, r1))
        {
            int s1 = Square.Index(file, r1);
            if (board[s1] == null && !board.IsSnailSquare(s1))
            {
                if (r1 == promoRank)
                    AddPromotions(moves, sq, s1);
                else
                    moves.Add(new Move(sq, s1));

                // Forward two from the starting rank
                if (rank == startRank)
                {
                    int r2 = rank + 2 * dir;
                    int s2 = Square.Index(file, r2);
                    if (board[s2] == null && !board.IsSnailSquare(s2))
                        moves.Add(new Move(sq, s2, null, MoveFlags.DoublePawnPush));
                }
            }
        }

        // Diagonal captures
        foreach (int df in new[] { -1, 1 })
        {
            int f = file + df;
            int r = rank + dir;
            if (!Square.IsValid(f, r))
                continue;
            int target = Square.Index(f, r);
            var piece = board[target];
            if (piece is Piece p && p.Color != color && !board.IsSnailSquare(target))
            {
                if (r == promoRank)
                    AddPromotions(moves, sq, target);
                else
                    moves.Add(new Move(sq, target));
            }
            else if (target == state.EnPassantTarget)
            {
                moves.Add(new Move(sq, target, null, MoveFlags.EnPassant));
            }
        }
    }

    private static void AddPromotions(List<Move> moves, int from, int to)
    {
        moves.Add(new Move(from, to, PieceType.Queen, MoveFlags.Promotion));
        moves.Add(new Move(from, to, PieceType.Rook, MoveFlags.Promotion));
        moves.Add(new Move(from, to, PieceType.Bishop, MoveFlags.Promotion));
        moves.Add(new Move(from, to, PieceType.Knight, MoveFlags.Promotion));
    }

    private static void GenerateKnightMoves(Board board, int sq, PieceColor color, List<Move> moves)
    {
        int file = Square.File(sq);
        int rank = Square.Rank(sq);
        foreach (var (df, dr) in KnightOffsets)
        {
            int f = file + df;
            int r = rank + dr;
            if (!Square.IsValid(f, r))
                continue;
            int target = Square.Index(f, r);
            if (board.IsSnailSquare(target))
                continue; // cannot land on a snail
            var piece = board[target];
            if (piece is not Piece p || p.Color != color)
                moves.Add(new Move(sq, target));
        }
    }

    private static void GenerateSlidingMoves(
        Board board, int sq, PieceColor color, (int Df, int Dr)[] directions, List<Move> moves)
    {
        int file = Square.File(sq);
        int rank = Square.Rank(sq);
        foreach (var (df, dr) in directions)
        {
            int f = file + df;
            int r = rank + dr;
            while (Square.IsValid(f, r))
            {
                int target = Square.Index(f, r);
                if (board.IsSnailSquare(target))
                    break; // snails are walls: cannot pass through or capture
                if (board[target] is not Piece piece)
                {
                    moves.Add(new Move(sq, target));
                }
                else
                {
                    if (piece.Color != color)
                        moves.Add(new Move(sq, target));
                    break;
                }
                f += df;
                r += dr;
            }
        }
    }

    private static void GenerateKingMoves(GameState state, int sq, PieceColor color, List<Move> moves)
    {
        var board = state.Board;
        int file = Square.File(sq);
        int rank = Square.Rank(sq);
        foreach (var (df, dr) in KingOffsets)
        {
            int f = file + df;
            int r = rank + dr;
            if (!Square.IsValid(f, r))
                continue;
            int target = Square.Index(f, r);
            if (board.IsSnailSquare(target))
                continue;
            var piece = board[target];
            if (piece is not Piece p || p.Color != color)
                moves.Add(new Move(sq, target));
        }

        // Castling. Snails on the path block it (they are walls); snails do not
        // give check, so they do not prevent castling through attacked squares.
        if (color == PieceColor.White && sq == Square.Parse("e1"))
        {
            if (state.CanCastleKingside(PieceColor.White) &&
                IsEmpty(board, "f1") && IsEmpty(board, "g1") &&
                board[Square.Parse("h1")] is { Type: PieceType.Rook, Color: PieceColor.White } &&
                !IsInCheck(state, PieceColor.White) &&
                !IsSquareAttacked(state, Square.Parse("f1"), PieceColor.Black) &&
                !IsSquareAttacked(state, Square.Parse("g1"), PieceColor.Black))
            {
                moves.Add(new Move(sq, Square.Parse("g1"), null, MoveFlags.KingsideCastle));
            }
            if (state.CanCastleQueenside(PieceColor.White) &&
                IsEmpty(board, "b1") && IsEmpty(board, "c1") && IsEmpty(board, "d1") &&
                board[Square.Parse("a1")] is { Type: PieceType.Rook, Color: PieceColor.White } &&
                !IsInCheck(state, PieceColor.White) &&
                !IsSquareAttacked(state, Square.Parse("d1"), PieceColor.Black) &&
                !IsSquareAttacked(state, Square.Parse("c1"), PieceColor.Black))
            {
                moves.Add(new Move(sq, Square.Parse("c1"), null, MoveFlags.QueensideCastle));
            }
        }
        else if (color == PieceColor.Black && sq == Square.Parse("e8"))
        {
            if (state.CanCastleKingside(PieceColor.Black) &&
                IsEmpty(board, "f8") && IsEmpty(board, "g8") &&
                board[Square.Parse("h8")] is { Type: PieceType.Rook, Color: PieceColor.Black } &&
                !IsInCheck(state, PieceColor.Black) &&
                !IsSquareAttacked(state, Square.Parse("f8"), PieceColor.White) &&
                !IsSquareAttacked(state, Square.Parse("g8"), PieceColor.White))
            {
                moves.Add(new Move(sq, Square.Parse("g8"), null, MoveFlags.KingsideCastle));
            }
            if (state.CanCastleQueenside(PieceColor.Black) &&
                IsEmpty(board, "b8") && IsEmpty(board, "c8") && IsEmpty(board, "d8") &&
                board[Square.Parse("a8")] is { Type: PieceType.Rook, Color: PieceColor.Black } &&
                !IsInCheck(state, PieceColor.Black) &&
                !IsSquareAttacked(state, Square.Parse("d8"), PieceColor.White) &&
                !IsSquareAttacked(state, Square.Parse("c8"), PieceColor.White))
            {
                moves.Add(new Move(sq, Square.Parse("c8"), null, MoveFlags.QueensideCastle));
            }
        }
    }

    private static bool IsEmpty(Board board, string name) =>
        board[Square.Parse(name)] == null && !board.IsSnailSquare(Square.Parse(name));

    public static bool IsInCheck(GameState state, PieceColor color)
    {
        int king = state.Board.FindKing(color);
        if (king < 0)
            return false; // no king (e.g. captured by a snail)
        return IsSquareAttacked(state, king, color.Opponent());
    }

    /// <summary>
    /// Checks whether <paramref name="square"/> is attacked by any piece of
    /// <paramref name="byColor"/>. Snails block sliding attacks and never attack.
    /// </summary>
    public static bool IsSquareAttacked(GameState state, int square, PieceColor byColor)
    {
        var board = state.Board;
        int file = Square.File(square);
        int rank = Square.Rank(square);

        // Pawns
        int pawnDir = byColor == PieceColor.White ? 1 : -1;
        foreach (int df in new[] { -1, 1 })
        {
            int f = file + df;
            int r = rank - pawnDir;
            if (Square.IsValid(f, r) &&
                board[f, r] is { Type: PieceType.Pawn, Color: var c } && c == byColor)
                return true;
        }

        // Knights
        foreach (var (df, dr) in KnightOffsets)
        {
            int f = file + df;
            int r = rank + dr;
            if (Square.IsValid(f, r) &&
                board[f, r] is { Type: PieceType.Knight, Color: var c } && c == byColor)
                return true;
        }

        // King
        foreach (var (df, dr) in KingOffsets)
        {
            int f = file + df;
            int r = rank + dr;
            if (Square.IsValid(f, r) &&
                board[f, r] is { Type: PieceType.King, Color: var c } && c == byColor)
                return true;
        }

        // Sliding pieces (rook/bishop/queen). Snails block the ray.
        foreach (var (df, dr) in RookDirections)
        {
            if (RayAttacks(board, file, rank, df, dr, byColor, PieceType.Rook))
                return true;
        }
        foreach (var (df, dr) in BishopDirections)
        {
            if (RayAttacks(board, file, rank, df, dr, byColor, PieceType.Bishop))
                return true;
        }

        return false;
    }

    private static bool RayAttacks(
        Board board, int file, int rank, int df, int dr, PieceColor byColor, PieceType requiredType)
    {
        int f = file + df;
        int r = rank + dr;
        while (Square.IsValid(f, r))
        {
            int sq = Square.Index(f, r);
            if (board.IsSnailSquare(sq))
                return false; // snail blocks the ray
            if (board[sq] is not Piece piece)
            {
                f += df;
                r += dr;
                continue;
            }
            if (piece.Color == byColor &&
                (piece.Type == requiredType || piece.Type == PieceType.Queen))
                return true;
            return false; // any other piece blocks
        }
        return false;
    }
}
