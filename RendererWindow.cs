using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace WirePC;

public class RendererWindow : GameWindow
{
    int _vao, _vbo, _shader;
    int _uMvp, _uColor;
    float[] _lineVerts = Array.Empty<float>();

    public RendererWindow(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.ClearColor(0.08f, 0.09f, 0.11f, 1f);
        GL.Enable(EnableCap.DepthTest);
        GL.LineWidth(2f);
        GL.Viewport(0, 0, Size.X, Size.Y);

        _shader = CreateShader(VertexSrc, FragmentSrc);
        _uMvp = GL.GetUniformLocation(_shader, "uMVP");
        _uColor = GL.GetUniformLocation(_shader, "uColor");

        // Construir escena (nueva arquitectura)
        var escena = new EscenaNuevaArquitectura();
        _lineVerts = escena.BuildWireframeVertices();
        Console.WriteLine($"[DEBUG] verts={_lineVerts.Length/3}");

        // VBO/VAO
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, _lineVerts.Length * sizeof(float), _lineVerts, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.BindVertexArray(0);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, Size.X, Size.Y);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var aspect = Size.X / (float)Size.Y;
        var proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60f), aspect, 0.1f, 100f);
        var view = Matrix4.LookAt(new Vector3(2.4f, 1.8f, 3.2f), Vector3.Zero, Vector3.UnitY);
        var model = Matrix4.Identity;

        // **MVP en el mismo orden que tu proyecto que sí "ve" la escena: model * view * proj**
        var mvp = model * view * proj;

        GL.UseProgram(_shader);
        GL.UniformMatrix4(_uMvp, false, ref mvp);
        GL.Uniform3(_uColor, new Vector3(0.95f, 0.9f, 0.5f));

        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Lines, 0, _lineVerts.Length / 3);
        GL.BindVertexArray(0);

        SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        if (KeyboardState.IsKeyDown(Keys.Escape)) Close();
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
        GL.DeleteProgram(_shader);
    }

    const string VertexSrc = @"
#version 330 core
layout (location = 0) in vec3 aPosition;
uniform mat4 uMVP;
void main()
{
    gl_Position = uMVP * vec4(aPosition, 1.0);
}";

    const string FragmentSrc = @"
#version 330 core
out vec4 FragColor;
uniform vec3 uColor;
void main()
{
    FragColor = vec4(uColor, 1.0);
}";

    static int CreateShader(string vsSrc, string fsSrc)
    {
        int vs = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vs, vsSrc);
        GL.CompileShader(vs);
        GL.GetShader(vs, ShaderParameter.CompileStatus, out int vStatus);
        if (vStatus == 0) throw new Exception("VS: " + GL.GetShaderInfoLog(vs));

        int fs = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fs, fsSrc);
        GL.CompileShader(fs);
        GL.GetShader(fs, ShaderParameter.CompileStatus, out int fStatus);
        if (fStatus == 0) throw new Exception("FS: " + GL.GetShaderInfoLog(fs));

        int prog = GL.CreateProgram();
        GL.AttachShader(prog, vs);
        GL.AttachShader(prog, fs);
        GL.LinkProgram(prog);
        GL.GetProgram(prog, GetProgramParameterName.LinkStatus, out int linkStatus);
        if (linkStatus == 0) throw new Exception("LINK: " + GL.GetProgramInfoLog(prog));

        GL.DeleteShader(vs);
        GL.DeleteShader(fs);
        return prog;
    }
}
