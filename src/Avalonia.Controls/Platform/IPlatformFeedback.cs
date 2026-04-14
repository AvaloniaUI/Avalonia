using Avalonia.Metadata;

namespace Avalonia.Controls.Platform
{
    [NotClientImplementable]
    public interface IPlatformFeedback
    {
        /// <summary>
        /// Performs the specified <see cref="FeedbackType"/> on the platform.
        /// </summary>
        /// <param name="feedback">The feedback type to perform.</param>
        /// <param name="type">The feedback effect relating to the action that triggered it</param>
        /// <returns>true if the platform performed the requested feedback; false otherwise.</returns>
        bool Perform(FeedbackEffect feedback, FeedbackType type);
    }

    /// <summary>
    /// The feedback type to be triggered for the attached control.
    /// </summary>
    public enum FeedbackType
    {
        /// <summary>
        /// Disables feedback for the attached control
        /// </summary>
        None,

        /// <summary>
        /// If available, triggers both sound and haptic feedback for the attached control
        /// </summary>
        Auto,

        /// <summary>
        /// If available, triggers only sound feedback for the attached control
        /// </summary>
        Sound,

        /// <summary>
        /// If available, triggers only haptic feedback for the attached control
        /// </summary>
        Haptic
    }

    /// <summary>
    /// Predefined platform feedback effect.
    /// </summary>
    public enum FeedbackEffect
    {
        /// <summary>
        /// The feedback is related to the Click action
        /// </summary>
        Click,

        /// <summary>
        /// The feedback is related to the Hold action
        /// </summary>
        LongPress,
    }
}
