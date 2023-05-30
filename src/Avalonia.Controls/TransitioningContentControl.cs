using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace Avalonia.Controls;

/// <summary>
/// Displays <see cref="ContentControl.Content"/> according to an <see cref="IDataTemplate"/>,
/// using a <see cref="PageTransition"/> to move between the old and new content. 
/// </summary>
public class TransitioningContentControl : ContentControl
{
    private CancellationTokenSource? _currentTransition;
    private ContentPresenter? _transitionPresenter;
    private Optional<object?> _transitionFrom;

    /// <summary>
    /// Defines the <see cref="PageTransition"/> property.
    /// </summary>
    public static readonly StyledProperty<IPageTransition?> PageTransitionProperty =
        AvaloniaProperty.Register<TransitioningContentControl, IPageTransition?>(
            nameof(PageTransition),
            defaultValue: new ImmutableCrossFade(TimeSpan.FromMilliseconds(125)));

    /// <summary>
    /// Gets or sets the animation played when content appears and disappears.
    /// </summary>
    public IPageTransition? PageTransition
    {
        get => GetValue(PageTransitionProperty);
        set => SetValue(PageTransitionProperty, value);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var result = base.ArrangeOverride(finalSize);

        if (_transitionFrom.HasValue)
        {
            _currentTransition?.Cancel();

            if (_transitionPresenter is not null &&
                Presenter is Visual presenter &&
                PageTransition is { } transition &&
                (_transitionFrom.Value is not Visual v || v.VisualParent is null))
            {
                _transitionPresenter.Content = _transitionFrom.Value;
                _transitionPresenter.IsVisible = true;
                _transitionFrom = Optional<object?>.Empty;
                
                var cancel = new CancellationTokenSource();
                _currentTransition = cancel;

                transition.Start(_transitionPresenter, presenter, true, cancel.Token).ContinueWith(x =>
                {
                    if (!cancel.IsCancellationRequested)
                    {
                        _transitionPresenter.Content = null;
                        _transitionPresenter.IsVisible = false;
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }

            _transitionFrom = Optional<object?>.Empty;
        }

        return result;
    }

    protected override bool RegisterContentPresenter(ContentPresenter presenter)
    {
        if (!base.RegisterContentPresenter(presenter) &&
            presenter is ContentPresenter p &&
            p.Name == "PART_TransitionContentPresenter")
        {
            _transitionPresenter = p;
            _transitionPresenter.IsVisible = false;
            return true;
        }

        return false;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ContentProperty && 
            _transitionPresenter is not null &&
            Presenter is Visual &&
            PageTransition is not null)
        {
            _transitionFrom = change.GetOldValue<object?>();
            InvalidateArrange();
        }
    }

    private class ImmutableCrossFade : IPageTransition
    {
        private readonly CrossFade _inner;

        public ImmutableCrossFade(TimeSpan duration) => _inner = new CrossFade(duration);

        public Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
        {
            return _inner.Start(from, to, cancellationToken);
        }
    }
}
