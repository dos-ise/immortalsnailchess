using ImmortalSnailChess.Core;
using ImmortalSnailChess.Core.AI;
using Silk.NET.Input;
using System.Numerics;

namespace ImmortalSnailChess.Renderer;

/// <summary>
/// The shared game view: game state, input handling and rendering. Used by
/// both the desktop (GLFW) and Android (SDL) hosts.
/// </summary>
public class GameView : IDisposable
{
    public enum Mode
    {
        Menu,
        Hotseat,
        VsAI
    }

    private const float StatusBarHeight = 52f;
    private const float BottomBarHeight = 60f;
    private const float BoardMargin = 12f;

    private readonly Renderer _renderer;
    private readonly BitmapFont _font;

    private GameState _state = null!;
    private Mode _mode = Mode.Menu;
    private PieceColor _humanColor = PieceColor.White;
    private int? _selected;
    private List<Move> _targets = new();
    private readonly List<GameState> _history = new();

    private bool _promotionPending;
    private int _promotionFrom;
    private int _promotionTo;
    private readonly (PieceType Type, float X, float Y, float Size)[] _promotionOptions = new (PieceType, float, float, float)[4];

    private int _width = 800;
    private int _height = 600;
    private float _boardX;
    private float _boardY;
    private float _cell;
    private float _boardSize;

    private double _aiDelay;
    private bool _aiThinking;

    public GameView(Renderer renderer, BitmapFont font)
    {
        _renderer = renderer;
        _font = font;
        NewGame(Mode.Hotseat);
    }

    public void Resize(int width, int height)
    {
        _width = Math.Max(320, width);
        _height = Math.Max(320, height);
        ComputeLayout();
    }

    public void Update(double dt)
    {
        if (_mode != Mode.VsAI || _state.Result != GameResult.Ongoing || _promotionPending)
            return;

        if (_state.SideToMove == _humanColor)
            return;

        _aiDelay -= dt;
        if (_aiDelay > 0)
            return;

        if (_aiThinking)
            return;

        _aiThinking = true;
        var move = SimpleAI.FindBestMove(_state, depth: 3);
        _aiThinking = false;

        if (move != null)
        {
            PushHistory();
            Game.MakeMove(_state, move.Value);
            Game.EvaluateState(_state);
            _selected = null;
            _targets.Clear();
            _aiDelay = 0.35;
        }
    }

    public void Render()
    {
        _renderer.BeginFrame();

        // Background
        _renderer.DrawQuad(0, 0, _width, _height, new Vector4(0.10f, 0.11f, 0.13f, 1f));

        if (_mode == Mode.Menu)
        {
            RenderMenu();
        }
        else
        {
            RenderBoard();
            RenderStatus();
            RenderBottomBar();
            if (_promotionPending)
                RenderPromotionPicker();
        }

        _renderer.EndFrame();
    }

    public void Tap(float x, float y)
    {
        if (_mode == Mode.Menu)
        {
            HandleMenuTap(x, y);
            return;
        }

        // Bottom bar buttons (Restart / Undo / Menu) work with mouse and touch.
        if (y >= _height - BottomBarHeight)
        {
            float zone = _width / 3f;
            if (x < zone)
                NewGame(_mode, _humanColor);
            else if (x < zone * 2)
                Undo();
            else
                _mode = Mode.Menu;
            return;
        }

        if (_promotionPending)
        {
            HandlePromotionTap(x, y);
            return;
        }

        if (_state.Result != GameResult.Ongoing)
            return;

        if (_mode == Mode.VsAI && _state.SideToMove != _humanColor)
            return;

        int? square = ScreenToSquare(x, y);
        if (square == null)
            return;

        // If a promotion is pending, ignore board taps.
        if (_selected is int from)
        {
            var move = _targets.FirstOrDefault(m => m.To == square.Value);
            if (move.From != 0 || move.To != 0)
            {
                if (move.Promotion != null)
                {
                    _promotionPending = true;
                    _promotionFrom = move.From;
                    _promotionTo = move.To;
                    return;
                }
                MakeMove(move);
                return;
            }
        }

        // Select or deselect.
        var piece = _state.Board[square.Value];
        if (piece is { } p && p.Color == _state.SideToMove)
        {
            _selected = square;
            _targets = Game.GetLegalMoves(_state).Where(m => m.From == square.Value).ToList();
        }
        else
        {
            _selected = null;
            _targets.Clear();
        }
    }

