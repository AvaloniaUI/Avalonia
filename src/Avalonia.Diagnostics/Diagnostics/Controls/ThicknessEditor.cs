using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;

namespace Avalonia.Diagnostics.Controls
{
    internal class ThicknessEditor : ContentControl
    {
        public static readonly StyledProperty<Thickness> ThicknessProperty =
            AvaloniaProperty.Register<ThicknessEditor, Thickness>(nameof(Thickness), 
                defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<string?> HeaderProperty =
            AvaloniaProperty.Register<ThicknessEditor, string?>(nameof(Header));

        public static readonly StyledProperty<bool> IsPresentProperty =
            AvaloniaProperty.Register<ThicknessEditor, bool>(nameof(IsPresent), true);

        public static readonly StyledProperty<double> LeftProperty =
            AvaloniaProperty.Register<ThicknessEditor, double>(nameof(Left));

        public static readonly StyledProperty<double> TopProperty =
            AvaloniaProperty.Register<ThicknessEditor, double>(nameof(Top));

        public static readonly StyledProperty<double> RightProperty =
            AvaloniaProperty.Register<ThicknessEditor, double>(nameof(Right));

        public static readonly StyledProperty<double> BottomProperty =
            AvaloniaProperty.Register<ThicknessEditor, double>(nameof(Bottom));

        public static readonly StyledProperty<IBrush> HighlightProperty =
            AvaloniaProperty.Register<ThicknessEditor, IBrush>(nameof(Highlight));

        private bool _isUpdatingThickness;

        public Thickness Thickness
        {
            get => GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }

        public string? Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public bool IsPresent
        {
            get => GetValue(IsPresentProperty);
            set => SetValue(IsPresentProperty, value);
        }

        public double Left
        {
            get => GetValue(LeftProperty);
            set => SetValue(LeftProperty, value);
        }

        public double Top
        {
            get => GetValue(TopProperty);
            set => SetValue(TopProperty, value);
        }

        public double Right
        {
            get => GetValue(RightProperty);
            set => SetValue(RightProperty, value);
        }

        public double Bottom
        {
            get => GetValue(BottomProperty);
            set => SetValue(BottomProperty, value);
        }

        public IBrush Highlight
        {
            get => GetValue(HighlightProperty);
            set => SetValue(HighlightProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ThicknessProperty)
            {
                try
                {
                    _isUpdatingThickness = true;

                    var value = change.GetNewValue<Thickness>();

                    SetCurrentValue(LeftProperty, value.Left);
                    SetCurrentValue(TopProperty, value.Top);
                    SetCurrentValue(RightProperty, value.Right);
                    SetCurrentValue(BottomProperty, value.Bottom);
                }
                finally
                {
                    _isUpdatingThickness = false;
                }
            }
            else if (!_isUpdatingThickness &&
                     (change.Property == LeftProperty || change.Property == TopProperty ||
                      change.Property == RightProperty || change.Property == BottomProperty))
            {
                SetCurrentValue(ThicknessProperty, new(Left, Top, Right, Bottom));
            }
        }
    }
}
