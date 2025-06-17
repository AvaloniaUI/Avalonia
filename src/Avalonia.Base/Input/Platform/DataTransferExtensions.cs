using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace Avalonia.Input.Platform;

/// <summary>
/// Contains extension methods for <see cref="IDataTransfer"/>.
/// </summary>
public static class DataTransferExtensions
{
    // TODO12: remove
    internal static IDataObject ToLegacyDataObject(this IDataTransfer dataTransfer)
        => (dataTransfer as DataObjectToDataTransferWrapper)?.DataObject
           ?? new DataTransferToDataObjectWrapper(dataTransfer);

    /// <summary>
    /// Gets whether a <see cref="IDataTransfer"/> supports a specific format.
    /// </summary>
    /// <param name="dataTransfer">The <see cref="IDataTransfer"/> instance.</param>
    /// <param name="format">The format to check.</param>
    /// <returns>true if <paramref name="format"/> is supported, false otherwise.</returns>
    public static bool Contains(this IDataTransfer dataTransfer, DataFormat format)
        => dataTransfer.GetItems([format]).Any();

    /// <summary>
    /// Tries to get a value for a given format from a <see cref="IDataTransfer"/>.
    /// </summary>
    /// <param name="dataTransfer">The <see cref="IDataTransfer"/> instance.</param>
    /// <param name="format">The format to retrieve.</param>
    /// <returns>A value for <paramref name="format"/>, or null if the format is not supported.</returns>
    /// <remarks>
    /// If the <see cref="IDataTransfer"/> contains several items supporting <paramref name="format"/>,
    /// the first matching one will be returned.
    /// </remarks>
    public static async Task<T?> TryGetValueAsync<T>(this IDataTransfer dataTransfer, DataFormat format)
    {
        foreach (var item in dataTransfer.GetItems([format]))
        {
            if (!item.Contains(format))
                continue;

            var result = await item.TryGetAsync(format).ConfigureAwait(false);
            return result is T typedResult ? typedResult : default;
        }

        return default;
    }

    /// <summary>
    /// Tries to get multiple values for a given format from a <see cref="IDataTransfer"/>.
    /// </summary>
    /// <param name="dataTransfer">The <see cref="IDataTransfer"/> instance.</param>
    /// <param name="format">The format to retrieve.</param>
    /// <returns>A list of values for <paramref name="format"/>, or null if the format is not supported.</returns>
    public static async Task<T[]?> TryGetValuesAsync<T>(this IDataTransfer dataTransfer, DataFormat format)
    {
        List<T>? results = null;

        foreach (var item in dataTransfer.GetItems([format]))
        {
            if (!item.Contains(format))
                continue;

            var result = await item.TryGetAsync(format).ConfigureAwait(false);
            if (result is not T typedResult)
                continue;

            results ??= [];
            results.Add(typedResult);
        }

        return results?.ToArray();
    }

    /// <summary>
    /// Returns a text, if available, from a <see cref="IDataTransfer"/> instance.
    /// </summary>
    /// <param name="dataTransfer">The data transfer instance.</param>
    /// <returns>A string, or null if the format isn't available.</returns>
    /// <seealso cref="DataFormat.Text"/>.
    public static Task<string?> TryGetTextAsync(this IDataTransfer dataTransfer)
        => dataTransfer.TryGetValueAsync<string>(DataFormat.Text);

    /// <summary>
    /// Returns a list of files, if available, from a <see cref="IDataTransfer"/> instance.
    /// </summary>
    /// <param name="dataTransfer">The data transfer instance.</param>
    /// <returns>An array of <see cref="IStorageItem"/> (files or folders), or null if the format isn't available.</returns>
    /// <seealso cref="DataFormat.File"/>.
    public static async Task<IStorageItem[]?> TryGetFilesAsync(this IDataTransfer dataTransfer)
        => await dataTransfer.TryGetValuesAsync<IStorageItem>(DataFormat.File).ConfigureAwait(false);
}
