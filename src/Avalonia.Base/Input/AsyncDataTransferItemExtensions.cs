using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace Avalonia.Input;

/// <summary>
/// Contains extension methods for <see cref="IAsyncDataTransferItem"/>.
/// </summary>
public static class AsyncDataTransferItemExtensions
{
    /// <summary>
    /// Gets whether a <see cref="IAsyncDataTransferItem"/> supports a specific format.
    /// </summary>
    /// <param name="dataTransferItem">The <see cref="IAsyncDataTransferItem"/> instance.</param>
    /// <param name="format">The format to check.</param>
    /// <returns>true if <paramref name="format"/> is supported, false otherwise.</returns>
    public static bool Contains(this IAsyncDataTransferItem dataTransferItem, DataFormat format)
    {
        var formats = dataTransferItem.Formats;
        var count = formats.Count;

        for (var i = 0; i < count; ++i)
        {
            if (format == formats[i])
                return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to get a value for a given format from a <see cref="IAsyncDataTransferItem"/>.
    /// </summary>
    /// <param name="dataTransferItem">The <see cref="IAsyncDataTransferItem"/> instance.</param>
    /// <param name="format">The format to retrieve.</param>
    /// <returns>A value for <paramref name="format"/>, or null if the format is not supported.</returns>
    public static async Task<T?> TryGetValueAsync<T>(this IAsyncDataTransferItem dataTransferItem, DataFormat<T> format)
        where T : class
        => await dataTransferItem.TryGetRawAsync(format).ConfigureAwait(false) as T;

    /// <summary>
    /// Returns a text, if available, from a <see cref="IAsyncDataTransferItem"/> instance.
    /// </summary>
    /// <param name="dataTransferItem">The <see cref="IAsyncDataTransferItem"/> instance.</param>
    /// <returns>A string, or null if the format isn't available.</returns>
    /// <seealso cref="DataFormat.Text"/>.
    public static Task<string?> TryGetTextAsync(this IAsyncDataTransferItem dataTransferItem)
        => dataTransferItem.TryGetValueAsync(DataFormat.Text);

    /// <summary>
    /// Returns a file, if available, from a <see cref="IAsyncDataTransferItem"/> instance.
    /// </summary>
    /// <param name="dataTransferItem">The <see cref="IAsyncDataTransferItem"/> instance.</param>
    /// <returns>An <see cref="IStorageItem"/> (file or folder), or null if the format isn't available.</returns>
    /// <seealso cref="DataFormat.File"/>.
    public static Task<IStorageItem?> TryGetFileAsync(this IAsyncDataTransferItem dataTransferItem)
        => dataTransferItem.TryGetValueAsync(DataFormat.File);

    /// <summary>
    /// Returns a bitmap, if available, from a <see cref="IAsyncDataTransferItem"/> instance.
    /// </summary>
    /// <param name="dataTransferItem">The <see cref="IAsyncDataTransferItem"/> instance.</param>
    /// <returns>A <see cref="Bitmap"/>, or null if the format isn't available.</returns>
    /// <seealso cref="DataFormat.Bitmap"/>.
    public static Task<Bitmap?> TryGetBitmapAsync(this IAsyncDataTransferItem dataTransferItem)
        => dataTransferItem.TryGetValueAsync(DataFormat.Bitmap);
}
