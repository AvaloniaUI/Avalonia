namespace Avalonia.Controls
{
    /// <summary>
    /// Specifies a contract for a scrolling control that supports scroll anchoring.
    /// </summary>
    public interface IScrollAnchorProvider
    {
        /// <summary>
        /// The currently chosen anchor element to use for scroll anchoring.
        /// </summary>
        IControl? CurrentAnchor { get; }

        /// <summary>
        /// Registers a control as a potential scroll anchor candidate.
        /// </summary>
        /// <param name="element">
        /// A control within the subtree of the <see cref="IScrollAnchorProvider"/>.
        /// </param>
        void RegisterAnchorCandidate(IControl element);

        /// <summary>
        /// Unregisters a control as a potential scroll anchor candidate.
        /// </summary>
        /// <param name="element">
        /// A control within the subtree of the <see cref="IScrollAnchorProvider"/>.
        /// </param>
        void UnregisterAnchorCandidate(IControl element);
    }
}
