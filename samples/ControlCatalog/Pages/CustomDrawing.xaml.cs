using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class CustomDrawing : UserControl
    {
        public CustomDrawing()
        {
            InitializeComponent();
        }

        private void RotateMinus(object? sender, RoutedEventArgs e)
        {
            CustomDrawingControl.Rotation -= Math.PI / 20.0d;
        }

        private void RotatePlus(object? sender, RoutedEventArgs e)
        {
            if (CustomDrawingControl is null)
                return;
            CustomDrawingControl.Rotation += Math.PI / 20.0d;
        }

        private void ZoomIn(object? sender, RoutedEventArgs e)
        {
            if (CustomDrawingControl is null)
                return;
            CustomDrawingControl.Scale *= 1.2d;
        }

        private void ZoomOut(object? sender, RoutedEventArgs e)
        {
            if (CustomDrawingControl is null)
                return;
            CustomDrawingControl.Scale /= 1.2d;
        }
    }
}
