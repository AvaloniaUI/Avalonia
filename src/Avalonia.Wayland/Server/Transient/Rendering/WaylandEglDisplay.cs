using System;
using System.Collections.Generic;
using Avalonia.OpenGL.Egl;
using NWayland.Protocols.LinuxDmabufV1;

namespace Avalonia.Wayland.Server.Transient.Rendering;

internal class WaylandEglDisplay : EglDisplay
{
    public IntPtr GbmDevice { get; }
    public int DrmFd { get; }
    public List<DmabufFormatModifierPair> SupportedFormats { get; }
    public ZwpLinuxDmabufV1 LinuxDmabuf { get; }
    
    public WaylandEglDisplay(
        EglDisplayCreationOptions options,
        IntPtr gbmDevice,
        int drmFd,
        ZwpLinuxDmabufV1 linuxDmabuf) : base(options)
    {
        GbmDevice = gbmDevice;
        DrmFd = drmFd;
        SupportedFormats = new List<DmabufFormatModifierPair>();
        LinuxDmabuf = linuxDmabuf;
    }
}
