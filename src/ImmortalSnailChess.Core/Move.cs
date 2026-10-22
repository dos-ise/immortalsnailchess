namespace ImmortalSnailChess.Core;

[Flags]
public enum MoveFlags
{
    None = 0,
    DoublePawnPush = 1,
    EnPassant = 2,
    KingsideCastle = 4,
    QueensideCastle = 8,
    Promotion = 16
}

public readonly struct Move
{
    public int From { get; }
    public int To { get; }
    public PieceType? Promotion { get; }
    public MoveFlags Flags { get; }

    public Move(int from, int to, PieceType? promotion = null, MoveFlags flags = MoveFlags.None)
    {
        From = from;
        To = to;
        Promotion = promotion;
        Flags = flags;
    }

    public bool IsCastle => (Flags & (MoveFlags.KingsideCastle | MoveFlags.QueensideCastle)) != 0;
    public bool IsEnPassant => (Flags & MoveFlags.EnPassant) != 0;
    public bool IsDoublePawnPush => (Flags & MoveFlags.DoublePawnPush) != 0;

    public override string ToString()
    {
        string s = Square.Name(From) + Square.Name(To);
        if (Promotion != null)
            s += "=" + char.ToUpper(Promotion.Value.ToString()[0]);
        return s;
    }
}
