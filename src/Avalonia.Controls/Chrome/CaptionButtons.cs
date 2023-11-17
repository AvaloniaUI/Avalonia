using System;
using Avalonia.Reactive;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace Avalonia.Controls.Chrome
{
    /// <summary>
    /// Draws window minimize / maximize / close buttons in a <see cref="TitleBar"/> when managed client decorations are enabled.
    /// </summary>
    [TemplatePart(PART_CloseButton,      typeof(Button))]
    [TemplatePart(PART_RestoreButton,    typeof(Button))]
    [TemplatePart(PART_MinimizeButton,   typeof(Button))]
    [TemplatePart(PART_FullScreenButton, typeof(Button))]
    [PseudoClasses(":minimized", ":normal", ":maximized", ":fullscreen")]
    public class CaptionButtons : TemplatedControl
    {
        private const string PART_CloseButton = "PART_CloseButton";
        private const string PART_RestoreButton = "PART_RestoreButton";
        private const string PART_MinimizeButton = "PART_MinimizeButton";
        private const string PART_FullScreenButton = "PART_FullScreenButton";

        private Button? _restoreButton;
        private IDisposable? _disposables;

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
                    HostWindow.GetObservable(Window.CanResizeProperty)
                        .Subscribe(x =>
                        {
                            if (_restoreButton is not null)
                                _restoreButton.IsEnabled = x;
                        }),
                    HostWindow.GetObservable(Window.WindowStateProperty)
                        .Subscribe(x =>
                        {
                            PseudoClasses.Set(":minimized", x == WindowState.Minimized);
                            PseudoClasses.Set(":normal", x == WindowState.Normal);
                            PseudoClasses.Set(":maximized", x == WindowState.Maximized);
                            PseudoClasses.Set(":fullscreen", x == WindowState.FullScreen);
                        }),
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

            var closeButton = e.NameScope.Get<Button>(PART_CloseButton);
            var restoreButton = e.NameScope.Get<Button>(PART_RestoreButton);
            var minimizeButton = e.NameScope.Get<Button>(PART_MinimizeButton);
            var fullScreenButton = e.NameScope.Get<Button>(PART_FullScreenButton);

            closeButton.Click += (sender, e) => OnClose();
            restoreButton.Click += (sender, e) => OnRestore();
            minimizeButton.Click += (sender, e) => OnMinimize();
            fullScreenButton.Click += (sender, e) => OnToggleFullScreen();

            restoreButton.IsEnabled = HostWindow?.CanResize ?? true;
            _restoreButton = restoreButton;
        }
    }
}
