using System;
using System.ComponentModel;
using System.Text;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class ControlLayoutViewModel : ViewModelBase
    {
        private readonly IVisual _control;
        private Thickness _marginThickness;
        private Thickness _borderThickness;
        private Thickness _paddingThickness;
        private double _width;
        private double _height;
        private string _widthConstraint;
        private string _heightConstraint;
        private bool _updatingFromControl;

        public Thickness MarginThickness
        {
            get => _marginThickness;
            set => RaiseAndSetIfChanged(ref _marginThickness, value);
        }
        
        public Thickness BorderThickness
        {
            get => _borderThickness;
            set => RaiseAndSetIfChanged(ref _borderThickness, value);
        }
        
        public Thickness PaddingThickness
        {
            get => _paddingThickness;
            set => RaiseAndSetIfChanged(ref _paddingThickness, value);
        }

        public double Width
        {
            get => _width;
            private set => RaiseAndSetIfChanged(ref _width, value);
        }

        public double Height
        {
            get => _height;
            private set => RaiseAndSetIfChanged(ref _height, value);
        }

        public string WidthConstraint
        {
            get => _widthConstraint;
            private set => RaiseAndSetIfChanged(ref _widthConstraint, value);
        }

        public string HeightConstraint
        {
            get => _heightConstraint;
            private set => RaiseAndSetIfChanged(ref _heightConstraint, value);
        }

        public bool HasPadding { get; }
        
        public bool HasBorder { get; }
        
        public ControlLayoutViewModel(IVisual control)
        {
            _control = control;

            HasPadding = AvaloniaPropertyRegistry.Instance.IsRegistered(control, Decorator.PaddingProperty);
            HasBorder = AvaloniaPropertyRegistry.Instance.IsRegistered(control, Border.BorderThicknessProperty);

            if (control is AvaloniaObject ao)
            {
                MarginThickness = ao.GetValue(Layoutable.MarginProperty);

                if (HasPadding)
                {
                    PaddingThickness = ao.GetValue(Decorator.PaddingProperty);
                }

                if (HasBorder)
                {
                    BorderThickness = ao.GetValue(Border.BorderThicknessProperty);
                }
            }

            UpdateSize();
            UpdateSizeConstraints();
        }

        private void UpdateSizeConstraints()
        {
            if (_control is IAvaloniaObject ao)
            {
                string CreateConstraintInfo(StyledProperty<double> minProperty, StyledProperty<double> maxProperty)
                {
                    bool hasMin = ao.IsSet(minProperty);
                    bool hasMax = ao.IsSet(maxProperty);

                    if (hasMin || hasMax)
                    {
                        var builder = new StringBuilder();

                        if (hasMin)
                        {
                            var minValue = ao.GetValue(minProperty);
                            builder.AppendFormat("Min: {0}", Math.Round(minValue, 2));
                            builder.AppendLine();
                        }

                        if (hasMax)
                        {
                            var maxValue = ao.GetValue(maxProperty);
                            builder.AppendFormat("Max: {0}", Math.Round(maxValue, 2));
                        }

                        return builder.ToString();
                    }

                    return null;
                }

                WidthConstraint = CreateConstraintInfo(Layoutable.MinWidthProperty, Layoutable.MaxWidthProperty);
                HeightConstraint = CreateConstraintInfo(Layoutable.MinHeightProperty, Layoutable.MaxHeightProperty);
            }
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (_updatingFromControl)
            {
                return;
            }

            if (_control is AvaloniaObject ao)
            {
                if (e.PropertyName == nameof(MarginThickness))
                {
                    ao.SetValue(Layoutable.MarginProperty, MarginThickness);
                }
                else if (HasPadding && e.PropertyName == nameof(PaddingThickness))
                {
                    ao.SetValue(Decorator.PaddingProperty, PaddingThickness);
                }
                else if (HasBorder && e.PropertyName == nameof(BorderThickness))
                {
                    ao.SetValue(Border.BorderThicknessProperty, BorderThickness);
                }
            }
        }

        public void ControlPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            try
            {
                _updatingFromControl = true;

                if (e.Property == Visual.BoundsProperty)
                {
                    UpdateSize();
                }
                else
                {
                    if (_control is IAvaloniaObject ao)
                    {
                        if (e.Property == Layoutable.MarginProperty)
                        {
                            MarginThickness = ao.GetValue(Layoutable.MarginProperty);
                        } 
                        else if (e.Property == Decorator.PaddingProperty)
                        {
                            PaddingThickness = ao.GetValue(Decorator.PaddingProperty);
                        } 
                        else if (e.Property == Border.BorderThicknessProperty)
                        {
                            BorderThickness = ao.GetValue(Border.BorderThicknessProperty);
                        } 
                        else if (e.Property == Layoutable.MinWidthProperty ||
                                 e.Property == Layoutable.MaxWidthProperty ||
                                 e.Property == Layoutable.MinHeightProperty ||
                                 e.Property == Layoutable.MaxHeightProperty)
                        {
                            UpdateSizeConstraints();
                        }
                    }
                }
            }
            finally
            {
                _updatingFromControl = false;
            }
        }

        private void UpdateSize()
        {
            var size = _control.Bounds;

            Width = Math.Round(size.Width, 2);
            Height = Math.Round(size.Height, 2);
        }
    }
}
