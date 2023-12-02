using System.Collections.Generic;
using Avalonia.Data;

namespace Avalonia.Diagnostics;

internal class LocalValueFrameDiagnostic : IValueFrameDiagnostic
{
    public LocalValueFrameDiagnostic(IEnumerable<ValueEntryDiagnostic> values)
    {
        Values = values;
    }
    
    public string? Description => null;
    public IValueFrameDiagnostic.FrameType Type => IValueFrameDiagnostic.FrameType.Local;
    public bool IsActive => true;
    public BindingPriority Priority => BindingPriority.LocalValue;
    public IEnumerable<ValueEntryDiagnostic> Values { get; }
}
