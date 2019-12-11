using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace ControlCatalog.Pages
{
    public class PointersPage : Control
    {
        class PointerInfo
        {
            public Point Point { get; set; }
            public Color Color { get; set; }
        }

        private static Color[] AllColors = new[]
        {
            Colors.Aqua,
            Colors.Beige, 
            Colors.Chartreuse, 
            Colors.Coral,
            Colors.Fuchsia,
            Colors.Crimson,
            Colors.Lavender, 
            Colors.Orange,
            Colors.Orchid,
            Colors.ForestGreen,
            Colors.SteelBlue,
            Colors.PapayaWhip,
            Colors.PaleVioletRed,
            Colors.Goldenrod,
            Colors.Maroon,
            Colors.Moccasin,
            Colors.Navy,
            Colors.Wheat,
            Colors.Violet,
            Colors.Sienna,
            Colors.Indigo,
            Colors.Honeydew
        };
        
        private Dictionary<IPointer, PointerInfo> _pointers = new Dictionary<IPointer, PointerInfo>();

        public PointersPage()
        {
            ClipToBounds = true;
        }
        
        void UpdatePointer(PointerEventArgs e)
        {
            if (!_pointers.TryGetValue(e.Pointer, out var info))
            {
                if (e.RoutedEvent == PointerMovedEvent)
                    return;
                var colors = AllColors.Except(_pointers.Values.Select(c => c.Color)).ToArray();
                var color = colors[new Random().Next(0, colors.Length - 1)];
                _pointers[e.Pointer] = info = new PointerInfo {Color = color};
            }

            info.Point = e.GetPosition(this);
            InvalidateVisual();
        }
        
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            UpdatePointer(e);
            e.Pointer.Capture(this);
            e.Handled = true;
            base.OnPointerPressed(e);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            UpdatePointer(e);
            e.Handled = true;
            base.OnPointerMoved(e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            _pointers.Remove(e.Pointer);
            e.Handled = true;
            InvalidateVisual();
        }

        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            _pointers.Remove(e.Pointer);
            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            context.FillRectangle(Brushes.Transparent, new Rect(default, Bounds.Size));
            foreach (var pt in _pointers.Values)
            {
                var brush = new ImmutableSolidColorBrush(pt.Color);
                context.DrawGeometry(brush, null, new EllipseGeometry(new Rect(pt.Point.X - 75, pt.Point.Y - 75,
                    150, 150)));
            }
            
        }
    }
}
