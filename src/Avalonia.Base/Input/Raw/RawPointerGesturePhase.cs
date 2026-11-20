namespace Avalonia.Input.Raw;

/// <summary>
/// The phase of a continuous gesture reported by the platform, for touchpad gestures and scrolling.
/// </summary>
public enum RawPointerGesturePhase
{
    /// <summary>
    /// The event isn't part of a gesture with phases (e.g. a classic mouse wheel).
    /// </summary>
    None,

    /// <summary>
    /// The gesture has started.
    /// </summary>
    Started,

    /// <summary>
    /// The gesture is in progress.
    /// </summary>
    Updated,

    /// <summary>
    /// The gesture was completed.
    /// </summary>
    Completed,

    /// <summary>
    /// The gesture was canceled by the system.
    /// </summary>
    Canceled
}
