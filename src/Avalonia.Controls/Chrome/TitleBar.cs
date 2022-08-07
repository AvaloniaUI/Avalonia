using System;
using System.Reactive.Disposables;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace Avalonia.Controls.Chrome
{
    /// <summary>
    /// Draws a titlebar when managed client decorations are enabled.
    /// </summary>
    [TemplatePart("PART_CaptionButtons", typeof(CaptionButtons))]
    [PseudoClasses(":minimized", ":normal", ":maximized", ":fullscreen")]
    public class TitleBar : TemplatedControl
    {
        private CompositeDisposable? _disposables;
        private CaptionButtons? _captionButtons;

        private void UpdateSize(Window window)
        {
            if (window != null)
            {
                Margin = new Thickness(
                    window.OffScreenMargin.Left,
                    window.OffScreenMargin.Top,
                    window.OffScreenMargin.Right,
                    window.OffScreenMargin.Bottom);

                if (window.WindowState != WindowState.FullScreen)
                {
                    Height = window.WindowDecorationMargin.Top;

                    if (_captionButtons != null)
                    {
                        _captionButtons.Height = Height;
                    }
                }

                IsVisible = window.PlatformImpl?.NeedsManagedDecorations ?? false;
            }
        }

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

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (VisualRoot is Window window)
            {
                _disposables = new CompositeDisposable
                {
                    window.GetObservable(Window.WindowDecorationMarginProperty)
                        .Subscribe(x => UpdateSize(window)),
                    window.GetObservable(Window.ExtendClientAreaTitleBarHeightHintProperty)
                        .Subscribe(x => UpdateSize(window)),
                    window.GetObservable(Window.OffScreenMarginProperty)
                        .Subscribe(x => UpdateSize(window)),
                    window.GetObservable(Window.ExtendClientAreaChromeHintsProperty)
                        .Subscribe(x => UpdateSize(window)),
                    window.GetObservable(Window.WindowStateProperty)
                        .Subscribe(x =>
                        {
                            PseudoClasses.Set(":minimized", x == WindowState.Minimized);
                            PseudoClasses.Set(":normal", x == WindowState.Normal);
                            PseudoClasses.Set(":maximized", x == WindowState.Maximized);
                            PseudoClasses.Set(":fullscreen", x == WindowState.FullScreen);
                        }),
                    window.GetObservable(Window.IsExtendedIntoWindowDecorationsProperty)
                        .Subscribe(x => UpdateSize(window))
                };
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            _disposables?.Dispose();

            _captionButtons?.Detach();
            _captionButtons = null;
        }
    }
}
