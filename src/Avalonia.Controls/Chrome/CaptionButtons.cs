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
        internal const string PART_CloseButton = "PART_CloseButton";
        internal const string PART_RestoreButton = "PART_RestoreButton";
        internal const string PART_MinimizeButton = "PART_MinimizeButton";
        internal const string PART_FullScreenButton = "PART_FullScreenButton";

        private Button? _restoreButton;
        private Button? _minimizeButton;
        private Button? _fullScreenButton;
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
                    HostWindow.GetObservable(Window.CanMaximizeProperty)
                        .Subscribe(_ =>
                        {
                            UpdateRestoreButtonState();
                            UpdateFullScreenButtonState();
                        }),
                    HostWindow.GetObservable(Window.CanMinimizeProperty)
                        .Subscribe(_ =>
                        {
                            UpdateMinimizeButtonState();
                        }),
                    HostWindow.GetObservable(Window.WindowStateProperty)
                        .Subscribe(x =>
                        {
                            PseudoClasses.Set(":minimized", x == WindowState.Minimized);
                            PseudoClasses.Set(":normal", x == WindowState.Normal);
                            PseudoClasses.Set(":maximized", x == WindowState.Maximized);
                            PseudoClasses.Set(":fullscreen", x == WindowState.FullScreen);
                            UpdateRestoreButtonState();
                            UpdateMinimizeButtonState();
                            UpdateFullScreenButtonState();
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
            
            if (e.NameScope.Find<Button>(PART_CloseButton) is { } closeButton)
            {
                closeButton.Click += (_, args) =>
                {
                    OnClose();
                    args.Handled = true;
                };
            }

            if (e.NameScope.Find<Button>(PART_RestoreButton) is { } restoreButton)
            {
                restoreButton.Click += (_, args) =>
                {
                    OnRestore();
                    args.Handled = true;
                };
                _restoreButton = restoreButton;
                UpdateRestoreButtonState();
            }
            
            if (e.NameScope.Find<Button>(PART_MinimizeButton) is { } minimizeButton)
            {
                minimizeButton.Click += (_, args) =>
                {
                    OnMinimize();
                    args.Handled = true;
                };
                _minimizeButton = minimizeButton;
                UpdateMinimizeButtonState();
            }
            
            if (e.NameScope.Find<Button>(PART_FullScreenButton) is { } fullScreenButton)
            {
                fullScreenButton.Click += (_, args) =>
                {
                    OnToggleFullScreen();
                    args.Handled = true;
                };
                _fullScreenButton = fullScreenButton;
                UpdateFullScreenButtonState();
            }
        }

        private void UpdateRestoreButtonState()
        {
            if (_restoreButton is null)
                return;

            _restoreButton.IsEnabled = HostWindow?.WindowState switch
            {
                WindowState.Maximized or WindowState.FullScreen => HostWindow.CanResize,
                WindowState.Normal => HostWindow.CanMaximize,
                _ => true
            };
        }

        private void UpdateMinimizeButtonState()
        {
            if (_minimizeButton is null)
                return;

            _minimizeButton.IsEnabled = HostWindow?.CanMinimize ?? true;
        }

        private void UpdateFullScreenButtonState()
        {
            if (_fullScreenButton is null)
                return;

            _fullScreenButton.IsEnabled = HostWindow?.WindowState == WindowState.FullScreen ?
                HostWindow.CanResize :
                HostWindow?.CanMaximize ?? true;
        }
    }
}
