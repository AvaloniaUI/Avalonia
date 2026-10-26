namespace Avalonia.Media.TextFormatting;

/// <summary>
/// Defines properties which can be overridden when rendering a <see cref="TextLayout"/>, without
/// affecting text shaping.
/// </summary>
/// <remarks>This type supports features such as dynamic text highlighting.</remarks>
public readonly record struct TextRenderOverrideProperties
{
    public RunRenderProperties? RunRenderProperties { get; init; }
    public LineRenderProperties? LineRenderProperties { get; init; }
}

/// <summary>
/// Defines properties which can be overridden when rendering a <see cref="TextRun"/>, without
/// affecting text shaping.
/// </summary>
public readonly record struct RunRenderProperties
{
    /// <inheritdoc cref="TextRunProperties.TextDecorations"/>
    /// <seealso cref="TextRunProperties.TextDecorations"/>
    public TextDecorationCollection? TextDecorations { get; init; }

    /// <inheritdoc cref="TextRunProperties.ForegroundBrush"/>
    public IBrush? Foreground { get; init; }

    /// <inheritdoc cref="TextRunProperties.BackgroundBrush"/>
    /// <seealso cref="TextRunProperties.BackgroundBrush"/>
    public IBrush? Background { get; init; }
}

/// <summary>
/// Defines properies with which a <see cref="TextLine"/> can be visualised, separately
/// from the <see cref="TextRun"/> elements inside it.
/// </summary>
public readonly record struct LineRenderProperties
{
    public IBrush? Background { get; init; }

    public IPen? Stroke { get; init; }

    public Thickness Padding { get; init; }

    public CornerRadius CornerRadius { get; init; }
}
