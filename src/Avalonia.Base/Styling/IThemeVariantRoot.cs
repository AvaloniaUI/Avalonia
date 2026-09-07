namespace Avalonia.Styling;

/// <summary>
/// Implemented by objects which resolve a <see cref="ThemeVariant.Default"/> requested theme variant from the platform.
/// </summary>
internal interface IThemeVariantRoot
{
    /// <summary>
    /// Gets whether this object currently acts as a theme variant root (it has no parent).
    /// </summary>
    bool IsThemeVariantRoot { get; }
}
