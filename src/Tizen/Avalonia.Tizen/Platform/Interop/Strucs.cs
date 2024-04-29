using System.Runtime.InteropServices;

namespace Avalonia.Tizen.Platform.Interop;

struct TexturedQuadVertex
{
    public Vec2 position;
};

[StructLayout(LayoutKind.Sequential)]
struct Vec2
{
    float x;
    float y;
    public Vec2(float xIn, float yIn)
    {
        x = xIn;
        y = yIn;
    }
}
