





namespace Perspex.Styling
{
    /// <summary>
    /// Defines the interface for styles.
    /// </summary>
    public interface IStyle
    {
        /// <summary>
        /// Attaches the style to a control if the style matches.
        /// </summary>
        /// <param name="control">The control to attach to.</param>
        void Attach(IStyleable control);
    }
}
