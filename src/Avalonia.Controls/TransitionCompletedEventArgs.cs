using Avalonia.Interactivity;

namespace Avalonia.Controls;

/// <summary>
/// Represents the event arguments for <see cref="TransitioningContentControl.TransitionCompletedEvent"/>.
/// </summary>
public class TransitionCompletedEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="TransitionCompletedEventArgs"/>.
    /// </summary>
    /// <param name="from">The content that was transitioned from.</param>
    /// <param name="to">The content that was transitioned to.</param>
    /// <param name="hasRunToCompletion">Whether the transition ran to completion.</param>
    public TransitionCompletedEventArgs(object? from, object? to, bool hasRunToCompletion)
        : base(TransitioningContentControl.TransitionCompletedEvent)
    {
        From = from;
        To = to;
        HasRunToCompletion = hasRunToCompletion;
    }

    /// <summary>
    /// Gets the content that was transitioned from.
    /// </summary>
    public object? From { get; }

    /// <summary>
    /// Gets the content that was transitioned to.
    /// </summary>
    public object? To { get; }

    /// <summary>
    /// Gets whether the transition ran to completion.
    /// If false, the transition may have completed instantly or been cancelled.
    /// </summary>
    public bool HasRunToCompletion { get; }
}
