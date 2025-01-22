using System;
using System.Collections.Generic;
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
    }

    public static HistogramReportDisposable BeginCompositorRenderPass() => new(s_compositorRender);
    public static HistogramReportDisposable BeginCompositorUpdatePass() => new(s_compositorUpdate);
    public static HistogramReportDisposable BeginLayoutMeasurePass() => new(s_layoutMeasure);
    public static HistogramReportDisposable BeginLayoutArrangePass() => new(s_layoutArrange);

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
