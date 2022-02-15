using System;
using System.Threading;
using Avalonia.Animation;

namespace Avalonia.Controls;

public class TransitioningContentControl : ContentControl
{
    private CancellationTokenSource? _lastTransitionCts;
    
    /// <summary>
    /// Defines the <see cref="PageTransition"/> property.
    /// </summary>
    public static readonly StyledProperty<IPageTransition?> PageTransitionProperty =
        AvaloniaProperty.Register<TransitioningContentControl, IPageTransition?>(nameof(PageTransition),
            new CrossFade(TimeSpan.FromSeconds(0.5)));
    
    
    /// <summary>
    /// Defines the <see cref="DisplayedContent"/> property.
    /// </summary>
    public static readonly StyledProperty<object?> DisplayedContentProperty =
        AvaloniaProperty.Register<TransitioningContentControl, object?>(nameof(DisplayedContent));
    
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
    public object? DisplayedContent
    {
        get => GetValue(DisplayedContentProperty);
        set => SetValue(DisplayedContentProperty, value);
    }

    protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ContentProperty)
        {
            UpdateContentWithTransition(change.NewValue.GetValueOrDefault());
        }
    }

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

        DisplayedContent = content;
        
        if (PageTransition != null)
            await PageTransition.Start(null, this, true, _lastTransitionCts.Token);
    }
}
