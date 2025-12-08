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

        public const string TotalEventHandleCountName = "avalonia.ui.event.handler.count";
        public const string TotalEventHandleCountDescription = "Number of event handlers currently registered in the application";
        public const string TotalEventHandleCountUnit = "{handler}";
        public const string TotalVisualCountName = "avalonia.ui.visual.count";
        public const string TotalVisualCountDescription = "Number of visual elements currently present in the visual tree";
        public const string TotalVisualCountUnit = "{visual}";
        public const string TotalDispatcherTimerCountName = "avalonia.ui.dispatcher.timer.count";
        public const string TotalDispatcherTimerCountDescription = "Number of active dispatcher timers in the application";
        public const string TotalDispatcherTimerCountUnit = "{timer}";
    }

    public static class Tags
    {
        public const string Style = nameof(Style);
        public const string SelectorResult = nameof(SelectorResult);

        public const string Key = nameof(Key);
        public const string ThemeVariant = nameof(ThemeVariant);
        public const string Result = nameof(Result);

        public const string Activator = nameof(Activator);
        public const string IsActive = nameof(IsActive);
        public const string Selector = nameof(Selector);
        public const string Control = nameof(Control);

        public const string RoutedEvent = nameof(RoutedEvent);
    }
}
