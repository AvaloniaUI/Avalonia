using Avalonia.Metadata;

namespace Avalonia.Platform
{
    /// <summary>
    /// Options for a paint request from <see cref="ITopLevelImpl.Paint"/>.
    /// </summary>
    [Unstable]
    public record struct PaintOptions
    {
        /// <summary>
        /// Runs pending layout and animations synchronously before the frame is rendered,
        /// with dispatcher processing disabled.
        /// </summary>
        /// <remarks>
        /// Use it when the platform needs an up-to-date frame right now (e. g. during an interactive resize)
        /// and must not run dispatcher jobs from the paint callback.
        /// </remarks>
        public bool FlushLayout { get; init; }
    }
}
