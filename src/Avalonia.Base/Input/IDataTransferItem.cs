using System.Collections.Generic;

namespace Avalonia.Input;

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
    /// To retrieve a typed value, use <see cref="DataTransferItemExtensions.TryGetValue"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="DataTransferItemExtensions.TryGetValue"/>
    /// <seealso cref="DataTransferExtensions.TryGetValue"/>
    object? TryGetRaw(DataFormat format);
}
