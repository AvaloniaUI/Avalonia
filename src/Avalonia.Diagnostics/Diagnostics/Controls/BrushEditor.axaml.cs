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
        private Button? _clearButton = default;
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
            if (_clearButton is not null)
            {
                _clearButton.Click -= clearHandler;
            }
            _clearButton = e.NameScope.Find<Button>("PART_ClearButton");
            if (_clearButton is Button button)
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
        /// Get Contrasted Text Color
        /// </summary>
        /// <param name="brush"></param>
        /// <returns></returns>
        private static IBrush GetTextBrush(IBrush brush)
        {
            if (brush is ISolidColorBrush solid)
            {
                var color = solid.Color;
                var l = ColorHelper.GetRelativeLuminance(color);

                return l < 0.5 ? Brushes.White : Brushes.Black;
            }
            return Brushes.White;
        }
    }
}
