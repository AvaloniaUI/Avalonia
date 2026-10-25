namespace Avalonia.Media.TextFormatting;

/// <summary>
/// Defines properties which can be overridden when rendering a <see cref="TextRun"/>, without
/// affecting text shaping.
/// </summary>
public readonly record struct TextRunRenderOverrideProperties
{
    /// <inheritdoc cref="TextRunProperties.TextDecorations"/>
    public TextDecorationCollection? TextDecorations { get; init; }

    /// <inheritdoc cref="TextRunProperties.ForegroundBrush"/>
    public IBrush? ForegroundBrush { get; init; }

    /// <inheritdoc cref="TextRunProperties.BackgroundBrush"/>
    public IBrush? BackgroundBrush { get; init; }
}
