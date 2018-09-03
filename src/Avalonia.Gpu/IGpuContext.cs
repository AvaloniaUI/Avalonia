using System;

namespace Avalonia.Gpu
{
    public interface IGpuContext
    {
        void Present();
        IntPtr GetProcAddress(string symbol);
        (double, double) GetFramebufferSize();
        void ResizeContext(double width, double height);
        void MakeCurrent();
        (double, double) GetDpi();
    }
}
