using System;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform.Interop;

namespace Avalonia.LinuxFramebuffer.Output
{
    /// <summary>
    ///     Provides the base handle to the DRM output and its associated EGL interface.
    /// </summary>
    /// <remarks>
    ///     Shared resource creates a separation of the <see cref="IPlatformOpenGlInterface" /> instance, and
    ///     the <see cref="DrmOutput" /> instances, which provide the <see cref="IGlPlatformSurface" /> for each DRM connector.
    /// </remarks>
    internal class DrmPlatform
    {
        public DrmPlatform(string path = null, double scaling = 1.0)
            : this(new DrmCard(path), scaling)
        {
        }

        public DrmPlatform(DrmCard card, double scaling = 1.0)
        {
            Scaling = scaling;
            Card = card;
            EglPlatformInterface = new EglPlatformOpenGlInterface(new EglDisplay(new EglInterface(eglGetProcAddress),
                false, 0x31D7, Card.GbmDevice, null));
        }

        public double Scaling { get; set; }

        public DrmCard Card { get; }

        internal EglPlatformOpenGlInterface EglPlatformInterface { get; }

        public DrmOutput CreateOutput()
        {
            var resources = Card.GetResources();

            var connector =
                resources.Connectors.LastOrDefault(x => x.Connection == DrmModeConnection.DRM_MODE_CONNECTED);
            if (connector == null)
                throw new InvalidOperationException("Unable to find connected DRM connector");

            var mode = connector.Modes.OrderByDescending(x => x.IsPreferred)
                .ThenByDescending(x => x.Resolution.Width * x.Resolution.Height)
                .FirstOrDefault();

            if (mode == null)
                throw new InvalidOperationException("Unable to find a usable DRM mode");

            return CreateOutput(resources, connector, mode);
        }

        public DrmOutput CreateOutput(DrmResources resources, DrmConnector connector, DrmModeInfo mode)
        {
            return new DrmOutput(this, resources, connector, mode) { Scaling = Scaling };
        }

        [DllImport("libEGL.so.1")]
        private static extern IntPtr eglGetProcAddress(Utf8Buffer proc);
    }
}