    public void HandleKey(Key key)
    {
        if (_mode == Mode.Menu)
        {
            switch (key)
            {
                case Key.Number1:
                    NewGame(Mode.Hotseat);
                    break;
                case Key.Number2:
                    NewGame(Mode.VsAI, PieceColor.White);
                    break;
                case Key.Number3:
                    NewGame(Mode.VsAI, PieceColor.Black);
                    break;
            }
            return;
        }

        switch (key)
        {
            case Key.R:
                NewGame(_mode, _humanColor);
                break;
            case Key.U:
                Undo();
                break;
            case Key.Escape:
            case Key.M:
                _mode = Mode.Menu;
                break;
        }
    }

    public void Dispose()
    {
    }

    // ---------- Game flow ----------

    private void NewGame(Mode mode, PieceColor humanColor = PieceColor.White)
    {
        _mode = mode;
        _humanColor = humanColor;
        _state = GameState.Initial();
        _history.Clear();
        _selected = null;
        _targets.Clear();
        _promotionPending = false;
        _aiDelay = 0.5;
        _aiThinking = false;
    }

    private void PushHistory()
    {
        _history.Add(_state.Clone());
        if (_history.Count > 200)
            _history.RemoveAt(0);
    }

    private void Undo()
    {
        if (_history.Count == 0)
            return;
        _state = _history[^1];
        _history.RemoveAt(_history.Count - 1);
        if (_mode == Mode.VsAI && _state.SideToMove != _humanColor && _history.Count > 0)
        {
            _state = _history[^1];
            _history.RemoveAt(_history.Count - 1);
        }
        _selected = null;
        _targets.Clear();
        _promotionPending = false;
        _aiDelay = 0.5;
    }

    private void MakeMove(Move move)
    {
        PushHistory();
        Game.MakeMove(_state, move);
        Game.EvaluateState(_state);
        _selected = null;
        _targets.Clear();
    }

    // ---------- Layout ----------

    private void ComputeLayout()
    {
        float available = Math.Min(_width - 2 * BoardMargin, _height - StatusBarHeight - BottomBarHeight - 2 * BoardMargin);
        _boardSize = Math.Max(200, available);
        _cell = _boardSize / 8f;
        _boardX = (_width - _boardSize) / 2f;
        _boardY = StatusBarHeight + BoardMargin;
    }

    private int? ScreenToSquare(float x, float y)
    {
        if (x < _boardX || x >= _boardX + _boardSize || y < _boardY || y >= _boardY + _boardSize)
            return null;
        int file = (int)((x - _boardX) / _cell);
        int rank = 7 - (int)((y - _boardY) / _cell);
        if (file < 0 || file > 7 || rank < 0 || rank > 7)
            return null;
        return Square.Index(file, rank);
    }

    // ---------- Rendering ----------

    private void RenderMenu()
    {
        float cx = _width / 2f;
        float scale = Math.Max(1.5f, _cell * 0.10f);

        string title = "IMMORTAL SNAIL CHESS";
        float tw = _font.Measure(title, scale);
        _renderer.DrawText(_font, title, cx - tw / 2, _height * 0.18f, scale, new Vector4(0.95f, 0.85f, 0.4f, 1f));

        string[] options = { "1: Hotseat", "2: White vs AI", "3: Black vs AI" };
        float oy = _height * 0.42f;
        float oScale = Math.Max(1.2f, _cell * 0.08f);
        foreach (string option in options)
        {
            float ow = _font.Measure(option, oScale);
            _renderer.DrawText(_font, option, cx - ow / 2, oy, oScale, new Vector4(0.85f, 0.87f, 0.9f, 1f));
            oy += oScale * 12;
        }

        string hint = "Press 1, 2 or 3";
        float hw = _font.Measure(hint, oScale);
        _renderer.DrawText(_font, hint, cx - hw / 2, oy + oScale * 4, oScale, new Vector4(0.55f, 0.58f, 0.62f, 1f));
    }

