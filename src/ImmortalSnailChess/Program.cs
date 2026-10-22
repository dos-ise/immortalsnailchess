using ImmortalSnailChess.Renderer;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using RendererNS = ImmortalSnailChess.Renderer.Renderer;

namespace ImmortalSnailChess;

public static class Program
{
    private static IWindow _window = null!;
    private static RendererNS _renderer = null!;
    private static BitmapFont _font = null!;
    private static GameView _gameView = null!;

    public static void Main()
    {
        var options = new WindowOptions(
            true,
            new Vector2D<int>(100, 100),
            new Vector2D<int>(900, 700),
            60.0,
            60.0,
            GraphicsAPI.Default,
            "Immortal Snail Chess",
            WindowState.Normal,
            WindowBorder.Resizable,
            true,
            false,
            new VideoMode(),
            24);

        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Update += OnUpdate;
        _window.Resize += OnResize;
        _window.Closing += OnClosing;
        _window.Run();
    }

    private static void OnLoad()
    {
        State.Init(_window);
        _renderer = new RendererNS(State.Gl);
        _font = new BitmapFont();
        _gameView = new GameView(_renderer, _font);

        _renderer.Resize(_window.Size.X, _window.Size.Y);
        _gameView.Resize(_window.Size.X, _window.Size.Y);

        var input = _window.CreateInput();
        input.Mice[0].MouseDown += OnMouseDown;
        input.Keyboards[0].KeyDown += OnKeyDown;
    }

    private static void OnRender(double dt)
    {
        _gameView.Render();
        _window.SwapBuffers();
    }

    private static void OnUpdate(double dt)
    {
        _gameView.Update(dt);
    }

    private static void OnResize(Vector2D<int> size)
    {
        if (_renderer == null)
            return;
        _renderer.Resize(size.X, size.Y);
        _gameView.Resize(size.X, size.Y);
    }

    private static void OnClosing()
    {
        _gameView?.Dispose();
        _renderer?.Dispose();
    }

    private static void OnMouseDown(IMouse mouse, MouseButton button)
    {
        if (button == MouseButton.Left)
            _gameView.Tap(mouse.Position.X, mouse.Position.Y);
    }

    private static void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        _gameView.HandleKey(key);
    }
}
