#nullable enable
using System.Collections.Generic;

namespace Avalonia.LinuxFramebuffer.Input.LibInput;

/// <summary>
/// LibInputBackend Options.
/// </summary>
public sealed record class LibInputBackendOptions
{
    /// <summary>
    /// List Events of events handler to monitoring eg: /dev/eventX.
    /// </summary>
    public IReadOnlyList<string>? Events { get; init; } = null;
}
