using System;
using System.Linq;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Diagnostics;

/// <summary>
/// Defines diagnostic extensions on <see cref="StyledElement"/>s.
/// </summary>
[PrivateApi]
public static class StyledElementExtensions
{
    /// <summary>
    /// Gets a style diagnostics for a <see cref="StyledElement"/>.
    /// </summary>
    /// <param name="styledElement">The element.</param>
    public static ValueStoreDiagnostic GetValueStoreDiagnostic(this StyledElement styledElement)
    {
        return styledElement.GetValueStore().GetStoreDiagnostic();
    }

    [Obsolete("Use StyledElementExtensions.GetValueStoreDiagnostic instead", true)]
    public static StyleDiagnostics GetStyleDiagnostics(this StyledElement styledElement)
    {
        var diagnostics = styledElement.GetValueStore().GetStoreDiagnostic();
        return new StyleDiagnostics(diagnostics.AppliedFrames
            .OfType<StyleValueFrameDiagnostic>()
            .Select(f => f.AsAppliedStyle())
            .ToArray());
    }
}

