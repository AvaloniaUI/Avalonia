using System;
using System.Threading;
using Avalonia.Animation;
using Avalonia.Controls.Templates;
using Avalonia.Threading;

namespace Avalonia.Controls;

/// <summary>
/// Displays <see cref="ContentControl.Content"/> according to a <see cref="FuncDataTemplate"/>.
/// Uses <see cref="PageTransition"/> to move between the old and new content values. 
/// </summary>
public class TransitioningContentControl : ContentControl
{
    private CancellationTokenSource? _lastTransitionCts;
    private object? _currentContent;

    /// <summary>
    /// Defines the <see cref="PageTransition"/> property.
    /// </summary>
    public static readonly StyledProperty<IPageTransition?> PageTransitionProperty =
        AvaloniaProperty.Register<TransitioningContentControl, IPageTransition?>(nameof(PageTransition),
            new CrossFade(TimeSpan.FromSeconds(0.125)));
    
    /// <summary>
    /// Defines the <see cref="CurrentContent"/> property.
    /// </summary>
    public static readonly DirectProperty<TransitioningContentControl, object?> CurrentContentProperty =
        AvaloniaProperty.RegisterDirect<TransitioningContentControl, object?>(nameof(CurrentContent),
            o => o.CurrentContent);

    /// <summary>
    /// Gets or sets the animation played when content appears and disappears.
    /// </summary>
    public IPageTransition? PageTransition
    {
        get => GetValue(PageTransitionProperty);
        set => SetValue(PageTransitionProperty, value);
    }

    /// <summary>
    /// Gets the content currently displayed on the screen.
    /// </summary>
    public object? CurrentContent
    {
        get => _currentContent;
        private set => SetAndRaise(CurrentContentProperty, ref _currentContent, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        Dispatcher.UIThread.Post(() => UpdateContentWithTransition(Content));
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        _lastTransitionCts?.Cancel();
    }

    protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ContentProperty)
        {
            Dispatcher.UIThread.Post(() => UpdateContentWithTransition(Content));
        }
    }

    /// <summary>
    /// Updates the content with transitions.
    /// </summary>
    /// <param name="content">New content to set.</param>
    private async void UpdateContentWithTransition(object? content)
    {
        if (VisualRoot is null)
        {
            return;
        }

        _lastTransitionCts?.Cancel();
        _lastTransitionCts = new CancellationTokenSource();
        var localToken = _lastTransitionCts.Token;

        if (PageTransition != null)
            await PageTransition.Start(this, null, true, localToken);

        if (localToken.IsCancellationRequested)
        {
            return;
        }

        CurrentContent = content;

        if (PageTransition != null)
            await PageTransition.Start(null, this, true, localToken);
    }
}
