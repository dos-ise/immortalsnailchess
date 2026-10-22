using ImmortalSnailChess.Core;
using ImmortalSnailChess.Core.AI;

namespace ImmortalSnailChess.Tests;

public class GameLogicTests
{
    // ---------- Initial position ----------

    [Fact]
    public void InitialPosition_Has19LegalMovesForWhite()
    {
        // 19, not 20: the white snail on a4 blocks the a-pawn's double push (a2-a4).
        var state = GameState.Initial();
        var moves = Game.GetLegalMoves(state);
        Assert.Equal(19, moves.Count);
        Assert.Equal(PieceColor.White, state.SideToMove);
        Assert.Equal(Square.Parse("a4"), state.Board.WhiteSnail);
        Assert.Equal(Square.Parse("a5"), state.Board.BlackSnail);
    }

    // ---------- Snail activation ----------

    [Fact]
    public void Snail_StepsAfterEveryThirdOwnMove()
    {
        var state = GameState.Initial();
        Play(state, "a2a3");
        Play(state, "a7a6");
        Play(state, "b2b3");
        Play(state, "b7b6");
        Assert.Equal(Square.Parse("a4"), state.Board.WhiteSnail); // not yet

        Play(state, "c2c3"); // white's 3rd move
        // From a4 the direct diagonal step toward e8 is b5.
        Assert.Equal(Square.Parse("b5"), state.Board.WhiteSnail);
    }

    [Fact]
    public void Snail_DoesNotStepOnNonMultipleMoves()
    {
        var state = TestBoard.Make(
            "4k3/8/8/8/8/8/8/4K3 w - - 0 1".Split(' ')[0],
            whiteSnail: "a4", blackSnail: "h5", whiteMoves: 1, blackMoves: 1);
        // White's 2nd move: no step.
        Play(state, "e1e2");
        Assert.Equal(Square.Parse("a4"), state.Board.WhiteSnail);
    }

    // ---------- Snail step algorithm ----------

    [Fact]
    public void Snail_MovesDiagonallyTowardKing()
    {
        var state = TestBoard.Make(
            "4k3/8/8/8/8/8/8/4K3",
            whiteSnail: "d4", blackSnail: "h5", whiteMoves: 2);
        Play(state, "e1e2"); // 3rd white move
        // From d4 toward e8: direct diagonal step is e5.
        Assert.Equal(Square.Parse("e5"), state.Board.WhiteSnail);
    }

    [Fact]
    public void Snail_AvoidsOwnPieces()
    {
        var state = TestBoard.Make(
            "4k3/8/8/3P4/8/8/8/4K3",
            whiteSnail: "d4", blackSnail: "h5", whiteMoves: 2);
        Play(state, "e1e2"); // 3rd white move
        // d5 is an own pawn; the snail prefers c5/e5 (same Chebyshev, no own piece).
        Assert.NotEqual(Square.Parse("d5"), state.Board.WhiteSnail);
        Assert.True(state.Board.WhiteSnail is int ws &&
                    (ws == Square.Parse("c5") || ws == Square.Parse("e5")));
    }

    [Fact]
    public void Snail_CapturesPieceOnLandingSquare()
    {
        // The direct diagonal step from d4 toward e8 is e5; a black knight there
        // is crushed when the snail lands on it.
        var state = TestBoard.Make(
            "4k3/8/8/4n3/8/8/8/4K3",
            whiteSnail: "d4", blackSnail: "h5", whiteMoves: 2);
        Play(state, "e1e2"); // 3rd white move
        Assert.Equal(Square.Parse("e5"), state.Board.WhiteSnail);
        Assert.Null(state.Board[Square.Parse("e5")]);
    }

    // ---------- Snail captures king ----------

    [Fact]
    public void Snail_CapturingEnemyKing_Wins()
    {
        var state = TestBoard.Make(
            "8/4k3/8/8/8/8/8/4K3",
            whiteSnail: "d6", blackSnail: "h5", whiteMoves: 2);
        Play(state, "e1e2"); // 3rd white move
        Assert.Equal(GameResult.WhiteWins, state.Result);
        Assert.Equal(GameEndReason.SnailCapturedKing, state.EndReason);
    }

