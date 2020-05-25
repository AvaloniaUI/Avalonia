using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace ControlCatalog
{
    public class CustomControl : TemplatedControl
    {
        public static readonly StyledProperty<IBrush> StrokeProperty =
            AvaloniaProperty.Register<CustomControl, IBrush>(nameof(Stroke));

        public IBrush Stroke
        {
            get => GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }
    }
}
