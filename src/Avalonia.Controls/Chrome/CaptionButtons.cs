using System;
using System.Reactive.Disposables;
using Avalonia.Controls.Primitives;

#nullable enable

namespace Avalonia.Controls.Chrome
{
    /// <summary>
    /// Draws window minimize / maximize / close buttons in a <see cref="TitleBar"/> when managed client decorations are enabled.
    /// </summary>
    public class CaptionButtons : TemplatedControl
    {
        private CompositeDisposable? _disposables;
        private Window? _hostWindow;

        public void Attach(Window hostWindow)
        {
            if (_disposables == null)
            {
                _hostWindow = hostWindow;

                _disposables = new CompositeDisposable
                {
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

        public void Detach()
        {
            if (_disposables != null)
            {
                _disposables.Dispose();
                _disposables = null;

                _hostWindow = null;
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            var closeButton = e.NameScope.Get<Panel>("PART_CloseButton");
            var restoreButton = e.NameScope.Get<Panel>("PART_RestoreButton");
            var minimiseButton = e.NameScope.Get<Panel>("PART_MinimiseButton");
            var fullScreenButton = e.NameScope.Get<Panel>("PART_FullScreenButton");

            closeButton.PointerReleased += (sender, e) => _hostWindow?.Close();

            restoreButton.PointerReleased += (sender, e) =>
            {
                if (_hostWindow != null)
                {
                    _hostWindow.WindowState = _hostWindow.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                }
            };

            minimiseButton.PointerReleased += (sender, e) =>
            {
                if (_hostWindow != null)
                {
                    _hostWindow.WindowState = WindowState.Minimized;
                }
            };

            fullScreenButton.PointerReleased += (sender, e) =>
            {
                if (_hostWindow != null)
                {
                    _hostWindow.WindowState = _hostWindow.WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
                }
            };
        }
    }
}
