using System;

namespace Avalonia.Controls.Platform
{
    /// <summary>
    /// This file is needed to represent how awesome Windows is
    /// See https://stackoverflow.com/questions/53000291/how-to-smooth-ugly-jitter-flicker-jumping-when-resizing-windows-especially-drag for more details
    /// </summary>
    public interface ITopLevelWithWin32JitterHacks
    {
        Action Win32JitterLastFrameRepaint { get; set; }
    }
}
