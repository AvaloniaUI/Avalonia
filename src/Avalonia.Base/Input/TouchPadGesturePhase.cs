namespace Avalonia.Input;

/// <summary>
/// Describes the phase of a touchpad gesture.
/// </summary>
public enum TouchPadGesturePhase
{
    /// <summary>
    /// The gesture has no phase information. This is the case for discrete gestures such as swipes,
    /// and on platforms that don't report a phase.
    /// </summary>
    None,

    /// <summary>
    /// The gesture has begun.
    /// </summary>
    Began,

    /// <summary>
    /// The gesture is in progress and its value has changed.
    /// </summary>
    Changed,

    /// <summary>
    /// The gesture has ended normally.
    /// </summary>
    Ended,

    /// <summary>
    /// The gesture has been cancelled by the system.
    /// </summary>
    Cancelled
}
