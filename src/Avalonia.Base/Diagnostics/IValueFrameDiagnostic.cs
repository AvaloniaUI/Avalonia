using System.Collections.Generic;
using Avalonia.Data;
using Avalonia.Metadata;

namespace Avalonia.Diagnostics;

[PrivateApi]
public record ValueEntryDiagnostic(AvaloniaProperty Property, object? Value);

[PrivateApi]
[NotClientImplementable]
public interface IValueFrameDiagnostic
{
    public enum FrameType
    {
        Unknown = 0,
        Local,
        Theme,
        Style,
        Template
    }

    object? Source { get; } 
    FrameType Type { get; }
    bool IsActive { get; }
    BindingPriority Priority { get; }
    IEnumerable<ValueEntryDiagnostic> Values { get; } 
}
