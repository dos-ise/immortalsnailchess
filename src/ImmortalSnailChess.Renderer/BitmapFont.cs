using System.Numerics;

namespace ImmortalSnailChess.Renderer;

/// <summary>
/// A 5x7 bitmap font (public domain font5x7). Each glyph is five bytes, one
/// per column; the low 7 bits of each byte are the rows (bit 0 = top row).
/// </summary>
public class BitmapFont
{
    public const int GlyphWidth = 5;
    public const int GlyphHeight = 7;

    private readonly Dictionary<char, byte[]> _glyphs = new();

    public BitmapFont()
    {
        AddRange(FontData);
    }

    public float Measure(string text, float scale)
    {
        float width = 0;
        foreach (char c in text)
            width += (c == ' ' ? 3 : GlyphWidth) + 1;
        return (width - 1) * scale;
    }

    public void Draw(Renderer renderer, string text, float x, float y, float scale, Vector4 color)
    {
        float cx = x;
        foreach (char c in text)
        {
            if (c == ' ')
            {
                cx += 4 * scale;
                continue;
            }
            if (!_glyphs.TryGetValue(c, out var glyph))
                continue;

            for (int col = 0; col < GlyphWidth; col++)
            {
                byte bits = glyph[col];
                for (int row = 0; row < GlyphHeight; row++)
                {
                    if ((bits & (1 << row)) != 0)
                    {
                        renderer.DrawQuad(
                            cx + col * scale,
                            y + row * scale,
                            scale,
                            scale,
                            color);
                    }
                }
            }
            cx += (GlyphWidth + 1) * scale;
        }
    }

    private void AddRange(string data)
    {
        // Each line: "char: b0 b1 b2 b3 b4"
        foreach (string line in data.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            string trimmed = line.Trim();
            if (trimmed.Length < 12)
                continue;
            char c = trimmed[0];
            var parts = trimmed.Substring(2).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5)
                continue;
            var bytes = new byte[5];
            for (int i = 0; i < 5; i++)
                bytes[i] = Convert.ToByte(parts[i], 16);
            _glyphs[c] = bytes;
        }
    }

    // Public domain font5x7 (Adafruit GFX / GLCD), plus German umlauts.
    private const string FontData = """
        : 00 00 00 00 00
        ! 00 00 5F 00 00
        " 00 07 00 07 00
        # 14 7F 14 7F 14
        $ 24 2A 7F 2A 12
        % 23 13 08 64 62
        & 36 49 55 22 50
        ' 00 05 03 00 00
        ( 00 1C 22 41 00
        ) 00 41 22 1C 00
        * 08 2A 1C 2A 08
        + 08 08 3E 08 08
        , 00 50 30 00 00
        - 08 08 08 08 08
        . 00 60 60 00 00
        / 20 10 08 04 02
        0 3E 51 49 45 3E
        1 00 42 7F 40 00
        2 42 61 51 49 46
        3 21 41 45 4B 31
        4 18 14 12 7F 10
        5 27 45 45 45 39
        6 3C 4A 49 49 30
        7 01 71 09 05 03
        8 36 49 49 49 36
        9 06 49 49 29 1E
        : 00 36 36 00 00
        ; 00 56 36 00 00
        < 00 08 14 22 41
        = 14 14 14 14 14
        > 41 22 14 08 00
        ? 02 01 51 09 06
        @ 32 49 79 41 3E
        A 7E 11 11 11 7E
        B 7F 49 49 49 36
        C 3E 41 41 41 22
        D 7F 41 41 22 1C
        E 7F 49 49 49 41
        F 7F 09 09 09 01
        G 3E 41 49 49 7A
        H 7F 08 08 08 7F
        I 00 41 7F 41 00
        J 20 40 41 3F 01
        K 7F 08 14 22 41
        L 7F 40 40 40 40
        M 7F 02 0C 02 7F
        N 7F 04 08 10 7F
        O 3E 41 41 41 3E
        P 7F 09 09 09 06
        Q 3E 41 51 21 5E
        R 7F 09 19 29 46
        S 46 49 49 49 31
        T 01 01 7F 01 01
        U 3F 40 40 40 3F
        V 1F 20 40 20 1F
        W 3F 40 38 40 3F
        X 63 14 08 14 63
        Y 07 08 70 08 07
        Z 61 51 49 45 43
        [ 00 7F 41 41 00
        \ 02 04 08 10 20
        ] 00 41 41 7F 00
        ^ 04 02 01 02 04
        _ 40 40 40 40 40
        ` 00 01 02 04 00
        a 20 54 54 54 78
        b 7F 48 44 44 38
        c 38 44 44 44 20
        d 38 44 44 48 7F
        e 38 54 54 54 18
        f 08 7E 09 01 02
        g 0C 52 52 52 3E
        h 7F 08 04 04 78
        i 00 44 7D 40 00
        j 20 40 44 3D 00
        k 7F 10 28 44 00
        l 00 41 7F 40 00
        m 7C 04 18 04 78
        n 7C 08 04 04 78
        o 38 44 44 44 38
        p 7C 14 14 14 08
        q 08 14 14 18 7C
        r 7C 08 04 04 08
        s 48 54 54 54 20
        t 04 3F 44 40 20
        u 3C 40 40 20 7C
        v 1C 20 40 20 1C
        w 3C 40 30 40 3C
        x 44 28 10 28 44
        y 0C 50 50 50 3C
        z 44 64 54 4C 44
        { 00 08 36 41 00
        | 00 00 7F 00 00
        } 00 41 36 08 00
        ~ 08 04 08 10 08
        Ä 7E 11 7E 11 7E
        Ö 3E 41 3E 41 3E
        Ü 3F 40 3F 40 3F
        ä 20 54 78 54 78
        ö 38 44 38 44 38
        ü 3C 40 3C 40 3C
        ß 7C 14 14 14 08
        """;
}
