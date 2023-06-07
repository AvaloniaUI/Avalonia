using System;
using Avalonia.Controls;
using Avalonia.Metadata;

namespace Avalonia.Styling;

/// <summary>
/// Interface for the host element with a theme variant.
/// </summary>
public interface IThemeVariantHost : IResourceHost
{
    /// <summary>
    /// Gets the UI theme that is currently used by the element, which might be different than the RequestedThemeVariantProperty.
    /// </summary>
    /// <returns>
    /// If current control is contained in the ThemeVariantScope, TopLevel or Application with non-default RequestedThemeVariant, that value will be returned.
    /// Otherwise, current OS theme variant is returned.
    /// </returns>
    ThemeVariant ActualThemeVariant { get; }

    /// <summary>
    /// Raised when the theme variant is changed on the element or an ancestor of the element.
    /// </summary>
    event EventHandler? ActualThemeVariantChanged;
}
