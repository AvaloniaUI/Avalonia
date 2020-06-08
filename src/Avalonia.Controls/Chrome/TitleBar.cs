using System;
using System.Reactive.Disposables;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
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

        public TitleBar()
        {

        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return base.ArrangeOverride(finalSize);
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
                    .Subscribe(x =>
                    {
                        Height = x.Top;
                    }),

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
            }
        }

        void InvalidateSize()
        {
            Margin = new Thickness(1, _hostWindow.OffScreenMargin.Top, 1, 1);
            Height = _hostWindow.WindowDecorationMargins.Top;
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
