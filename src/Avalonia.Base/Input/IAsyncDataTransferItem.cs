using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.Input;

/// <summary>
/// Represent an item inside a <see cref="IAsyncDataTransfer"/>.
/// An item may support several formats and can return the value of a given format on demand.
/// </summary>
/// <seealso cref="DataTransferItem"/>
public interface IAsyncDataTransferItem
{
    /// <summary>
    /// Gets the formats supported by this item.
    /// </summary>
    IReadOnlyList<DataFormat> Formats { get; }

    /// <summary>
    /// Tries to get a value for a given format.
    /// </summary>
    /// <param name="format">The format to retrieve.</param>
    /// <returns>A value for <paramref name="format"/>, or null if the format is not supported.</returns>
    Task<object?> TryGetAsync(DataFormat format);
}
