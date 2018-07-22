using System;

namespace Avalonia.Gpu
{
    public interface IGpuContext
    {
        void Present();
        IntPtr GetProcAddress(string symbol);
        (double, double) GetFramebufferSize();
    }
}
