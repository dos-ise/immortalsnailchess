namespace ImmortalSnailChess.Core;

/// <summary>
/// Square helpers. Squares are indexed 0..63 with file = index &amp; 7 (a=0..h=7)
/// and rank = index &gt;&gt; 3 (1=0..8=7). a1 = 0, h1 = 7, a8 = 56, h8 = 63.
/// </summary>
public static class Square
{
    public static int File(int sq) => sq & 7;
    public static int Rank(int sq) => sq >> 3;

    public static int Index(int file, int rank) => file + (rank << 3);

    public static bool IsValid(int file, int rank) => file >= 0 && file < 8 && rank >= 0 && rank < 8;

    public static string Name(int sq) => $"{(char)('a' + File(sq))}{Rank(sq) + 1}";

    public static int Parse(string name)
    {
        if (name.Length != 2)
            throw new ArgumentException($"Invalid square name '{name}'.");
        int file = name[0] - 'a';
        int rank = name[1] - '1';
        if (!IsValid(file, rank))
            throw new ArgumentException($"Invalid square name '{name}'.");
        return Index(file, rank);
    }

    /// <summary>Chebyshev distance between two squares.</summary>
    public static int Chebyshev(int a, int b) =>
        Math.Max(Math.Abs(File(a) - File(b)), Math.Abs(Rank(a) - Rank(b)));
}
