using Android.App;
using Android.Content.PM;
using ImmortalSnailChess.Renderer;
using Silk.NET.Input;
using Silk.NET.Input.Sdl;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl;
using Silk.NET.Windowing.Sdl.Android;
using RendererNS = ImmortalSnailChess.Renderer.Renderer;

namespace ImmortalSnailChess.Android;

[Activity(Label = "@string/app_name", MainLauncher = true,
    ScreenOrientation = ScreenOrientation.Landscape, Exported = true)]
public class MainActivity : SilkActivity
{
    private static IView view = null!;
    private static RendererNS renderer = null!;
    private static BitmapFont font = null!;
    private static GameView gameView = null!;

    protected override void OnRun()
    {
        try
        {
            SdlWindowing.RegisterPlatform();
            SdlInput.RegisterPlatform();
            SdlWindowing.Use();

            // On Android the swap does not block on vsync, so cap the loop to
            // avoid draining the battery.
            var options = new WindowOptions(
                true,
                new Vector2D<int>(0, 0),
                new Vector2D<int>(1280, 720),
                50.0,
                50.0,
                new GraphicsAPI(ContextAPI.OpenGLES, ContextProfile.Compatability, ContextFlags.Default, new APIVersion(3, 0)),
                "Immortal Snail Chess",
                WindowState.Normal,
                WindowBorder.Fixed,
                false,
                false,
                new VideoMode(60),
                24);

            view = Silk.NET.Windowing.Window.GetView(new ViewOptions(options));
            view.Load += Window_Load;
            view.Render += Window_Render;
            view.Update += Window_Update;
            view.Resize += Window_Resize;
            view.Closing += Window_Closing;

            view.Initialize();
            view.Run(() =>
            {
                view.DoEvents();
                if (!view.IsClosing)
                    view.DoUpdate();
                if (!view.IsClosing)
                    view.DoRender();
            });
            view.Reset();
        }
        finally
        {
            view?.Dispose();
        }
    }

    private static void Window_Load()
    {
        view.MakeCurrent();
        State.Init(view);
        renderer = new RendererNS(State.Gl);
        font = new BitmapFont();
        gameView = new GameView(renderer, font);

        renderer.Resize(view.Size.X, view.Size.Y);
        gameView.Resize(view.Size.X, view.Size.Y);

        var input = view.CreateInput();
        input.Mice[0].MouseDown += OnMouseDown;
        input.Keyboards[0].KeyDown += OnKeyDown;
    }

    private static void Window_Render(double dt)
    {
        gameView.Render();
        view.SwapBuffers();
    }

    private static void Window_Update(double dt)
    {
        gameView.Update(dt);
    }

    private static void Window_Resize(Vector2D<int> size)
    {
        if (renderer == null)
            return;
        renderer.Resize(size.X, size.Y);
        gameView.Resize(size.X, size.Y);
    }

    private static void Window_Closing()
    {
        gameView?.Dispose();
        renderer?.Dispose();
    }

    private static void OnMouseDown(IMouse mouse, MouseButton button)
    {
        if (button == MouseButton.Left)
            gameView.Tap(mouse.Position.X, mouse.Position.Y);
    }

    private static void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        gameView.HandleKey(key);
    }
}
