using Avalonia.Media;

namespace TextInputDebugger
{
    internal enum TraceCategory
    {
        Read,
        Mutation,
        Composition,
        Geometry,
        Event,
        Legacy,
        Invariant,
    }

    /// <summary>One logged call or event on the structured text input seam.</summary>
    internal sealed class TraceEntry
    {
        public int Seq { get; init; }

        public string Time { get; init; } = "";

        public TraceCategory Category { get; init; }

        public string Member { get; init; } = "";

        public string Details { get; init; } = "";

        public bool IsError { get; init; }

        public IBrush CategoryBrush => Category switch
        {
            TraceCategory.Read => Brushes.Gray,
            TraceCategory.Mutation => Brushes.OrangeRed,
            TraceCategory.Composition => Brushes.MediumOrchid,
            TraceCategory.Geometry => Brushes.SteelBlue,
            TraceCategory.Event => Brushes.SeaGreen,
            TraceCategory.Legacy => Brushes.DarkKhaki,
            _ => Brushes.Red,
        };

        public IBrush TextBrush => IsError ? Brushes.Red : Brushes.Black;

        public string CategoryLabel => Category switch
        {
            TraceCategory.Read => "read",
            TraceCategory.Mutation => "mut",
            TraceCategory.Composition => "comp",
            TraceCategory.Geometry => "geo",
            TraceCategory.Event => "evt",
            TraceCategory.Legacy => "leg",
            _ => "INV",
        };
    }
}
