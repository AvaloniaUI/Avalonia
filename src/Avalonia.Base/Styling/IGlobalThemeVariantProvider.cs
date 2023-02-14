using System;
using Avalonia.Controls;
using Avalonia.Metadata;

namespace Avalonia.Styling;

/// <summary>
/// Interface for an application host element with a root theme variant.
/// </summary>
[Unstable]
public interface IGlobalThemeVariantProvider : IResourceHost
{
    /// <summary>
    /// Gets the UI theme variant that is used by the control (and its child elements) for resource determination.
    /// </summary>
    ThemeVariant ActualThemeVariant { get; }

    /// <summary>
    /// Raised when the theme variant is changed on the element or an ancestor of the element.
    /// </summary>
    event EventHandler? ActualThemeVariantChanged;
}
