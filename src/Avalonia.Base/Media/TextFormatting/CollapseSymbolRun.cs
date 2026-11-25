using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Avalonia.Media.TextFormatting;

/// <summary>
/// A run which indicates that other runs have been collapsed into it.
/// </summary>
/// <param name="collapsedRuns">The runs which have been collapsed into this run. A reference is added to each one.</param>
/// <param name="symbol">The symbol to draw, typically "…". No references are added.</param>
/// <exception cref="ArgumentException">Thrown when <paramref name="symbol"/> is another <see cref="CollapseSymbolRun"/>.</exception>
public class CollapseSymbolRun(IEnumerable<TextRun> collapsedRuns, DrawableTextRun symbol) : DrawableTextRun, IDisposable
{
    /// <summary>
    /// Gets the underlying symbol to draw.
    /// </summary>
    public DrawableTextRun Symbol { get; } = symbol is CollapseSymbolRun ? throw new ArgumentException($"Symbol may not be another {nameof(CollapseSymbolRun)}!", nameof(symbol)) : symbol;

    /// <summary>
    /// Gets a list of the <see cref="TextRun"/> objects which the collapse symbol replaces.
    /// </summary>
    public ImmutableArray<TextRun> CollapsedRuns { get; } = [.. collapsedRuns.Select(AddRef)];

    /// <summary>
    /// Gets the number of characters in <see cref="CollapsedRuns"/>.
    /// </summary>
    public int CollapsedLength { get; } = collapsedRuns.Sum(r => r.Length);

    public sealed override Size Size => Symbol.Size;
    public sealed override double Baseline => Symbol.Baseline;
    public sealed override int Length => Symbol.Length;
    public sealed override ReadOnlyMemory<char> Text => Symbol.Text;
    public sealed override TextRunProperties? Properties => Symbol.Properties;

    public override void Draw(DrawingContext drawingContext, Point origin) => Symbol.Draw(drawingContext, origin);

    internal void AddRef()
    {
        (Symbol as ShapedTextRun)?.AddRef();

        foreach (var run in CollapsedRuns)
        {
            AddRef(run);
        }
    }

    private static TextRun AddRef(TextRun run)
    {
        switch (run)
        {
            case ShapedTextRun shaped:
                shaped.AddRef();
                break;
            case CollapseSymbolRun collapse: // weird, but possible
                collapse.AddRef();
                break;
        }
        return run;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var run in CollapsedRuns.Prepend(Symbol).OfType<IDisposable>())
            {
                run.Dispose();
            }
        }
    }
}
