namespace Avalonia.Diagnostics;

internal static partial class Diagnostic
{
    public static class Meters
    {
        public const string SecondsUnit = "s";
        public const string MillisecondsUnit = "ms";

        public const string CompositorRenderPassName = "avalonia.comp.render.time";
        public const string CompositorRenderPassDescription = "Duration of the compositor render pass on render thread";
        public const string CompositorUpdatePassName = "avalonia.comp.update.time";
        public const string CompositorUpdatePassDescription = "Duration of the compositor update pass on render thread";

        public const string LayoutMeasurePassName = "avalonia.ui.measure.time";
        public const string LayoutMeasurePassDescription = "Duration of layout measurement pass on UI thread";
        public const string LayoutArrangePassName = "avalonia.ui.arrange.time";
        public const string LayoutArrangePassDescription = "Duration of layout arrangement pass on UI thread";
        public const string LayoutRenderPassName = "avalonia.ui.render.time";
        public const string LayoutRenderPassDescription = "Duration of render recording pass on UI thread";
        public const string LayoutInputPassName = "avalonia.ui.input.time";
        public const string LayoutInputPassDescription = "Duration of input processing on UI thread";
    }
}
