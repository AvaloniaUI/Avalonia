using Avalonia.Metadata;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Defines the interface for easing classes.
    /// </summary>
    [NotClientImplementable]
    public interface IEasing
    {
        /// <summary>
        /// Returns the value of the transition for the specified progress.
        /// </summary>
        double Ease(double progress);
    }
}