    [Fact]
    public void Snail_CapturingOwnKing_Loses()
    {
        // Black snail on a1, black king on b2 (between the snail and the white
        // king on h8). Black's 3rd move makes the black snail crush its own king.
        var state = TestBoard.Make(
            "7K/p7/8/8/8/8/1k6/8",
            whiteSnail: "h5", blackSnail: "a1", blackMoves: 2,
            sideToMove: PieceColor.Black);
        Play(state, "a7a6"); // black's 3rd move
        Assert.Equal(GameResult.WhiteWins, state.Result);
        Assert.Equal(GameEndReason.SnailCapturedKing, state.EndReason);
    }

    // ---------- Snails are walls ----------

    [Fact]
    public void Snail_BlocksSlidingAttack()
    {
        // White rook on a1, white snail on a4: the rook cannot attack through it.
        var state = TestBoard.Make(
            "k7/8/8/8/8/8/8/R3K3",
            whiteSnail: "a4", blackSnail: "h5");
        Assert.False(Game.IsInCheck(state, PieceColor.Black));
    }

    [Fact]
    public void Piece_CannotMoveOntoSnailSquare()
    {
        var state = TestBoard.Make(
            "4k3/8/8/8/8/8/8/R3K3",
            whiteSnail: "a4", blackSnail: "h5");
        var moves = Game.GetLegalMoves(state);
        Assert.DoesNotContain(moves, m => m.To == Square.Parse("a4"));
    }

    [Fact]
    public void Knight_JumpsOverSnail()
    {
        // White knight on b3, white snail on a4: the knight can jump to a5/c5.
        var state = TestBoard.Make(
            "4k3/8/8/8/8/1N6/8/4K3",
            whiteSnail: "a4", blackSnail: "h5");
        var moves = Game.GetLegalMoves(state);
        Assert.Contains(moves, m => m.From == Square.Parse("b3") && m.To == Square.Parse("a5"));
        Assert.Contains(moves, m => m.From == Square.Parse("b3") && m.To == Square.Parse("c5"));
    }

    [Fact]
    public void Snail_DoesNotGiveCheck()
    {
        // White king adjacent to the black snail: not in check.
        var state = TestBoard.Make(
            "4k3/8/8/8/8/8/8/4K3",
            whiteSnail: "h5", blackSnail: "e2");
        Assert.False(Game.IsInCheck(state, PieceColor.White));
    }

    // ---------- King safety after snail activation ----------

    [Fact]
    public void Move_ThatExposesKingAfterSnailStep_IsIllegal()
    {
        // White snail on e2 shields the king from the black rook on e8. Any white
        // move (3rd move) makes the snail step away and exposes the king.
        var state = TestBoard.Make(
            "4r1k1/8/8/8/8/8/P7/4K3",
            whiteSnail: "e2", blackSnail: "h5", whiteMoves: 2);
        var moves = Game.GetLegalMoves(state);
        // a2-a3 would trigger the snail step and expose the king: illegal.
        Assert.DoesNotContain(moves, m => m.From == Square.Parse("a2") && m.To == Square.Parse("a3"));
        // The king can move off the e-file.
        Assert.Contains(moves, m => m.From == Square.Parse("e1") && m.To == Square.Parse("d1"));
    }

    // ---------- Check, checkmate, stalemate ----------

    [Fact]
    public void Checkmate_IsDetected()
    {
        // Back-rank mate.
        var state = TestBoard.Make(
            "4k3/8/8/8/8/8/5PPP/4r1K1",
            whiteSnail: "h5", blackSnail: "a4");
        Game.EvaluateState(state);
        Assert.Equal(GameResult.BlackWins, state.Result);
        Assert.Equal(GameEndReason.Checkmate, state.EndReason);
    }

    [Fact]
    public void Stalemate_IsDetected()
    {
        // White Kh1, black Rg2 + Kf3: no legal moves, not in check.
        var state = TestBoard.Make(
            "8/8/8/8/8/5k2/6r1/7K",
            whiteSnail: "h5", blackSnail: "a4");
        Game.EvaluateState(state);
        Assert.Equal(GameResult.Draw, state.Result);
        Assert.Equal(GameEndReason.Stalemate, state.EndReason);
    }

    // ---------- Castling ----------

    [Fact]
    public void Castling_WorksNormally()
    {
        var state = TestBoard.Make(
            "4k3/8/8/8/8/8/8/4K2R",
            whiteSnail: "h5", blackSnail: "a4");
        var moves = Game.GetLegalMoves(state);
        Assert.Contains(moves, m => m.Flags.HasFlag(MoveFlags.KingsideCastle));
    }

