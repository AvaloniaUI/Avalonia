using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Input
{
    public class FindNextElementOptions
    {
        public InputElement? SearchRoot { get; set; }
        public Rect ExclusionRect { get; set; }
        public Rect? FocusHintRectangle { get; set; }
        public XYFocusNavigationStrategy? NavigationStrategyOverride { get; set; }
        public bool IgnoreOcclusivity { get; set; }
    }
}
