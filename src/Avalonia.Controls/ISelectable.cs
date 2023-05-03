namespace Avalonia.Controls
{
    /// <summary>
    /// An interface that is implemented by objects that expose their selection state via a
    /// boolean <see cref="IsSelected"/> property.
    /// </summary>
    public interface ISelectable
    {
        /// <summary>
        /// Gets or sets the selected state of the object.
        /// </summary>
        bool IsSelected { get; set; }
    }
}
