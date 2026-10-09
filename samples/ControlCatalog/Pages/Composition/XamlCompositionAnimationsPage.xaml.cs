using System;
using Avalonia.Controls;
using Avalonia.Threading;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages.Composition
{
    public partial class XamlCompositionAnimationsPage : UserControl
    {
        private bool _pointerPressed;
        private IDisposable? _timer;
        private CompositionAnimationViewModel? _vm;

        public XamlCompositionAnimationsPage()
        {
            InitializeComponent();

            ImplicitAnimationsCanvas.PointerPressed += ImplicitAnimationsCanvas_PointerPressed;
            ImplicitAnimationsCanvas.PointerReleased += ImplicitAnimationsCanvas_PointerReleased;
            ImplicitAnimationsCanvas.PointerMoved += ImplicitAnimationsCanvas_PointerMoved;
            ImplicitAnimationsCanvas.PointerCaptureLost += ImplicitAnimationsCanvas_PointerCaptureLost;
        }

        private void ImplicitAnimationsCanvas_PointerCaptureLost(object? sender, Avalonia.Input.PointerCaptureLostEventArgs e)
        {
            _pointerPressed = false;
            _timer = DispatcherTimer.RunOnce(() => _vm?.ElipseCanvasOpacity = 0.2f, TimeSpan.FromSeconds(1));
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            _vm = DataContext as CompositionAnimationViewModel;
        }

        private void ImplicitAnimationsCanvas_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (_pointerPressed && _vm != null)
            {
                SetElipsePosition(e);
                _vm?.ElipseCanvasOpacity = 1;
            }
        }

        private void ImplicitAnimationsCanvas_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
        {
            _pointerPressed = false;
            _timer = DispatcherTimer.RunOnce(() => _vm?.ElipseCanvasOpacity = 0.2f, TimeSpan.FromSeconds(1));
        }

        private void ImplicitAnimationsCanvas_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            _pointerPressed = true;
            SetElipsePosition(e);
            _timer?.Dispose();
            _timer = null;
            _vm?.ElipseCanvasOpacity = 1;
        }

        public void SetElipsePosition(Avalonia.Input.PointerEventArgs pointerEventArgs)
        {
            if (_vm != null)
            {
                var point = pointerEventArgs.GetPosition(ImplicitAnimationsCanvas);
                _vm.ElipseCanvasLeft = point.X;
                _vm.ElipseCanvasTop = point.Y;
            }
        }

        private void ExplicitAnimationsCanvas_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            _vm?.ExplicitAnimationCanvasSize = e.NewSize;
        }
    }
}
