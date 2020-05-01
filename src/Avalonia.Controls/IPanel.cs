namespace Avalonia.Controls
{
    /// <summary>
    /// Interface for controls that can contain multiple children.
    /// </summary>
    public interface IPanel : IControl
    {
        /// <summary>
        /// Gets the children of the <see cref="Panel"/>.
        /// </summary>
        Controls Children { get; }
    }
}