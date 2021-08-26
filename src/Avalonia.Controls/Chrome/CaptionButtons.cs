using System;
using System.Reactive.Disposables;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

#nullable enable

namespace Avalonia.Controls.Chrome
{
    /// <summary>
    /// Draws window minimize / maximize / close buttons in a <see cref="TitleBar"/> when managed client decorations are enabled.
    /// </summary>
    [PseudoClasses(":minimized", ":normal", ":maximized", ":fullscreen")]
    public class CaptionButtons : TemplatedControl
    {
        private CompositeDisposable? _disposables;

        /// <summary>
        /// Currently attached window.
        /// </summary>
        protected Window? HostWindow { get; private set; }

        public virtual void Attach(Window hostWindow)
        {
            if (_disposables == null)
            {
                HostWindow = hostWindow;

                _disposables = new CompositeDisposable
                {
                    HostWindow.GetObservable(Window.WindowStateProperty)
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

        public virtual void Detach()
        {
            if (_disposables != null)
            {
                _disposables.Dispose();
                _disposables = null;

                HostWindow = null;
            }
        }

        protected virtual void OnClose()
        {
            HostWindow?.Close();
        }

        protected virtual void OnRestore()
        {
            if (HostWindow != null)
            {
                HostWindow.WindowState = HostWindow.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
        }

        protected virtual void OnMinimize()
        {
            if (HostWindow != null)
            {
                HostWindow.WindowState = WindowState.Minimized;
            }
        }

        protected virtual void OnToggleFullScreen()
        {
            if (HostWindow != null)
            {
                HostWindow.WindowState = HostWindow.WindowState == WindowState.FullScreen
                    ? WindowState.Normal
                    : WindowState.FullScreen;
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            var closeButton = e.NameScope.Get<Panel>("PART_CloseButton");
            var restoreButton = e.NameScope.Get<Panel>("PART_RestoreButton");
            var minimiseButton = e.NameScope.Get<Panel>("PART_MinimiseButton");
            var fullScreenButton = e.NameScope.Get<Panel>("PART_FullScreenButton");

            closeButton.PointerReleased += (sender, e) => OnClose();

            restoreButton.PointerReleased += (sender, e) => OnRestore();

            minimiseButton.PointerReleased += (sender, e) => OnMinimize();

            fullScreenButton.PointerReleased += (sender, e) => OnToggleFullScreen();
        }
    }
}
