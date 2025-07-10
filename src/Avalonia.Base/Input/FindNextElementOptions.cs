using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Input
{
    public sealed class FindNextElementOptions
    {
        public InputElement? SearchRoot { get; init; }
        public Rect ExclusionRect { get; init; }
        public Rect? FocusHintRectangle { get; init; }
        public XYFocusNavigationStrategy? NavigationStrategyOverride { get; init; }
        public bool IgnoreOcclusivity { get; init; }
    }
}