    private void RenderBoard()
    {
        var light = new Vector4(0.93f, 0.85f, 0.70f, 1f);
        var dark = new Vector4(0.72f, 0.55f, 0.35f, 1f);
        var lastMove = new Vector4(0.95f, 0.85f, 0.30f, 0.45f);
        var selected = new Vector4(0.30f, 0.55f, 0.95f, 0.55f);
        var check = new Vector4(0.90f, 0.20f, 0.20f, 0.8f);
        var dot = new Vector4(0.15f, 0.55f, 0.20f, 0.9f);
        var ring = new Vector4(0.15f, 0.55f, 0.20f, 0.9f);
        var trail = new Vector4(0.40f, 0.80f, 0.40f, 0.5f);

        // Board frame
        _renderer.DrawQuad(_boardX - 4, _boardY - 4, _boardSize + 8, _boardSize + 8, new Vector4(0.25f, 0.22f, 0.18f, 1f));

        int? checkSquare = null;
        if (_state.Result == GameResult.Ongoing && Game.IsInCheck(_state, _state.SideToMove))
            checkSquare = _state.Board.FindKing(_state.SideToMove);

        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                float x = _boardX + file * _cell;
                float y = _boardY + (7 - rank) * _cell;
                var color = (file + rank) % 2 == 0 ? light : dark;
                _renderer.DrawQuad(x, y, _cell, _cell, color);

                int sq = Square.Index(file, rank);

                if (_state.LastMove is Move lm &&
                    (lm.From == sq || lm.To == sq))
                {
                    _renderer.DrawQuad(x, y, _cell, _cell, lastMove);
                }

                if (_state.LastSnailStep is var (sf, st) && (sf == sq || st == sq))
                {
                    _renderer.DrawQuad(x, y, _cell, _cell, trail);
                }

                if (_selected == sq)
                {
                    _renderer.DrawQuad(x, y, _cell, _cell, selected);
                }

                if (checkSquare == sq)
                {
                    _renderer.DrawRing(x + _cell / 2, y + _cell / 2, _cell * 0.46f, _cell * 0.10f, check);
                }
            }
        }

        // Legal move markers
        foreach (var move in _targets)
        {
            int file = Square.File(move.To);
            int rank = Square.Rank(move.To);
            float x = _boardX + file * _cell;
            float y = _boardY + (7 - rank) * _cell;
            bool capture = _state.Board[move.To] != null || move.IsEnPassant;
            if (capture)
                _renderer.DrawRing(x + _cell / 2, y + _cell / 2, _cell * 0.42f, _cell * 0.08f, ring);
            else
                _renderer.DrawCircle(x + _cell / 2, y + _cell / 2, _cell * 0.12f, dot);
        }

        // Pieces and snails
        for (int sq = 0; sq < Board.Size; sq++)
        {
            int file = Square.File(sq);
            int rank = Square.Rank(sq);
            float x = _boardX + file * _cell;
            float y = _boardY + (7 - rank) * _cell;

            if (_state.Board.WhiteSnail == sq)
                PieceShapes.DrawSnail(_renderer, PieceColor.White, x, y, _cell);
            else if (_state.Board.BlackSnail == sq)
                PieceShapes.DrawSnail(_renderer, PieceColor.Black, x, y, _cell);
            else if (_state.Board[sq] is Piece piece)
                PieceShapes.DrawPiece(_renderer, piece.Type, piece.Color, x, y, _cell);
        }

        // Coordinates
        float coordScale = Math.Max(0.6f, _cell * 0.055f);
        var coordColor = new Vector4(0.15f, 0.13f, 0.10f, 1f);
        for (int file = 0; file < 8; file++)
        {
            string label = ((char)('a' + file)).ToString();
            float lw = _font.Measure(label, coordScale);
            _renderer.DrawText(_font, label, _boardX + file * _cell + (_cell - lw) / 2, _boardY + _boardSize + 2, coordScale, coordColor);
        }
        for (int rank = 0; rank < 8; rank++)
        {
            string label = (rank + 1).ToString();
            float lw = _font.Measure(label, coordScale);
            _renderer.DrawText(_font, label, _boardX - lw - 4, _boardY + (7 - rank) * _cell + (_cell - coordScale * 7) / 2, coordScale, coordColor);
        }
    }

    private void RenderStatus()
    {
        float scale = Math.Max(1.0f, _cell * 0.07f);
        string text;
        var color = new Vector4(0.9f, 0.9f, 0.92f, 1f);

        if (_state.Result == GameResult.Ongoing)
        {
            string turn = _state.SideToMove == PieceColor.White ? "White" : "Black";
            text = turn + " to move";
            if (Game.IsInCheck(_state, _state.SideToMove))
            {
                text += " - CHECK!";
                color = new Vector4(1f, 0.5f, 0.4f, 1f);
            }
            int remaining = SnailLogic.StepInterval - _state.MoveCountOf(_state.SideToMove) % SnailLogic.StepInterval;
            text += $"   |   snail in {remaining} move" + (remaining == 1 ? "" : "s");
        }
        else
        {
            text = ResultText();
            color = new Vector4(1f, 0.85f, 0.4f, 1f);
        }

        float tw = _font.Measure(text, scale);
        _renderer.DrawText(_font, text, (_width - tw) / 2, (StatusBarHeight - scale * 7) / 2, scale, color);
    }

    private void RenderBottomBar()
    {
        float scale = Math.Max(0.9f, _cell * 0.06f);
        string[] labels = { "Restart", "Undo", "Menu" };
        float zone = _width / 3f;
        for (int i = 0; i < 3; i++)
        {
            float lw = _font.Measure(labels[i], scale);
            float x = i * zone + (zone - lw) / 2;
            float y = _height - BottomBarHeight + (BottomBarHeight - scale * 7) / 2;
            _renderer.DrawText(_font, labels[i], x, y, scale, new Vector4(0.6f, 0.63f, 0.67f, 1f));
        }
    }

    private void RenderPromotionPicker()
    {
        // Dim the board.
        _renderer.DrawQuad(_boardX, _boardY, _boardSize, _boardSize, new Vector4(0f, 0f, 0f, 0.55f));

        var color = _state.Board[_promotionFrom] is Piece p ? p.Color : PieceColor.White;
        float size = _cell * 0.8f;
        float gap = _cell * 0.2f;
        float total = 4 * size + 3 * gap;
        float x0 = _boardX + (_boardSize - total) / 2;
        float y0 = _boardY + (_boardSize - size) / 2;

        PieceType[] types = { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight };
        for (int i = 0; i < 4; i++)
        {
            float x = x0 + i * (size + gap);
            _renderer.DrawQuad(x - 4, y0 - 4, size + 8, size + 8, new Vector4(0.9f, 0.9f, 0.9f, 1f));
            _renderer.DrawQuad(x, y0, size, size, new Vector4(0.25f, 0.28f, 0.32f, 1f));
            PieceShapes.DrawPiece(_renderer, types[i], color, x, y0, size);
            _promotionOptions[i] = (types[i], x, y0, size);
        }
    }

    private string ResultText()
    {
        return _state.Result switch
        {
            GameResult.WhiteWins => _state.EndReason == GameEndReason.SnailCapturedKing
                ? "The snail crushed the black king - White wins!"
                : "Checkmate - White wins!",
            GameResult.BlackWins => _state.EndReason == GameEndReason.SnailCapturedKing
                ? "The snail crushed the white king - Black wins!"
                : "Checkmate - Black wins!",
            GameResult.Draw => _state.EndReason switch
            {
                GameEndReason.Stalemate => "Stalemate - Draw",
                GameEndReason.FiftyMoveRule => "Draw by 50-move rule",
                GameEndReason.ThreefoldRepetition => "Draw by threefold repetition",
                GameEndReason.InsufficientMaterial => "Draw - insufficient material",
                _ => "Draw"
            },
            _ => ""
        };
    }

    // ---------- Input handling ----------

    private void HandleMenuTap(float x, float y)
    {
        // Three options centered vertically.
        float cx = _width / 2f;
        float oScale = Math.Max(1.2f, _cell * 0.08f);
        float oy = _height * 0.42f;
        string[] options = { "1: Hotseat", "2: White vs AI", "3: Black vs AI" };
        for (int i = 0; i < options.Length; i++)
        {
            float ow = _font.Measure(options[i], oScale);
            float left = cx - ow / 2;
            float top = oy;
            float bottom = oy + oScale * 12;
            if (x >= left && x <= left + ow && y >= top && y <= bottom)
            {
                switch (i)
                {
                    case 0: NewGame(Mode.Hotseat); break;
                    case 1: NewGame(Mode.VsAI, PieceColor.White); break;
                    case 2: NewGame(Mode.VsAI, PieceColor.Black); break;
                }
                return;
            }
            oy += oScale * 12;
        }
    }

    private void HandlePromotionTap(float x, float y)
    {
        foreach (var (type, px, py, size) in _promotionOptions)
        {
            if (x >= px - 4 && x <= px + size + 4 && y >= py - 4 && y <= py + size + 4)
            {
                var move = new Move(_promotionFrom, _promotionTo, type, MoveFlags.Promotion);
                _promotionPending = false;
                MakeMove(move);
                return;
            }
        }
    }
}
