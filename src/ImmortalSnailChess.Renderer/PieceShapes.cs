using ImmortalSnailChess.Core;
using System.Numerics;

namespace ImmortalSnailChess.Renderer;

/// <summary>
/// Draws chess pieces and snails as simple geometric shapes within a cell.
/// </summary>
public static class PieceShapes
{
    private static readonly Vector4 WhiteFill = new(0.96f, 0.95f, 0.90f, 1f);
    private static readonly Vector4 WhiteOutline = new(0.18f, 0.18f, 0.20f, 1f);
    private static readonly Vector4 BlackFill = new(0.16f, 0.16f, 0.19f, 1f);
    private static readonly Vector4 BlackOutline = new(0.92f, 0.92f, 0.90f, 1f);

    private static readonly Vector4 SnailShell = new(0.45f, 0.62f, 0.30f, 1f);
    private static readonly Vector4 SnailShellDark = new(0.28f, 0.40f, 0.18f, 1f);
    private static readonly Vector4 SnailBody = new(0.85f, 0.72f, 0.45f, 1f);
    private static readonly Vector4 SnailOutline = new(0.10f, 0.12f, 0.08f, 1f);

    public static void DrawPiece(Renderer r, PieceType type, PieceColor color, float x, float y, float cell)
    {
        float cx = x + cell * 0.5f;
        float cy = y + cell * 0.5f;
        var fill = color == PieceColor.White ? WhiteFill : BlackFill;
        var outline = color == PieceColor.White ? WhiteOutline : BlackOutline;

        switch (type)
        {
            case PieceType.Pawn:
                DrawCircle(r, cx, cy, cell * 0.30f, fill, outline);
                break;
            case PieceType.Rook:
                DrawSquare(r, cx, cy, cell * 0.52f, fill, outline);
                break;
            case PieceType.Knight:
                DrawTriangle(r, cx, cy, cell * 0.60f, fill, outline);
                break;
            case PieceType.Bishop:
                DrawDiamond(r, cx, cy, cell * 0.40f, cell * 0.52f, fill, outline);
                break;
            case PieceType.Queen:
                DrawCircle(r, cx, cy + cell * 0.06f, cell * 0.30f, fill, outline);
                // small crown on top
                DrawTriangle(r, cx - cell * 0.16f, cy - cell * 0.10f, cell * 0.16f, fill, outline);
                DrawTriangle(r, cx, cy - cell * 0.16f, cell * 0.16f, fill, outline);
                DrawTriangle(r, cx + cell * 0.16f, cy - cell * 0.10f, cell * 0.16f, fill, outline);
                break;
            case PieceType.King:
                DrawCross(r, cx, cy, cell * 0.52f, cell * 0.18f, fill, outline);
                break;
        }
    }

    public static void DrawSnail(Renderer r, PieceColor color, float x, float y, float cell)
    {
        float cx = x + cell * 0.5f;
        float cy = y + cell * 0.5f;

        // Body (head) on the left, shell on the right.
        DrawCircle(r, cx - cell * 0.20f, cy + cell * 0.10f, cell * 0.16f, SnailBody, SnailOutline);
        // Shell
        DrawCircle(r, cx + cell * 0.10f, cy, cell * 0.34f, SnailShell, SnailOutline);
        // Spiral
        DrawCircle(r, cx + cell * 0.10f, cy, cell * 0.16f, SnailShellDark, SnailOutline);
        DrawCircle(r, cx + cell * 0.10f, cy, cell * 0.06f, SnailBody, SnailOutline);
    }

    private static void DrawCircle(Renderer r, float cx, float cy, float radius, Vector4 fill, Vector4 outline)
    {
        r.DrawCircle(cx, cy, radius * 1.18f, outline);
        r.DrawCircle(cx, cy, radius, fill);
    }

    private static void DrawSquare(Renderer r, float cx, float cy, float size, Vector4 fill, Vector4 outline)
    {
        float half = size * 0.5f;
        r.DrawQuad(cx - half * 1.12f, cy - half * 1.12f, size * 1.12f, size * 1.12f, outline);
        r.DrawQuad(cx - half, cy - half, size, size, fill);
    }

    private static void DrawTriangle(Renderer r, float cx, float cy, float size, Vector4 fill, Vector4 outline)
    {
        float half = size * 0.5f;
        // Outline (slightly larger)
        r.DrawTriangle(cx, cy - half * 1.15f, cx - half * 1.15f, cy + half * 1.15f, cx + half * 1.15f, cy + half * 1.15f, outline);
        r.DrawTriangle(cx, cy - half, cx - half, cy + half, cx + half, cy + half, fill);
    }

    private static void DrawDiamond(Renderer r, float cx, float cy, float halfW, float halfH, Vector4 fill, Vector4 outline)
    {
        r.DrawDiamond(cx, cy, halfW * 1.15f, halfH * 1.15f, outline);
        r.DrawDiamond(cx, cy, halfW, halfH, fill);
    }

    private static void DrawCross(Renderer r, float cx, float cy, float size, float thickness, Vector4 fill, Vector4 outline)
    {
        float half = size * 0.5f;
        float t = thickness * 0.5f;
        // Outline
        r.DrawQuad(cx - t * 1.2f, cy - half * 1.1f, thickness * 1.2f, size * 1.1f, outline);
        r.DrawQuad(cx - half * 1.1f, cy - t * 1.2f, size * 1.1f, thickness * 1.2f, outline);
        // Fill
        r.DrawQuad(cx - t, cy - half, thickness, size, fill);
        r.DrawQuad(cx - half, cy - t, size, thickness, fill);
    }
}
