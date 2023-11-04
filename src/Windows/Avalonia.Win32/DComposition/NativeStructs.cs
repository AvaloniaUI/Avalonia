using System.Runtime.InteropServices;
using Avalonia.Win32.DirectX;

namespace Avalonia.Win32.DComposition;

[StructLayout(LayoutKind.Sequential)]
public struct DXGI_RATIONAL
{
    public uint Numerator;
    public uint Denominator;
}

[StructLayout(LayoutKind.Sequential)]
public struct DCOMPOSITION_FRAME_STATISTICS
{
    public long lastFrameTime;
    public DXGI_RATIONAL currentCompositionRate;
    public long currentTime;
    public long timeFrequency;
    public long nextEstimatedFrameTime;
}
