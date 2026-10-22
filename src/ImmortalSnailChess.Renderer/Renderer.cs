using Silk.NET.OpenGL;
using System.Numerics;

namespace ImmortalSnailChess.Renderer;

/// <summary>
/// A minimal 2D batch renderer: colored triangles/quads/text drawn with a
/// single shader and one dynamic vertex buffer. Coordinates are in pixels
/// with the origin at the top-left corner.
/// </summary>
public class Renderer : IDisposable
{
    private readonly GL _gl;
    private uint _program;
    private uint _vao;
    private uint _vbo;
    private int _projectionLocation;
    private readonly List<float> _vertices = new(4096);
    private Matrix4x4 _projection = Matrix4x4.Identity;
    private bool _disposed;

    public Renderer(GL gl)
    {
        _gl = gl;
        CreateShader();
        CreateBuffers();
    }

    public void Resize(int width, int height)
    {
        _projection = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);
        _gl.Viewport(0, 0, (uint)width, (uint)height);
    }

    public void BeginFrame()
    {
        _vertices.Clear();
    }

    public void DrawQuad(float x, float y, float w, float h, Vector4 color)
    {
        AddVertex(x, y, color);
        AddVertex(x + w, y, color);
        AddVertex(x, y + h, color);
        AddVertex(x, y + h, color);
        AddVertex(x + w, y, color);
        AddVertex(x + w, y + h, color);
    }

    public void DrawQuadOutline(float x, float y, float w, float h, float thickness, Vector4 color)
    {
        DrawQuad(x, y, w, thickness, color);
        DrawQuad(x, y + h - thickness, w, thickness, color);
        DrawQuad(x, y, thickness, h, color);
        DrawQuad(x + w - thickness, y, thickness, h, color);
    }

    public void DrawCircle(float cx, float cy, float radius, Vector4 color, int segments = 28)
    {
        for (int i = 0; i < segments; i++)
        {
            float a0 = i * MathF.Tau / segments;
            float a1 = (i + 1) * MathF.Tau / segments;
            AddVertex(cx, cy, color);
            AddVertex(cx + MathF.Cos(a0) * radius, cy + MathF.Sin(a0) * radius, color);
            AddVertex(cx + MathF.Cos(a1) * radius, cy + MathF.Sin(a1) * radius, color);
        }
    }

    public void DrawRing(float cx, float cy, float radius, float thickness, Vector4 color, int segments = 28)
    {
        float inner = radius - thickness;
        for (int i = 0; i < segments; i++)
        {
            float a0 = i * MathF.Tau / segments;
            float a1 = (i + 1) * MathF.Tau / segments;
            float c0 = MathF.Cos(a0), s0 = MathF.Sin(a0);
            float c1 = MathF.Cos(a1), s1 = MathF.Sin(a1);
            AddVertex(cx + c0 * inner, cy + s0 * inner, color);
            AddVertex(cx + c0 * radius, cy + s0 * radius, color);
            AddVertex(cx + c1 * radius, cy + s1 * radius, color);
            AddVertex(cx + c1 * radius, cy + s1 * radius, color);
            AddVertex(cx + c1 * inner, cy + s1 * inner, color);
            AddVertex(cx + c0 * inner, cy + s0 * inner, color);
        }
    }

    public void DrawTriangle(float x0, float y0, float x1, float y1, float x2, float y2, Vector4 color)
    {
        AddVertex(x0, y0, color);
        AddVertex(x1, y1, color);
        AddVertex(x2, y2, color);
    }

    public void DrawDiamond(float cx, float cy, float halfWidth, float halfHeight, Vector4 color)
    {
        AddVertex(cx, cy - halfHeight, color);
        AddVertex(cx + halfWidth, cy, color);
        AddVertex(cx, cy + halfHeight, color);
        AddVertex(cx, cy + halfHeight, color);
        AddVertex(cx - halfWidth, cy, color);
        AddVertex(cx, cy - halfHeight, color);
    }

    public void DrawText(BitmapFont font, string text, float x, float y, float scale, Vector4 color)
    {
        font.Draw(this, text, x, y, scale, color);
    }

    public void EndFrame()
    {
        if (_vertices.Count == 0)
            return;

        _gl.UseProgram(_program);
        unsafe
        {
            // System.Numerics stores matrices in row-major order (M11..M44).
            // OpenGL's glUniformMatrix4fv expects column-major storage, which
            // for this layout is exactly what transpose = false produces.
            float* m = stackalloc float[16];
            m[0] = _projection.M11; m[1] = _projection.M12; m[2] = _projection.M13; m[3] = _projection.M14;
            m[4] = _projection.M21; m[5] = _projection.M22; m[6] = _projection.M23; m[7] = _projection.M24;
            m[8] = _projection.M31; m[9] = _projection.M32; m[10] = _projection.M33; m[11] = _projection.M34;
            m[12] = _projection.M41; m[13] = _projection.M42; m[14] = _projection.M43; m[15] = _projection.M44;
            _gl.UniformMatrix4(_projectionLocation, 1, false, m);
        }

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(_vertices.Count * sizeof(float)), _vertices.ToArray(), BufferUsageARB.StreamDraw);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)(_vertices.Count / 6));
    }

    private void AddVertex(float x, float y, Vector4 color)
    {
        _vertices.Add(x);
        _vertices.Add(y);
        _vertices.Add(color.X);
        _vertices.Add(color.Y);
        _vertices.Add(color.Z);
        _vertices.Add(color.W);
    }

    private void CreateShader()
    {
        string version = State.GetShaderVersionDirective();
        string precision = State.IsOpenGLES ? "precision mediump float;\n" : "";

        string vertexSource = version + "\n" +
            "layout(location = 0) in vec2 aPos;\n" +
            "layout(location = 1) in vec4 aColor;\n" +
            "uniform mat4 uProjection;\n" +
            "out vec4 vColor;\n" +
            "void main() {\n" +
            "    gl_Position = uProjection * vec4(aPos, 0.0, 1.0);\n" +
            "    vColor = aColor;\n" +
            "}\n";

        string fragmentSource = version + "\n" +
            precision +
            "in vec4 vColor;\n" +
            "out vec4 fragColor;\n" +
            "void main() {\n" +
            "    fragColor = vColor;\n" +
            "}\n";

        uint vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
        uint fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

        _program = _gl.CreateProgram();
        _gl.AttachShader(_program, vertexShader);
        _gl.AttachShader(_program, fragmentShader);
        _gl.LinkProgram(_program);

        _gl.GetProgram(_program, ProgramPropertyARB.LinkStatus, out int status);
        if (status == 0)
        {
            string log = _gl.GetProgramInfoLog(_program);
            throw new InvalidOperationException($"Shader link failed: {log}");
        }

        _gl.DetachShader(_program, vertexShader);
        _gl.DetachShader(_program, fragmentShader);
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        _projectionLocation = _gl.GetUniformLocation(_program, "uProjection");
    }

    private uint CompileShader(ShaderType type, string source)
    {
        uint shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
        if (status == 0)
        {
            string log = _gl.GetShaderInfoLog(shader);
            throw new InvalidOperationException($"Shader compile failed: {log}");
        }
        return shader;
    }

    private unsafe void CreateBuffers()
    {
        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)(2 * sizeof(float)));
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteProgram(_program);
    }
}
