namespace Avalonia.Layout;

/// <summary>
/// A <see cref="ILayoutManager"/> that handles <see cref="BringIntoViewRequest"/>.
/// </summary>
internal interface IBringIntoViewLayoutManager : ILayoutManager
{
    /// <summary>
    /// Gets whether a layout pass is currently running.
    /// </summary>
    bool IsInLayoutPass { get; }

    /// <summary>
    /// Enqueues a bring-into-view request to be processed at the end of the current or next layout pass,
    /// replacing any previously enqueued request for the same target.
    /// </summary>
    void EnqueueBringIntoView(BringIntoViewRequest request);
}
