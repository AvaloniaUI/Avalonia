using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Rendering.Composition;

namespace ControlCatalog.Pages
{
    public partial class GesturePinchZoomPage : UserControl
    {
        private bool _isInit;
        private double _currentScale;

        public GesturePinchZoomPage()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (_isInit)
            {
                return;
            }

            _isInit = true;

            var image = PinchImage;
            SetPinchHandlers(image);

            ResetButton.Click += (_, _) =>
            {
                var compositionVisual = ElementComposition.GetElementVisual(image);

                if (compositionVisual != null)
                {
                    _currentScale = 1;
                    compositionVisual.Scale = new(1, 1, 1);
                    compositionVisual.Offset = default;
                    image.InvalidateMeasure();
                }
            };
        }

        private void SetPinchHandlers(Control? control)
        {
            if (control == null)
            {
                return;
            }

            _currentScale = 1;
            Vector3D currentOffset = default;

            CompositionVisual? compositionVisual = null;

            void InitComposition(Control visual)
            {
                if (compositionVisual != null)
                {
                    return;
                }

                compositionVisual = ElementComposition.GetElementVisual(visual);
            }

            control.LayoutUpdated += (s, e) =>
            {
                InitComposition(control!);
                if (compositionVisual != null)
                {
                    compositionVisual.Scale = new(_currentScale, _currentScale, 1);

                    if (currentOffset == default)
                    {
                        currentOffset = compositionVisual.Offset;
                    }
                }
            };

            control.AddHandler(InputElement.PinchEvent, (s, e) =>
            {
                InitComposition(control!);

                if (compositionVisual != null)
                {
                    var scale = _currentScale * (float)e.Scale;

                    if (scale <= 1)
                    {
                        scale = 1;
                        compositionVisual.Offset = default;
                    }

                    compositionVisual.Scale = new(scale, scale, 1);

                    e.Handled = true;
                }
            });

            control.AddHandler(InputElement.PinchEndedEvent, (s, e) =>
            {
                InitComposition(control!);

                if (compositionVisual != null)
                {
                    _currentScale = compositionVisual.Scale.X;
                }
            });

            control.AddHandler(InputElement.ScrollGestureEvent, (s, e) =>
            {
                InitComposition(control!);

                if (compositionVisual != null && _currentScale != 1)
                {
                    currentOffset += new Vector3D(e.Delta.X, e.Delta.Y, 0);

                    var currentSize = control.Bounds.Size * _currentScale;

                    currentOffset = new Vector3D(Math.Clamp(currentOffset.X, 0, currentSize.Width - control.Bounds.Width),
                        (float)Math.Clamp(currentOffset.Y, 0, currentSize.Height - control.Bounds.Height),
                        0);

                    compositionVisual.Offset = currentOffset * -1;

                    e.Handled = true;
                }
            });
        }
    }
}
