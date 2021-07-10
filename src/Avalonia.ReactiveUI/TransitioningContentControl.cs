using System;
using System.Threading;

using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Avalonia.ReactiveUI
{
    /// <summary>
    /// A ContentControl that animates the transition when its content is changed.
    /// </summary>
    public class TransitioningContentControl : ContentControl, IStyleable
    {
        /// <summary>
        /// <see cref="AvaloniaProperty"/> for the <see cref="PageTransition"/> property.
        /// </summary>
        public static readonly StyledProperty<IPageTransition?> PageTransitionProperty =
            AvaloniaProperty.Register<TransitioningContentControl, IPageTransition?>(nameof(PageTransition),
                new CrossFade(TimeSpan.FromSeconds(0.5)));

        /// <summary>
        /// <see cref="AvaloniaProperty"/> for the <see cref="DefaultContent"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> DefaultContentProperty =
            AvaloniaProperty.Register<TransitioningContentControl, object?>(nameof(DefaultContent));

        private CancellationTokenSource? _lastTransitionCts;

        /// <summary>
        /// Gets or sets the animation played when content appears and disappears.
        /// </summary>
        public IPageTransition? PageTransition
        {
            get => GetValue(PageTransitionProperty);
            set => SetValue(PageTransitionProperty, value);
        }

        /// <summary>
        /// Gets or sets the content displayed whenever there is no page currently routed.
        /// </summary>
        public object? DefaultContent
        {
            get => GetValue(DefaultContentProperty);
            set => SetValue(DefaultContentProperty, value);
        }

        /// <summary>
        /// Gets or sets the content with animation.
        /// </summary>
        public new object? Content
        {
            get => base.Content;
            set => UpdateContentWithTransition(value);
        }
        
        /// <summary>
        /// TransitioningContentControl uses the default ContentControl 
        /// template from Avalonia default theme.
        /// </summary>
        Type IStyleable.StyleKey => typeof(ContentControl);

        /// <summary>
        /// Updates the content with transitions.
        /// </summary>
        /// <param name="content">New content to set.</param>
        private async void UpdateContentWithTransition(object? content)
        {
            _lastTransitionCts?.Cancel();
            _lastTransitionCts = new CancellationTokenSource();

            if (PageTransition != null)
                await PageTransition.Start(this, null, true, _lastTransitionCts.Token);
            base.Content = content;
            if (PageTransition != null)
                await PageTransition.Start(null, this, true, _lastTransitionCts.Token);
        }
    }
}
