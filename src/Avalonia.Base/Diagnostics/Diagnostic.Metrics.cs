using System.Diagnostics;
using System.Diagnostics.Metrics;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Diagnostics;

internal static partial class Diagnostic
{
    private static readonly Meter s_meter;

    private static readonly Histogram<double> s_compositorRender;
    private static readonly Histogram<double> s_compositorUpdate;

    private static readonly Histogram<double> s_layoutMeasure;
    private static readonly Histogram<double> s_layoutArrange;
    private static readonly Histogram<double> s_layoutRender;
    private static readonly Histogram<double> s_layoutInput;

    static Diagnostic()
    {
        s_meter = new Meter("Avalonia.Diagnostic.Meter");
        s_compositorRender = s_meter.CreateHistogram<double>(
            Meters.CompositorRenderPassName,
            Meters.MillisecondsUnit,
            Meters.CompositorRenderPassDescription);
        s_compositorUpdate = s_meter.CreateHistogram<double>(
            Meters.CompositorUpdatePassName,
            Meters.MillisecondsUnit,
            Meters.CompositorUpdatePassDescription);
        s_layoutMeasure = s_meter.CreateHistogram<double>(
            Meters.LayoutMeasurePassName,
            Meters.MillisecondsUnit,
            Meters.LayoutMeasurePassDescription);
        s_layoutArrange = s_meter.CreateHistogram<double>(
            Meters.LayoutArrangePassName,
            Meters.MillisecondsUnit,
            Meters.LayoutArrangePassDescription);
        s_layoutRender = s_meter.CreateHistogram<double>(
            Meters.LayoutRenderPassName,
            Meters.MillisecondsUnit,
            Meters.LayoutRenderPassDescription);
        s_layoutInput = s_meter.CreateHistogram<double>(
            Meters.LayoutInputPassName,
            Meters.MillisecondsUnit,
            Meters.LayoutInputPassDescription);
        s_meter.CreateObservableUpDownCounter(
            Meters.TotalEventHandleCountName,
            () => Interactive.TotalHandlersCount,
            Meters.TotalEventHandleCountUnit,
            Meters.TotalEventHandleCountDescription);
        s_meter.CreateObservableUpDownCounter(
            Meters.TotalVisualCountName,
            () => Visual.RootedVisualChildrenCount,
            Meters.TotalVisualCountUnit,
            Meters.TotalVisualCountDescription);
        s_meter.CreateObservableUpDownCounter(
            Meters.TotalDispatcherTimerCountName,
            () => DispatcherTimer.ActiveTimersCount,
            Meters.TotalDispatcherTimerCountUnit,
            Meters.TotalDispatcherTimerCountDescription);
    }

    public static HistogramReportDisposable BeginCompositorRenderPass() => Begin(s_compositorRender);
    public static HistogramReportDisposable BeginCompositorUpdatePass() => Begin(s_compositorUpdate);
    public static HistogramReportDisposable BeginLayoutMeasurePass() => Begin(s_layoutMeasure);
    public static HistogramReportDisposable BeginLayoutArrangePass() => Begin(s_layoutArrange);
    public static HistogramReportDisposable BeginLayoutInputPass() => Begin(s_layoutInput);
    public static HistogramReportDisposable BeginLayoutRenderPass() => Begin(s_layoutRender);

    private static HistogramReportDisposable Begin(Histogram<double> histogram) => IsEnabled ? new(histogram) : default;
    
    internal readonly ref struct HistogramReportDisposable
    {
        private readonly Histogram<double> _histogram;
        private readonly long _timestamp;

        public HistogramReportDisposable(Histogram<double> histogram)
        {
            _histogram = histogram;
            if (histogram.Enabled)
            {
                _timestamp = Stopwatch.GetTimestamp();
            }
        }

        public void Dispose()
        {
            if (_timestamp > 0)
            {
                _histogram.Record(StopwatchHelper.GetElapsedTimeMs(_timestamp));
            }
        }
    }
}
