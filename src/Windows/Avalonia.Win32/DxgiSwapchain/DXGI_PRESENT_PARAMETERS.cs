using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.DxgiSwapchain
{
    internal unsafe struct DXGI_PRESENT_PARAMETERS
    {
        public uint DirtyRectsCount;

        public RECT* pDirtyRects;

        public RECT* pScrollRect;

        public POINT* pScrollOffset;
    }
}
