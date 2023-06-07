using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Avalonia.Diagnostics.Controls
{
    [TemplatePart("PART_ClearButton", typeof(Button))]
    partial class BrushEditor : TemplatedControl
    {
        private readonly EventHandler<RoutedEventArgs> clearHandler;
        private Button? _cleraButton = default;
        private readonly ColorView _colorView = new()
        {
            HexInputAlphaPosition = AlphaComponentPosition.Leading, // Always match XAML
        };


        public BrushEditor()
        {
            FlyoutBase.SetAttachedFlyout(this, new Flyout { Content = _colorView });
            _colorView.ColorChanged += (_, e) => Brush = new ImmutableSolidColorBrush(e.NewColor);
            clearHandler = (s, e) => Brush = default;
        }

        protected override Type StyleKeyOverride => typeof(BrushEditor);

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            if (_cleraButton is not null)
            {
                _cleraButton.Click -= clearHandler;
            }
            _cleraButton = e.NameScope.Find<Button>("PART_ClearButton");
            if (_cleraButton is Button button)
            {
                button.Click += clearHandler;
            }
        }

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
                if (Brush is ISolidColorBrush scb)
                {
                    _colorView.Color = scb.Color;
                }
                ToolTip.SetTip(this, Brush?.GetType().Name ?? "(null)");
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

            var brush = Brush ?? Brushes.Black;

            context.FillRectangle(brush, Bounds);

            var text = (Brush as ISolidColorBrush)?.Color.ToString() ?? "(null)";

            var ft = new FormattedText(text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                10,
                GetTextBrush(brush));

            context.DrawText(ft,
                new Point(Bounds.Width / 2 - ft.Width / 2, Bounds.Height / 2 - ft.Height / 2));
        }

        /// <summary>
        /// <see cref="https://stackoverflow.com/questions/6763032/how-to-pick-a-background-color-depending-on-font-color-to-have-proper-contrast">How to pick a background color depending on font color to have proper contrast</see>/>
        /// </summary>
        /// <param name="brush"></param>
        /// <returns></returns>
        private static IBrush GetTextBrush(IBrush brush)
        {
            if (brush is ISolidColorBrush solid)
            {
                var color = solid.Color;


                double R = color.R / 255.0;
                double G = color.G / 255.0;
                double B = color.B / 255.0;

                R = Math.Pow((R + 0.055) / 1.055, 2.4);
                G = Math.Pow((G + 0.055) / 1.055, 2.4);
                B = Math.Pow((B + 0.055) / 1.055, 2.4);
                var l = 0.2126 * R + 0.7152 * G + 0.0722 * B;

                return l < 0.5 ? Brushes.White : Brushes.Black;
            }
            return Brushes.White;
        }
    }
}
