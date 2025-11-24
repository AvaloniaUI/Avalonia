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
    /// <remarks>
    /// <para>
    /// Implementations of this method are expected to return a value matching the exact type
    /// of the generic argument of the underlying <see cref="DataFormat{T}"/>.
    /// </para>
    /// <para>
    /// To retrieve a typed value, use <see cref="AsyncDataTransferItemExtensions.TryGetValueAsync"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="AsyncDataTransferItemExtensions.TryGetValueAsync"/>
    /// <seealso cref="AsyncDataTransferExtensions.TryGetValueAsync"/>
    Task<object?> TryGetRawAsync(DataFormat format);
}
