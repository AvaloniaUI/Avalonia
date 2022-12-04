using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public partial class CustomDrawing : UserControl
    {
        public CustomDrawing()
        {
            InitializeComponent();
        }

        private CustomDrawingExampleControl? _customControl;
        public CustomDrawingExampleControl CustomDrawingControl
        {
            get
            {
                if (_customControl is not null)
                    return _customControl;
                throw new System.Exception("Control did not get initialized");
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            var cntrl = this.FindControl<CustomDrawingExampleControl>("CustomDrawingControl");
            if (cntrl != null)
            {
                _customControl = cntrl;
            }
            else
            {
                // be sad about it 
            }
        }

        private void RotateMinus (object? sender, RoutedEventArgs e)
        {
            if (_customControl is null) return;
            _customControl.Rotation -= Math.PI / 20.0d;
        }

        private void RotatePlus(object? sender, RoutedEventArgs e)
        {
            if (_customControl is null)
                return;
            _customControl.Rotation += Math.PI / 20.0d;
        }

        private void ZoomIn(object? sender, RoutedEventArgs e)
        {
            if (_customControl is null)
                return;
            _customControl.Scale *= 1.2d;
        }

        private void ZoomOut(object? sender, RoutedEventArgs e)
        {
            if (_customControl is null)
                return;
            _customControl.Scale /= 1.2d;
        }
    }
}
