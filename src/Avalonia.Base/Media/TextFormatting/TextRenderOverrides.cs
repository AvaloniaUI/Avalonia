namespace Avalonia.Media.TextFormatting;

/// <summary>
/// Defines properties which can be overridden when rendering a <see cref="TextLayout"/>, without
/// affecting text shaping.
/// </summary>
/// <remarks>This type supports features such as dynamic text highlighting.</remarks>
public record struct TextRenderOverrides
{
    public RunRenderOverrides? Run { get; set; }
    public LineRenderOverrides? Line { get; set; }

    public static TextRenderOverrides operator |(TextRenderOverrides? left, TextRenderOverrides right) => new()
    {
        Run = right.Run.HasValue ? (left?.Run | right.Run.Value) : left?.Run,
        Line = right.Line.HasValue ? (left?.Line | right.Line.Value) : left?.Line,
    };
}

/// <summary>
/// Defines properties which can be overridden when rendering a <see cref="TextRun"/>, without
/// affecting text shaping.
/// </summary>
public record struct RunRenderOverrides
{
    /// <inheritdoc cref="TextRunProperties.TextDecorations"/>
    /// <seealso cref="TextRunProperties.TextDecorations"/>
    public TextDecorationCollection? TextDecorations { get; set; }

    /// <inheritdoc cref="TextRunProperties.ForegroundBrush"/>
    public IBrush? Foreground { get; set; }

    /// <inheritdoc cref="TextRunProperties.BackgroundBrush"/>
    /// <seealso cref="TextRunProperties.BackgroundBrush"/>
    public IBrush? Background { get; set; }

    public static RunRenderOverrides operator |(RunRenderOverrides? left, RunRenderOverrides right) => new()
    {
        TextDecorations = right.TextDecorations ?? left?.TextDecorations,
        Foreground = right.Foreground ?? left?.Foreground,
        Background = right.Background ?? left?.Background,
    };
}

/// <summary>
/// Defines properies with which a <see cref="TextLine"/> can be visualised, separately
/// from the <see cref="TextRun"/> elements inside it.
/// </summary>
public record struct LineRenderOverrides
{
    public IBrush? Background { get; set; }

    public IPen? Stroke { get; set; }

    public Thickness? Padding { get; set; }

    public CornerRadius? CornerRadius { get; set; }

    public static LineRenderOverrides operator |(LineRenderOverrides? left, LineRenderOverrides right) => new()
    {
        Background = right.Background ?? left?.Background,
        Stroke = right.Stroke ?? left?.Stroke,
        Padding = right.Padding ?? left?.Padding,
        CornerRadius = right.CornerRadius ?? left?.CornerRadius,
    };
}
