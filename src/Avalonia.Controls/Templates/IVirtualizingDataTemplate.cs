namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Extends IDataTemplate to enable content-level virtualization.
    /// Templates implementing this interface can recycle content controls
    /// based on custom keys, reducing allocation and layout pressure during virtualization.
    /// </summary>
    public interface IVirtualizingDataTemplate : IDataTemplate
    {
        /// <summary>
        /// Gets a key that identifies which recycling pool this data belongs to.
        /// Controls created for data with the same key can be recycled together.
        /// </summary>
        /// <param name="data">The data object to get a key for.</param>
        /// <returns>
        /// A key object for recycling (typically the data's Type), or null to create
        /// a new control without recycling.
        /// </returns>
        object? GetKey(object? data);

        /// <summary>
        /// Builds or updates a control for the specified data, optionally recycling
        /// an existing control from the pool.
        /// </summary>
        /// <param name="data">The data object to display.</param>
        /// <param name="existing">
        /// An existing control to recycle, or null to create new.
        /// The control will have the same key as returned by GetKey(data).
        /// The template is responsible for updating the recycled control's state.
        /// </param>
        /// <returns>The control to display (either recycled or newly created).</returns>
        Control? Build(object? data, Control? existing);

        /// <summary>
        /// Gets the maximum number of controls to keep in the recycle pool
        /// for each key. Default is 5.
        /// </summary>
        int MaxPoolSizePerKey { get; }
    }
}
