namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Extends IDataTemplate to enable content-level virtualization.
    /// Templates implementing this interface can recycle content controls
    /// based on custom keys, reducing allocation and layout pressure during virtualization.
    /// </summary>
    public interface IVirtualizingDataTemplate : IRecyclingDataTemplate
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
        /// Gets the maximum number of controls to keep in the recycle pool
        /// for each key. Default is 5.
        /// </summary>
        int MaxPoolSizePerKey { get; }
        
        /// <summary>
        /// Gets the minimum number of controls to keep in the recycle pool
        /// for each key. Default is 0.
        /// This is only used when warmup is enabled
        /// </summary>
        int MinPoolSizePerKey { get; }
    }
}
