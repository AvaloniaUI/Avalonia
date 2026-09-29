namespace Avalonia.Layout
{
    /// <summary>
    /// A bring-into-view request, processed by the <see cref="IBringIntoViewLayoutManager"/> at the end of a layout pass.
    /// </summary>
    internal abstract class BringIntoViewRequest(Layoutable target)
    {
        /// <summary>
        /// Gets the control this request is associated with.
        /// </summary>
        public Layoutable Target { get; } = target;

        /// <summary>
        /// Attempts to execute the request.
        /// </summary>
        /// <returns>
        /// true if the request has been executed (or abandoned) and should be removed from the queue;
        /// false if the target isn't ready yet and the request should be retained for a following layout pass.
        /// </returns>
        public abstract bool TryExecute();
    }
}
