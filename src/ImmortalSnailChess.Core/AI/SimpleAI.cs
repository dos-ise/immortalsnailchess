namespace ImmortalSnailChess.Core.AI;

/// <summary>
/// A simple minimax (negamax) AI with alpha-beta pruning, material evaluation
/// and a snail-urgency heuristic. Uses the same variant-legal move generation
/// as the human player.
/// </summary>
public static class SimpleAI
{
    private const int MateScore = 100000;

    public static Move? FindBestMove(GameState state, int depth = 3)
    {
        var moves = Game.GetLegalMoves(state);
        if (moves.Count == 0)
            return null;

        OrderMoves(state, moves);

        int bestScore = int.MinValue;
        Move? bestMove = null;

        foreach (var move in moves)
        {
            var clone = state.Clone();
            Game.MakeMove(clone, move);
            int score = -Negamax(clone, depth - 1, int.MinValue + 1, int.MaxValue - 1);
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private static int Negamax(GameState state, int depth, int alpha, int beta)
    {
        if (state.Result != GameResult.Ongoing)
            return TerminalScore(state);

        var moves = Game.GetLegalMoves(state);
        if (moves.Count == 0)
        {
            // Mate or stalemate.
            return Game.IsInCheck(state, state.SideToMove)
                ? -MateScore + depth
                : 0;
        }

        if (depth <= 0)
            return Evaluate(state);

        OrderMoves(state, moves);

        foreach (var move in moves)
        {
            var clone = state.Clone();
            Game.MakeMove(clone, move);
            int score = -Negamax(clone, depth - 1, -beta, -alpha);
            if (score >= beta)
                return beta;
            if (score > alpha)
                alpha = score;
        }

        return alpha;
    }

    private static int TerminalScore(GameState state)
    {
        // Score from the perspective of the side to move.
        return state.Result switch
        {
            GameResult.WhiteWins => state.SideToMove == PieceColor.White ? MateScore : -MateScore,
            GameResult.BlackWins => state.SideToMove == PieceColor.Black ? MateScore : -MateScore,
            _ => 0
        };
    }

    /// <summary>
    /// Material plus snail urgency, from the perspective of the side to move.
    /// </summary>
    public static int Evaluate(GameState state)
    {
        var board = state.Board;
        int score = 0;

        for (int sq = 0; sq < Board.Size; sq++)
        {
            if (board[sq] is not Piece piece)
                continue;
            int value = PieceValue(piece.Type);
            score += piece.Color == PieceColor.White ? value : -value;
        }

        // White snail hunts the black king: proximity is good for White.
        if (board.WhiteSnail is int ws)
        {
            int blackKing = board.FindKing(PieceColor.Black);
            if (blackKing >= 0)
            {
                int d = Square.Chebyshev(ws, blackKing);
                score += (8 - d) * 25;
                if (d == 1)
                    score += 2000; // can crush the king on the next step
            }
        }

        // Black snail hunts the white king: proximity is bad for White.
        if (board.BlackSnail is int bs)
        {
            int whiteKing = board.FindKing(PieceColor.White);
            if (whiteKing >= 0)
            {
                int d = Square.Chebyshev(bs, whiteKing);
                score -= (8 - d) * 25;
                if (d == 1)
                    score -= 2000;
            }
        }

        return state.SideToMove == PieceColor.White ? score : -score;
    }

    public static int PieceValue(PieceType type) => type switch
    {
        PieceType.Pawn => 100,
        PieceType.Knight => 320,
        PieceType.Bishop => 330,
        PieceType.Rook => 500,
        PieceType.Queen => 900,
        _ => 0
    };

    private static void OrderMoves(GameState state, List<Move> moves)
    {
        // Captures first (MVV-LVA), then promotions, then the rest.
        moves.Sort((a, b) => MoveScore(state, b).CompareTo(MoveScore(state, a)));
    }

    private static int MoveScore(GameState state, Move move)
    {
        int score = 0;
        var victim = state.Board[move.To];
        if (move.IsEnPassant)
            score += 10000 + PieceValue(PieceType.Pawn);
        else if (victim is Piece v)
            score += 10000 + PieceValue(v.Type) * 10 - PieceValue(state.Board[move.From]!.Value.Type);
        if (move.Promotion != null)
            score += 8000 + PieceValue(move.Promotion.Value);
        return score;
    }
}
