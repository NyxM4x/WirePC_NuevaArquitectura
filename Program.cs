using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace WirePC;

class Program
{
    static void Main()
    {
        var native = new NativeWindowSettings
        {
            ClientSize = new Vector2i(1366, 768),
            Title = "Pc_Lineas"
        };
        using var win = new RendererWindow(GameWindowSettings.Default, native);
        win.Run();
    }
}
