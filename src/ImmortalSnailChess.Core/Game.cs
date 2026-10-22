namespace ImmortalSnailChess.Core;

/// <summary>
/// The full move sequence and game adjudication.
/// </summary>
public static class Game
{
    /// <summary>
    /// Applies a conventional move and then the mover's snail activation (if
    /// its move counter is a multiple of <see cref="SnailLogic.StepInterval"/>).
    /// If the snail captures a king the result is set immediately; otherwise
    /// the caller is responsible for adjudication via <see cref="EvaluateState"/>.
    /// </summary>
    public static void MakeMove(GameState state, Move move)
    {
        ApplyMove(state, move);
        var mover = state.SideToMove.Opponent();

        if (state.MoveCountOf(mover) % SnailLogic.StepInterval == 0)
            SnailLogic.StepSnail(state, mover);

        state.RecordRepetition();
    }

    /// <summary>
    /// Adjudicates mate, stalemate and the draw rules for the side to move.
    /// Does nothing if the game already ended (e.g. by a snail king capture).
    /// </summary>
    public static void EvaluateState(GameState state)
    {
        if (state.Result != GameResult.Ongoing)
            return;

        var moves = GetLegalMoves(state);
        if (moves.Count == 0)
        {
            if (IsInCheck(state, state.SideToMove))
            {
                state.Result = state.SideToMove == PieceColor.White
                    ? GameResult.BlackWins
                    : GameResult.WhiteWins;
                state.EndReason = GameEndReason.Checkmate;
            }
            else
            {
                state.Result = GameResult.Draw;
                state.EndReason = GameEndReason.Stalemate;
            }
            return;
        }

        if (state.HalfmoveClock >= 100)
        {
            state.Result = GameResult.Draw;
            state.EndReason = GameEndReason.FiftyMoveRule;
            return;
        }

        if (state.RepetitionCount() >= 3)
        {
            state.Result = GameResult.Draw;
            state.EndReason = GameEndReason.ThreefoldRepetition;
            return;
        }

        if (IsInsufficientMaterial(state))
        {
            state.Result = GameResult.Draw;
            state.EndReason = GameEndReason.InsufficientMaterial;
        }
    }

    public static bool IsInCheck(GameState state, PieceColor color) =>
        MoveGenerator.IsInCheck(state, color);

    public static List<Move> GetLegalMoves(GameState state) =>
        MoveGenerator.GetLegalMoves(state);

    private static void ApplyMove(GameState state, Move move)
    {
        var board = state.Board;
        var piece = board[move.From] ?? throw new InvalidOperationException(
            $"No piece on {Square.Name(move.From)}.");

        var captured = board[move.To];

        // En passant: remove the captured pawn behind the target square.
        if (move.IsEnPassant)
        {
            int capturedPawn = move.To + (piece.Color == PieceColor.White ? -8 : 8);
            board[capturedPawn] = null;
        }

        // Castling: move the rook.
        if (move.Flags.HasFlag(MoveFlags.KingsideCastle))
        {
            int rookFrom = move.To + 1;
            int rookTo = move.To - 1;
            board[rookTo] = board[rookFrom];
            board[rookFrom] = null;
        }
        else if (move.Flags.HasFlag(MoveFlags.QueensideCastle))
        {
            int rookFrom = move.To - 2;
            int rookTo = move.To + 1;
            board[rookTo] = board[rookFrom];
            board[rookFrom] = null;
        }

        // Move the piece.
        board[move.To] = piece;
        board[move.From] = null;

        // Promotion.
        if (move.Promotion != null)
            board[move.To] = new Piece(move.Promotion.Value, piece.Color);

        // Castling rights.
        if (piece.Type == PieceType.King)
        {
            state.RemoveCastlingRight(piece.Color == PieceColor.White
                ? GameState.WhiteKingside | GameState.WhiteQueenside
                : GameState.BlackKingside | GameState.BlackQueenside);
        }
        if (piece.Type == PieceType.Rook)
            RemoveCastlingRightForSquare(state, move.From);
        if (captured is { Type: PieceType.Rook })
            RemoveCastlingRightForSquare(state, move.To);

        // En passant target.
        state.EnPassantTarget = move.IsDoublePawnPush
            ? (move.From + move.To) / 2
            : -1;

        // Halfmove clock.
        if (piece.Type == PieceType.Pawn || captured != null || move.IsEnPassant)
            state.HalfmoveClock = 0;
        else
            state.HalfmoveClock++;

        // Move counters (drive snail activation).
        if (piece.Color == PieceColor.White)
            state.WhiteMoveCount++;
        else
            state.BlackMoveCount++;

        // Fullmove number.
        if (piece.Color == PieceColor.Black)
            state.FullmoveNumber++;

        state.SideToMove = piece.Color.Opponent();
        state.LastMove = move;
        state.LastSnailStep = null;
    }

    private static void RemoveCastlingRightForSquare(GameState state, int sq)
    {
        switch (sq)
        {
            case 0: // a1
                state.RemoveCastlingRight(GameState.WhiteQueenside);
                break;
            case 7: // h1
                state.RemoveCastlingRight(GameState.WhiteKingside);
                break;
            case 56: // a8
                state.RemoveCastlingRight(GameState.BlackQueenside);
                break;
            case 63: // h8
                state.RemoveCastlingRight(GameState.BlackKingside);
                break;
        }
    }

    /// <summary>
    /// Standard insufficient material rules (same as chess.js): only kings,
    /// king + one minor piece, or king + bishop vs king + bishop with bishops
    /// on the same colour.
    /// </summary>
    public static bool IsInsufficientMaterial(GameState state)
    {
        var board = state.Board;
        int pieceCount = 0;
        int bishopCount = 0;
        int knightCount = 0;
        int lightBishops = 0;
        int darkBishops = 0;

        for (int sq = 0; sq < Board.Size; sq++)
        {
            if (board[sq] is not Piece piece)
                continue;
            pieceCount++;
            switch (piece.Type)
            {
                case PieceType.Bishop:
                    bishopCount++;
                    if ((Square.File(sq) + Square.Rank(sq)) % 2 == 0)
                        lightBishops++;
                    else
                        darkBishops++;
                    break;
                case PieceType.Knight:
                    knightCount++;
                    break;
                case PieceType.King:
                    break;
                default:
                    return false; // pawn, rook or queen present
            }
        }

        if (pieceCount <= 2)
            return true; // only kings

        if (pieceCount == 3 && (knightCount == 1 || bishopCount == 1))
            return true; // king + one minor piece

        if (pieceCount == 4 && bishopCount == 2 && knightCount == 0)
            return lightBishops == 2 || darkBishops == 2; // same-coloured bishops

        return false;
    }
}
