using System.Collections.Generic;
using Avalonia.Styling;

namespace Avalonia.Diagnostics;

public class ValueStoreDiagnostic
{
    /// <summary>
    /// Currently applied frames.
    /// </summary>
    public IReadOnlyList<IValueFrameDiagnostic> AppliedFrames { get; }

    internal ValueStoreDiagnostic(IReadOnlyList<IValueFrameDiagnostic> appliedFrames)
    {
        AppliedFrames = appliedFrames;
    }
}
