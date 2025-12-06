using System;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Reactive;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace Avalonia.Controls.Chrome
{
    /// <summary>
    /// Draws a titlebar when managed client decorations are enabled.
    /// </summary>
    [TemplatePart("PART_CaptionButtons", typeof(CaptionButtons), IsRequired = true)]
    [PseudoClasses(":minimized", ":normal", ":maximized", ":fullscreen")]
    public class TitleBar : TemplatedControl
    {
        private CompositeDisposable? _disposables;
        private CaptionButtons? _captionButtons;

        private void UpdateSize(Window window)
        {
            Margin = new Thickness(
                window.OffScreenMargin.Left,
                window.OffScreenMargin.Top,
                window.OffScreenMargin.Right,
                window.OffScreenMargin.Bottom);

            if (window.WindowState != WindowState.FullScreen)
            {
                var height = Math.Max(0, window.WindowDecorationMargin.Top);
                Height = height;
                _captionButtons?.Height = window.SystemDecorations == SystemDecorations.Full ? height : 0;
            }
            else
            {
                // Note: apparently the titlebar was supposed to be displayed when hovering the top of the screen,
                // to mimic macOS behavior. This has been broken for years. It actually only partially works if the
                // window is FullScreen right on startup, and only once. Any size change will then break it.
                // Disable it for now.
                // TODO: restore that behavior so that it works in all cases
                Height = 0;
                _captionButtons?.Height = 0;
            }

            IsVisible = window.PlatformImpl?.NeedsManagedDecorations ?? false;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _captionButtons?.Detach();

            _captionButtons = e.NameScope.Get<CaptionButtons>("PART_CaptionButtons");

            if (VisualRoot is Window window)
            {
                _captionButtons?.Attach(window);

                UpdateSize(window);
            }
        }

        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (VisualRoot is Window window)
            {
                _disposables = new CompositeDisposable(6)
                {
                    window.GetObservable(Window.WindowDecorationMarginProperty)
                        .Subscribe(_ => UpdateSize(window)),
                    window.GetObservable(Window.ExtendClientAreaTitleBarHeightHintProperty)
                        .Subscribe(_ => UpdateSize(window)),
                    window.GetObservable(Window.OffScreenMarginProperty)
                        .Subscribe(_ => UpdateSize(window)),
                    window.GetObservable(Window.ExtendClientAreaChromeHintsProperty)
                        .Subscribe(_ => UpdateSize(window)),
                    window.GetObservable(Window.WindowStateProperty)
                        .Subscribe(x =>
                        {
                            PseudoClasses.Set(":minimized", x == WindowState.Minimized);
                            PseudoClasses.Set(":normal", x == WindowState.Normal);
                            PseudoClasses.Set(":maximized", x == WindowState.Maximized);
                            PseudoClasses.Set(":fullscreen", x == WindowState.FullScreen);
                            UpdateSize(window);
                        }),
                    window.GetObservable(Window.IsExtendedIntoWindowDecorationsProperty)
                        .Subscribe(_ => UpdateSize(window))
                };
            }
        }

        /// <inheritdoc />
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            _disposables?.Dispose();

            _captionButtons?.Detach();
            _captionButtons = null;
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer() => new TitleBarAutomationPeer(this);
    }
}
