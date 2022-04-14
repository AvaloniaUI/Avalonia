using Avalonia.Controls;
using Avalonia.Media;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Visuals;

[MemoryDiagnoser]
public class VisualAffectsRenderBenchmarks
{
    private readonly TestVisual _target;
    private readonly IPen _pen;
        
    public VisualAffectsRenderBenchmarks()
    {
        _target = new TestVisual();
        _pen = new Pen(Brushes.Black);
    }
        
    [Benchmark]
    public void SetPropertyThatAffectsRender()
    {
        _target.Pen = _pen;
        _target.Pen = null;
    }

    private class TestVisual : Visual
    {
        /// <summary>
        /// Defines the <see cref="Pen"/> property.
        /// </summary>
        public static readonly StyledProperty<IPen> PenProperty =
            AvaloniaProperty.Register<Border, IPen>(nameof(Pen));
            
        public IPen Pen
        {
            get => GetValue(PenProperty);
            set => SetValue(PenProperty, value);
        }

        static TestVisual()
        {
            AffectsRender<TestVisual>(PenProperty);
        }
    }
}
