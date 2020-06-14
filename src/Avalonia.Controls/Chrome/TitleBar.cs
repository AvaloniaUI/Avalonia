using System;
using System.Reactive.Disposables;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;

namespace Avalonia.Controls.Chrome
{
    public class TitleBar : TemplatedControl
    {
        private CompositeDisposable _disposables;
        private Window _hostWindow;
        private CaptionButtons _captionButtons;

        public TitleBar(Window hostWindow)
        {
            _hostWindow = hostWindow;
        }

        public void Attach()
        {
            if (_disposables == null)
            {
                var layer = ChromeOverlayLayer.GetOverlayLayer(_hostWindow);

                layer.Children.Add(this);

                _disposables = new CompositeDisposable
                {
                    _hostWindow.GetObservable(Window.WindowDecorationMarginsProperty)
                    .Subscribe(x => InvalidateSize()),

                    _hostWindow.GetObservable(Window.ExtendClientAreaTitleBarHeightHintProperty)
                    .Subscribe(x => InvalidateSize()),

                    _hostWindow.GetObservable(Window.OffScreenMarginProperty)
                    .Subscribe(x => InvalidateSize()),

                    _hostWindow.GetObservable(Window.WindowStateProperty)
                    .Subscribe(x =>
                    {
                        PseudoClasses.Set(":minimized", x == WindowState.Minimized);
                        PseudoClasses.Set(":normal", x == WindowState.Normal);
                        PseudoClasses.Set(":maximized", x == WindowState.Maximized);
                        PseudoClasses.Set(":fullscreen", x == WindowState.FullScreen);
                    })
                };

                _captionButtons?.Attach(_hostWindow);

                InvalidateSize();
            }
        }

        void InvalidateSize()
        {
            Margin = new Thickness(
                _hostWindow.OffScreenMargin.Left,
                _hostWindow.OffScreenMargin.Top,
                _hostWindow.OffScreenMargin.Right,
                _hostWindow.OffScreenMargin.Bottom);

            if (_hostWindow.WindowState != WindowState.FullScreen)
            {
                Height = _hostWindow.WindowDecorationMargins.Top;

                if (_captionButtons != null)
                {
                    _captionButtons.Height = Height;
                }
            }
        }

        public void Detach()
        {
            if (_disposables != null)
            {
                var layer = ChromeOverlayLayer.GetOverlayLayer(_hostWindow);

                layer.Children.Remove(this);

                _disposables.Dispose();
                _disposables = null;

                _captionButtons?.Detach();
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _captionButtons = e.NameScope.Find<CaptionButtons>("PART_CaptionButtons");

            _captionButtons.Attach(_hostWindow);
        }
    }
}
