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
    }
}
