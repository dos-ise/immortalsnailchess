namespace ImmortalSnailChess.Core;

/// <summary>
/// Snail movement. Each snail hunts the opposite king and steps one square
/// closer every <see cref="StepInterval"/> moves of its own color.
/// </summary>
public static class SnailLogic
{
    public const int StepInterval = 3;

    /// <summary>
    /// Steps the snail of the given color (if its move counter is a multiple of
    /// <see cref="StepInterval"/>). The snail captures anything on the square it
    /// lands on. If it captures a king the game ends immediately.
    /// </summary>
    public static void StepSnail(GameState state, PieceColor color)
    {
        var board = state.Board;
        int? snail = board.SnailOf(color);
        if (snail == null)
            return;

        int enemyKing = board.FindKing(color.Opponent());
        if (enemyKing < 0)
            return;

        int? target = ComputeStepTarget(board, snail.Value, color, enemyKing);
        if (target == null)
            return; // snail waits

        board.SetSnail(color, target.Value);
        state.LastSnailStep = (snail.Value, target.Value);

        if (board[target.Value] is not Piece victim)
            return;

        board[target.Value] = null;
        state.HalfmoveClock = 0; // a capture happened

        if (victim.Type == PieceType.Rook)
            RemoveCastlingRightForSquare(state, target.Value);

        // If the snail removed the pawn that could be captured en passant,
        // that en passant target is no longer valid.
        if (victim.Type == PieceType.Pawn && state.EnPassantTarget == target.Value)
            state.EnPassantTarget = -1;

        if (victim.Type != PieceType.King)
            return;

        // A king was captured: game over. Snail captures take priority over
        // any mate/stalemate produced by the same conventional move.
        int ownKing = board.FindKing(color);
        int otherKing = board.FindKing(color.Opponent());
        if (ownKing < 0 && otherKing < 0)
        {
            // Both kings fell at once: draw.
            state.Result = GameResult.Draw;
        }
        else if (victim.Color == color)
        {
            // The snail crushed its own king: the other side wins.
            state.Result = color == PieceColor.White ? GameResult.BlackWins : GameResult.WhiteWins;
        }
        else
        {
            // The snail crushed the enemy king: the snail's side wins.
            state.Result = color == PieceColor.White ? GameResult.WhiteWins : GameResult.BlackWins;
        }
        state.EndReason = GameEndReason.SnailCapturedKing;
    }

    /// <summary>
    /// Chooses the snail's next square among the eight neighbours that are on
    /// board, not occupied by the other snail and strictly closer (Chebyshev)
    /// to the enemy king. Ranking: Chebyshev distance, avoid own pieces (own
    /// king worst), direct diagonal-first step, squared distance, file, rank.
    /// Returns null if no square qualifies (the snail waits).
    /// </summary>
    public static int? ComputeStepTarget(Board board, int snail, PieceColor color, int enemyKing)
    {
        int snailFile = Square.File(snail);
        int snailRank = Square.Rank(snail);
        int kingFile = Square.File(enemyKing);
        int kingRank = Square.Rank(enemyKing);
        int baseCheb = Square.Chebyshev(snail, enemyKing);
        int directFile = snailFile + Math.Sign(kingFile - snailFile);
        int directRank = snailRank + Math.Sign(kingRank - snailRank);

        var candidates = new List<(int Cheb, int Own, int Direct, int SqDist, int File, int Rank)>();

        for (int df = -1; df <= 1; df++)
        {
            for (int dr = -1; dr <= 1; dr++)
            {
                if (df == 0 && dr == 0)
                    continue;

                int file = snailFile + df;
                int rank = snailRank + dr;
                if (!Square.IsValid(file, rank))
                    continue;

                int sq = Square.Index(file, rank);
                if (board.IsSnailSquare(sq))
                    continue; // the other snail is a wall

                int cheb = Square.Chebyshev(sq, enemyKing);
                if (cheb >= baseCheb)
                    continue; // must be strictly closer

                int own = 0;
                var piece = board[sq];
                if (piece is Piece p && p.Color == color)
                    own = p.Type == PieceType.King ? 2 : 1;

                int direct = (file == directFile && rank == directRank) ? 0 : 1;
                int sqDist = (kingFile - file) * (kingFile - file) + (kingRank - rank) * (kingRank - rank);

                candidates.Add((cheb, own, direct, sqDist, file, rank));
            }
        }

        if (candidates.Count == 0)
            return null;

        candidates.Sort();
        return Square.Index(candidates[0].File, candidates[0].Rank);
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
}
