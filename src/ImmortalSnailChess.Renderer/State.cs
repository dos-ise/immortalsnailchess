using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using System.Text.RegularExpressions;

namespace ImmortalSnailChess.Renderer;

/// <summary>
/// Global OpenGL state. Detects whether the context is desktop OpenGL or
/// OpenGL ES and provides the matching shader #version directive.
/// </summary>
public static class State
{
    public static GL Gl { get; private set; } = null!;
    public static bool IsOpenGLES { get; private set; }
    public static int GLSLVersionMajor { get; private set; }
    public static int GLSLVersionMinor { get; private set; }

    public static void Init(IGLContextSource contextSource)
    {
        Gl = GL.GetApi(contextSource);

        var openGLVersion = Gl.GetStringS(StringName.Version).TrimStart();
        var versionRegex = new Regex(@"([0-9]+)\.([0-9]+)", RegexOptions.Compiled);
        var match = versionRegex.Match(openGLVersion);

        if (!match.Success || match.Groups.Count < 3)
            throw new InvalidOperationException("OpenGL is not supported or the version could not be determined.");

        IsOpenGLES = openGLVersion.StartsWith("OpenGL ES", StringComparison.OrdinalIgnoreCase);

        var glslVersion = Gl.GetStringS(StringName.ShadingLanguageVersion);
        match = versionRegex.Match(glslVersion);
        if (match.Success && match.Groups.Count >= 3)
        {
            GLSLVersionMajor = int.Parse(match.Groups[1].Value);
            GLSLVersionMinor = int.Parse(match.Groups[2].Value);
        }

        if (GLSLVersionMajor == 0)
            throw new NotSupportedException("The graphics driver does not provide GLSL shaders.");
    }

    /// <summary>
    /// The #version directive for shaders, e.g. "#version 330" on desktop or
    /// "#version 300 es" on OpenGL ES.
    /// </summary>
    public static string GetShaderVersionDirective() =>
        $"#version {GLSLVersionMajor}{GLSLVersionMinor:00}{(IsOpenGLES ? " es" : "")}";
}
