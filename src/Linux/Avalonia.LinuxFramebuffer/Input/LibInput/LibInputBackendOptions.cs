using System;
using System.Collections.Generic;

namespace Avalonia.LinuxFramebuffer.Input.LibInput;

/// <summary>
/// LibInputBackend Options.
/// </summary>
public sealed record class LibInputBackendOptions
{
    /// <summary>
    /// Used internally to pass libinput context to <see cref="LibInputBackend.InputThread(object?)"/>.
    /// </summary>
    internal IntPtr LibInputContext { get; init; }

    /// <summary>
    /// List Events of events handler to monitoring eg: /dev/eventX.
    /// </summary>
    public IReadOnlyList<string> Events { get; init; } = null;
}