    [Fact]
    public void Castling_BlockedBySnailOnPath()
    {
        var state = TestBoard.Make(
            "4k3/8/8/8/8/8/8/4K2R",
            whiteSnail: "f1", blackSnail: "a4");
        var moves = Game.GetLegalMoves(state);
        Assert.DoesNotContain(moves, m => m.Flags.HasFlag(MoveFlags.KingsideCastle));
    }

    // ---------- En passant ----------

    [Fact]
    public void EnPassant_Works()
    {
        var state = TestBoard.Make(
            "4k3/8/8/3pP3/8/8/8/4K3",
            whiteSnail: "h5", blackSnail: "a4");
        // Set up the en passant target as if black just played d7-d5.
        state.EnPassantTarget = Square.Parse("d6");
        var moves = Game.GetLegalMoves(state);
        Assert.Contains(moves, m => m.IsEnPassant && m.From == Square.Parse("e5") && m.To == Square.Parse("d6"));
    }

    // ---------- Promotion ----------

    [Fact]
    public void Promotion_OffersFourPieces()
    {
        var state = TestBoard.Make(
            "3k4/4P3/8/8/8/8/8/4K3",
            whiteSnail: "h5", blackSnail: "a4");
        var moves = Game.GetLegalMoves(state);
        var promotions = moves.Where(m => m.From == Square.Parse("e7") && m.To == Square.Parse("e8")).ToList();
        Assert.Equal(4, promotions.Count);
        Assert.Contains(promotions, m => m.Promotion == PieceType.Queen);
        Assert.Contains(promotions, m => m.Promotion == PieceType.Rook);
        Assert.Contains(promotions, m => m.Promotion == PieceType.Bishop);
        Assert.Contains(promotions, m => m.Promotion == PieceType.Knight);
    }

    // ---------- Draws ----------

    [Fact]
    public void FiftyMoveRule_Draw()
    {
        var state = TestBoard.Make(
            "4k3/8/8/8/8/8/8/4K3",
            whiteSnail: "h5", blackSnail: "a4");
        state.HalfmoveClock = 100;
        Game.EvaluateState(state);
        Assert.Equal(GameResult.Draw, state.Result);
        Assert.Equal(GameEndReason.FiftyMoveRule, state.EndReason);
    }

    [Fact]
    public void InsufficientMaterial_Draw()
    {
        var state = TestBoard.Make(
            "4k3/8/8/8/8/8/8/4KB2",
            whiteSnail: "h5", blackSnail: "a4");
        Game.EvaluateState(state);
        Assert.Equal(GameResult.Draw, state.Result);
        Assert.Equal(GameEndReason.InsufficientMaterial, state.EndReason);
    }

    // ---------- AI ----------

    [Fact]
    public void AI_ReturnsLegalMove()
    {
        var state = GameState.Initial();
        var move = SimpleAI.FindBestMove(state, depth: 2);
        Assert.NotNull(move);
        Assert.Contains(move.Value, Game.GetLegalMoves(state));
    }

    [Fact]
    public void AI_DoesNotLoseImmediately_WhenAlternativeExists()
    {
        // White king e1, white snail e2 shields from rook e8; white to move with
        // whiteMoves=2. The AI must not pick a move that exposes the king.
        var state = TestBoard.Make(
            "4r1k1/8/8/8/8/8/P7/4K3",
            whiteSnail: "e2", blackSnail: "h5", whiteMoves: 2);
        var move = SimpleAI.FindBestMove(state, depth: 2);
        Assert.NotNull(move);
        // The chosen move must be legal (king stays safe after the snail step).
        Assert.Contains(move.Value, Game.GetLegalMoves(state));
    }

    // ---------- Helpers ----------

    private static void Play(GameState state, string move)
    {
        int from = Square.Parse(move.Substring(0, 2));
        int to = Square.Parse(move.Substring(2, 2));
        var legal = Game.GetLegalMoves(state);
        var found = legal.FirstOrDefault(m => m.From == from && m.To == to);
        Assert.True(found.From != 0 || found.To != 0, $"Move {move} is not legal in this position.");
        Game.MakeMove(state, found);
        Game.EvaluateState(state);
    }
}
