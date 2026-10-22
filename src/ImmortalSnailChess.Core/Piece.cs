namespace ImmortalSnailChess.Core;

public readonly struct Piece
{
    public PieceType Type { get; }
    public PieceColor Color { get; }

    public Piece(PieceType type, PieceColor color)
    {
        Type = type;
        Color = color;
    }

    public static Piece White(PieceType type) => new(type, PieceColor.White);
    public static Piece Black(PieceType type) => new(type, PieceColor.Black);

    public override string ToString()
    {
        char c = Type switch
        {
            PieceType.Pawn => 'p',
            PieceType.Knight => 'n',
            PieceType.Bishop => 'b',
            PieceType.Rook => 'r',
            PieceType.Queen => 'q',
            _ => 'k'
        };
        return Color == PieceColor.White ? char.ToUpper(c).ToString() : c.ToString();
    }
}
