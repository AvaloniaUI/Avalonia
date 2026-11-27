using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;
using ControlCatalog.Controls;

namespace ControlCatalog.Pages.Composition
{
    public partial class CustomAnimationPage : UserControl
    {
        private CompositionCustomVisual? _customVisual;

        public CustomAnimationPage()
        {
            InitializeComponent();
            AttachCustomVisual(CustomVisualHost);
        }

        private void ButtonThreadSleep(object? sender, RoutedEventArgs e)
        {
            Thread.Sleep(5000);
        }

        private void ButtonStartCustomVisual(object? sender, RoutedEventArgs e)
        {
            _customVisual?.SendHandlerMessage(CustomVisualHandler.StartMessage);
        }

        private void ButtonStopCustomVisual(object? sender, RoutedEventArgs e)
        {
            _customVisual?.SendHandlerMessage(CustomVisualHandler.StopMessage);
        }

        private void PreciseDirtyRectsCheckboxCustomVisualChanged(object sender, RoutedEventArgs e)
        {
            _customVisual?.SendHandlerMessage(PreciseDirtyRectsCheckboxCustomVisual?.IsChecked == true
                ? CustomVisualHandler.UsePreciseDirtyRects
                : CustomVisualHandler.UseNonPreciseDirtyRects);
        }


        void AttachCustomVisual(Visual v)
        {
            void Update()
            {
                if (_customVisual == null)
                    return;
                var h = (float)Math.Min(v.Bounds.Height, v.Bounds.Width / 3);
                _customVisual.Size = new(v.Bounds.Width, h);
                _customVisual.Offset = new(0, (v.Bounds.Height - h) / 2, 0);
            }
            v.AttachedToVisualTree += delegate
            {
                var compositor = ElementComposition.GetElementVisual(v)?.Compositor;
                if (compositor == null || _customVisual?.Compositor == compositor)
                    return;
                _customVisual = compositor.CreateCustomVisual(new CustomVisualHandler());
                ElementComposition.SetElementChildVisual(v, _customVisual);
                _customVisual.SendHandlerMessage(CustomVisualHandler.StartMessage);
                PreciseDirtyRectsCheckboxCustomVisualChanged(this, new());
                Update();
            };

            v.PropertyChanged += (_, a) =>
            {
                if (a.Property == BoundsProperty)
                    Update();
            };
        }
    }
}
