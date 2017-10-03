using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media;

namespace ControlCatalog
{
    public class ColorTheme
    {
        public static readonly ColorTheme VisualStudioLight = new ColorTheme
        {
            WindowBorder = Brush.Parse("#9B9FB9"),
            Background = Brush.Parse("#EEEEF2"),
            Foreground = Brush.Parse("#1E1E1E"),
            ForegroundLight = Brush.Parse("#525252"),
            BorderLight = Brush.Parse("#9B9FB9"),
            BorderMid = Brush.Parse("#9B9FB9"),
            BorderDark = Brush.Parse("Red"),
            ControlLight = Brush.Parse("#9B9FB9"),
            ControlMid = Brush.Parse("#F5F5F5"),
            ControlDark = Brush.Parse("#E6E7E8")
        };

        public static readonly ColorTheme VisualStudioDark = new ColorTheme
        {
            WindowBorder = Brush.Parse("#9B9FB9"),
            Background = Brush.Parse("#FF2D2D30"),
            Foreground = Brush.Parse("#FFC4C4C4"),
            ForegroundLight = Brush.Parse("#FF808080"),
            BorderLight = Brush.Parse("#FFAAAAAA"),
            BorderMid = Brush.Parse("#FF888888"),
            BorderDark = Brush.Parse("Green"),
            ControlLight = Brush.Parse("#FFFFFFFF"),
            ControlMid = Brush.Parse("#FF3E3E42"),
            ControlDark = Brush.Parse("#FF252526")
        };

        public IBrush WindowBorder { get; set; }

        public IBrush Background { get; set; }

        public IBrush Foreground { get; set; }

        public IBrush ForegroundLight { get; set; }

        public IBrush BorderLight { get; set; }

        public IBrush BorderMid { get; set; }

        public IBrush BorderDark { get; set; }

        public IBrush ControlLight { get; set; }

        public IBrush ControlMid { get; set; }

        public IBrush ControlDark { get; set; }
    }
}
