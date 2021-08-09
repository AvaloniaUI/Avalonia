using System;
using Avalonia.MicroCom;
using Avalonia.Win32.DirectX;

namespace Avalonia.Win32.OpenGl
{
    class WglD3DInterop : IDisposable
    {
        private readonly WglContext _context;
        public ID3D11Device Device { get; }
        public IntPtr GlDevice { get; }

        public WglD3DInterop(WglContext context)
        {
            _context = context;
            using var factory = DirectXUnmanagedMethods.CreateDxgiFactory();
            using var adapter = factory.EnumAdapters(0);
            Device = DirectXUnmanagedMethods.CreateDevice(adapter,
                new[]
                {
                    D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0, D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_1,
                    D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_0
                });

            using (context.EnsureCurrent())
                GlDevice = context.WglInterface.DXOpenDeviceNV(Device.GetNativeIntPtr());
        }

        public void Dispose()
        {
            using (_context.EnsureCurrent())
                _context.WglInterface.DXCloseDeviceNV(GlDevice);
            Device.Dispose();
        }
    }
}
