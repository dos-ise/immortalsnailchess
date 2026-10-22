namespace ImmortalSnailChess.Core;

/// <summary>
/// The 8x8 board. Snails are stored separately from the piece array because
/// they are walls: they are never captured and never part of the piece set.
/// </summary>
public class Board
{
    public const int Size = 64;

    private readonly Piece?[] _squares = new Piece?[Size];

    public int? WhiteSnail { get; set; }
    public int? BlackSnail { get; set; }

    public Piece? this[int sq]
    {
        get => _squares[sq];
        set => _squares[sq] = value;
    }

    public Piece? this[int file, int rank]
    {
        get => _squares[Square.Index(file, rank)];
        set => _squares[Square.Index(file, rank)] = value;
    }

    public int? SnailOf(PieceColor color) => color == PieceColor.White ? WhiteSnail : BlackSnail;

    public void SetSnail(PieceColor color, int sq)
    {
        if (color == PieceColor.White)
            WhiteSnail = sq;
        else
            BlackSnail = sq;
    }

    public bool IsSnailSquare(int sq) => WhiteSnail == sq || BlackSnail == sq;

    public int FindKing(PieceColor color)
    {
        for (int sq = 0; sq < Size; sq++)
        {
            var piece = _squares[sq];
            if (piece is { Type: PieceType.King, Color: var c } && c == color)
                return sq;
        }
        return -1;
    }

    public Board Clone()
    {
        var clone = new Board
        {
            WhiteSnail = WhiteSnail,
            BlackSnail = BlackSnail
        };
        Array.Copy(_squares, clone._squares, Size);
        return clone;
    }

    public static Board Initial()
    {
        var board = new Board();

        // White back rank
        board[0] = Piece.White(PieceType.Rook);
        board[1] = Piece.White(PieceType.Knight);
        board[2] = Piece.White(PieceType.Bishop);
        board[3] = Piece.White(PieceType.Queen);
        board[4] = Piece.White(PieceType.King);
        board[5] = Piece.White(PieceType.Bishop);
        board[6] = Piece.White(PieceType.Knight);
        board[7] = Piece.White(PieceType.Rook);
        for (int f = 0; f < 8; f++)
            board[f, 1] = Piece.White(PieceType.Pawn);

        // Black back rank
        board[56] = Piece.Black(PieceType.Rook);
        board[57] = Piece.Black(PieceType.Knight);
        board[58] = Piece.Black(PieceType.Bishop);
        board[59] = Piece.Black(PieceType.Queen);
        board[60] = Piece.Black(PieceType.King);
        board[61] = Piece.Black(PieceType.Bishop);
        board[62] = Piece.Black(PieceType.Knight);
        board[63] = Piece.Black(PieceType.Rook);
        for (int f = 0; f < 8; f++)
            board[f, 6] = Piece.Black(PieceType.Pawn);

        // Snails: white on a4, black on a5 (left side, middle ranks).
        board.WhiteSnail = Square.Parse("a4");
        board.BlackSnail = Square.Parse("a5");

        return board;
    }
}
