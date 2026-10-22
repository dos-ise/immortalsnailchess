namespace ImmortalSnailChess.Core;

public enum PieceColor
{
    White,
    Black
}

public static class PieceColorExtensions
{
    public static PieceColor Opponent(this PieceColor color) =>
        color == PieceColor.White ? PieceColor.Black : PieceColor.White;
}
