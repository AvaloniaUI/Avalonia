using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Avalonia.Diagnostics.Controls
{
    internal sealed class BrushEditor : Control
    {
        /// <summary>
        ///     Defines the <see cref="Brush" /> property.
        /// </summary>
        public static readonly DirectProperty<BrushEditor, IBrush?> BrushProperty =
            AvaloniaProperty.RegisterDirect<BrushEditor, IBrush?>(
                nameof(Brush), o => o.Brush, (o, v) => o.Brush = v);

        private IBrush? _brush;

        public IBrush? Brush
        {
            get => _brush;
            set => SetAndRaise(BrushProperty, ref _brush, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == BrushProperty)
            {
                switch (Brush)
                {
                    case ISolidColorBrush scb:
                    {
                        var colorView = new ColorView
                        {
                            HexInputAlphaPosition = AlphaComponentPosition.Leading, // Always match XAML
                            Color = scb.Color,
                        };

                        colorView.ColorChanged += (_, e) => Brush = new ImmutableSolidColorBrush(e.NewColor);

                        FlyoutBase.SetAttachedFlyout(this, new Flyout { Content = colorView });
                        ToolTip.SetTip(this, $"{scb.Color} ({Brush.GetType().Name})");

                        break;
                    }

                    default:

                        FlyoutBase.SetAttachedFlyout(this, null);
                        ToolTip.SetTip(this, Brush?.GetType().Name ?? "(null)");

                        break;
                }

                InvalidateVisual();
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            FlyoutBase.ShowAttachedFlyout(this);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (Brush != null)
            {
                context.FillRectangle(Brush, Bounds);
            }
            else
            {
                context.FillRectangle(Brushes.Black, Bounds);

                var ft = new FormattedText("(null)",
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    Typeface.Default,
                    10,
                    Brushes.White);

                context.DrawText(ft, 
                    new Point(Bounds.Width / 2 - ft.Width / 2, Bounds.Height / 2 - ft.Height / 2));
            }
        }
    }
}
