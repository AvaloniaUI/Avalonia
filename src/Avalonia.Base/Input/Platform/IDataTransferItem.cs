using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.Input.Platform;

/// <summary>
/// Represent an item inside a <see cref="IDataTransfer"/>.
/// An item may support several formats and can return the value of a given format on demand.
/// </summary>
/// <seealso cref="DataTransferItem"/>
public interface IDataTransferItem
{
    /// <summary>
    /// Gets the formats supported by this item.
    /// </summary>
    /// <returns>A list of supported formats.</returns>
    IEnumerable<DataFormat> GetFormats();

    /// <summary>
    /// Gets whether this item supports a specific format.
    /// </summary>
    /// <param name="format">The format to check.</param>
    /// <returns>true if <paramref name="format"/> is supported, false otherwise.</returns>
    bool Contains(DataFormat format);

    /// <summary>
    /// Tries to get a value for a given format.
    /// </summary>
    /// <param name="format">The format to retrieve.</param>
    /// <returns>A value for <paramref name="format"/>, or null if the format is not supported.</returns>
    /// <remarks>
    /// <para>
    /// Several supported platforms might wait on the returned task synchronously from the UI thread
    /// while writing to the UI thread or during a drag and drop operation. This limitation is due to the underlying
    /// platform.
    /// </para>
    /// <para>
    /// For this reason, implementations of this method should never resume their work on the UI thread
    /// (use <c>ConfigureAwait(false)</c> when awaiting) to avoid potential deadlocks.
    /// </para>
    /// </remarks>
    Task<object?> TryGetAsync(DataFormat format);
}
