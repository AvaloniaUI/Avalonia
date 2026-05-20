using System;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Text;

/// <summary>
/// Single-variant benchmark used as a profiling target. Pins the most expensive
/// realistic case from <see cref="HugeTextLayout"/> (Wrap=true, Trim=false on
/// the emoji block) so BDN's EventPipe profiler captures only this one run.
/// Not part of the regular perf coverage — leave excluded from normal sweeps
/// or run with an explicit filter.
/// </summary>
[MemoryDiagnoser]
[MinIterationTime(150)]
[MaxWarmupCount(15)]
public class TextLayoutProfile : IDisposable
{
    private readonly IDisposable _app;

    public TextLayoutProfile()
    {
        _app = UnitTestApplication.Start(TestServices.StyledWindow);
    }

    private const string Emojis = HugeTextLayout.EmojisText;

    [Benchmark]
    public TextLayout BuildEmojisWrapped()
    {
        var layout = new TextLayout(
            Emojis,
            Typeface.Default,
            12d,
            Brushes.Black,
            maxWidth: 120,
            textTrimming: TextTrimming.None,
            textWrapping: TextWrapping.WrapWithOverflow);
        layout.Dispose();
        return layout;
    }

    public void Dispose() => _app?.Dispose();
}
