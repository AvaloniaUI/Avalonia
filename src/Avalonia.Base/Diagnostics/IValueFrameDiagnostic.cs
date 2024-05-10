using System.Collections.Generic;
using Avalonia.Data;
using Avalonia.Metadata;

namespace Avalonia.Diagnostics;

public record ValueEntryDiagnostic(AvaloniaProperty Property, object? Value);

[Unstable]
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
    
    string? Description { get; }
    FrameType Type { get; }
    bool IsActive { get; }
    BindingPriority Priority { get; }
    IEnumerable<ValueEntryDiagnostic> Values { get; } 
}
