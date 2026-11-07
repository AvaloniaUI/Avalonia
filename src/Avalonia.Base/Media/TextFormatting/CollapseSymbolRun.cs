using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Avalonia.Media.TextFormatting;

public class CollapseSymbolRun(IEnumerable<TextRun> collapsedRuns, DrawableTextRun symbol) : DrawableTextRun
{
    /// <summary>
    /// Gets the underlying symbol to draw.
    /// </summary>
    public DrawableTextRun Symbol { get; } = symbol;
    
    /// <summary>
    /// Gets a list of the <see cref="TextRun"/> objects which the collapse symbol replaces.
    /// </summary>
    public ImmutableArray<TextRun> CollapsedRuns { get; } = [.. collapsedRuns];

    public int CollapsedLength { get; } = collapsedRuns.Sum(r => r.Length);

    public sealed override Size Size => Symbol.Size;
    public sealed override double Baseline => Symbol.Baseline;
    public sealed override int Length => Symbol.Length;
    public sealed override ReadOnlyMemory<char> Text => Symbol.Text;
    public sealed override TextRunProperties? Properties => Symbol.Properties;

    public override void Draw(DrawingContext drawingContext, Point origin) => Symbol.Draw(drawingContext, origin);
}
