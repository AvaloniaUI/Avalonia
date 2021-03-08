using System;
using System.ComponentModel;

namespace Avalonia.LinuxFramebuffer.Output
{
    public sealed class GbmDevice : IDisposable
    {
        private IntPtr _gbmDevice;

        public GbmDevice(DrmCard drmCard)
        {
            _gbmDevice = LibDrm.gbm_create_device(drmCard.Fd);
            
            if (_gbmDevice == IntPtr.Zero)
                throw new Win32Exception("Could not create GBM Device for DRM card " + drmCard.Path);
        }

        ~GbmDevice()
        {
            ReleaseUnmanagedResources();
        }

        public IntPtr Handle => _gbmDevice;

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        internal IntPtr CreateSurface(int width, int height, uint format, LibDrm.GbmBoFlags flags)
        {
            return LibDrm.gbm_surface_create(_gbmDevice, width, height, format, flags);
        }

        private void ReleaseUnmanagedResources()
        {
            if (_gbmDevice != IntPtr.Zero)
            {
                LibDrm.gbm_device_destroy(_gbmDevice);
                _gbmDevice = IntPtr.Zero;
            }
        }
    }
}
